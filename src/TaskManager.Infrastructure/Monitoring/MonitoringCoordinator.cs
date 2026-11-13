using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Monitoring.Interfaces;
using TaskManager.Application.Monitoring.Models;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Monitoring;

public class MonitoringCoordinator : IMonitoringCoordinator, IHostedService, IAsyncDisposable
{
    private readonly ICpuMonitor _cpuMonitor;
    private readonly IMemoryMonitor _memoryMonitor;
    private readonly IDiskMonitor _diskMonitor;
    private readonly INetworkMonitor _networkMonitor;
    private readonly IGpuMonitor _gpuMonitor;
    private readonly IProcessMonitor _processMonitor;
    private readonly ILogger<MonitoringCoordinator> _logger;
    private readonly IServiceScopeFactory? _scopeFactory;

    private readonly SemaphoreSlim _cycleLock = new(1, 1);
    private CancellationTokenSource? _cts;
    private Task? _executionTask;
    private TimeSpan _interval = TimeSpan.FromSeconds(1);
    private PeriodicTimer? _activeTimer;
    private int _isRunning;
    private DateTime _lastHistoryPersist = DateTime.MinValue;
    private DateTime _lastPruneTime = DateTime.MinValue;

    public bool IsRunning => Volatile.Read(ref _isRunning) == 1;

    public TimeSpan Interval
    {
        get => _interval;
        set
        {
            if (value < TimeSpan.FromMilliseconds(200))
                value = TimeSpan.FromMilliseconds(200);
            _interval = value;
            if (_activeTimer != null)
            {
                try
                {
                    _activeTimer.Period = _interval;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not dynamically adjust PeriodicTimer.Period");
                }
            }
        }
    }

    public event Action<SystemOverviewSnapshot>? SystemOverviewUpdated;
    public event Action<ProcessDeltaSnapshot>? ProcessesUpdated;

    public MonitoringCoordinator(
        ICpuMonitor cpuMonitor,
        IMemoryMonitor memoryMonitor,
        IDiskMonitor diskMonitor,
        INetworkMonitor networkMonitor,
        IGpuMonitor gpuMonitor,
        IProcessMonitor processMonitor,
        ILogger<MonitoringCoordinator> logger,
        IServiceScopeFactory? scopeFactory = null)
    {
        _cpuMonitor = cpuMonitor;
        _memoryMonitor = memoryMonitor;
        _diskMonitor = diskMonitor;
        _networkMonitor = networkMonitor;
        _gpuMonitor = gpuMonitor;
        _processMonitor = processMonitor;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _isRunning, 1, 0) == 1)
        {
            _logger.LogDebug("MonitoringCoordinator is already running.");
            return Task.CompletedTask;
        }

        _logger.LogInformation("Starting MonitoringCoordinator with interval {Interval}", _interval);
        _cts = new CancellationTokenSource();
        _executionTask = Task.Run(() => RunMonitoringLoopAsync(_cts.Token), CancellationToken.None);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _isRunning, 0, 1) == 0)
        {
            return;
        }

        _logger.LogInformation("Stopping MonitoringCoordinator...");
        if (_cts != null)
        {
            _cts.Cancel();
            if (_executionTask != null)
            {
                try
                {
                    await Task.WhenAny(_executionTask, Task.Delay(TimeSpan.FromSeconds(3), cancellationToken));
                }
                catch (OperationCanceledException) { }
            }
            _cts.Dispose();
            _cts = null;
        }
        _logger.LogInformation("MonitoringCoordinator stopped.");
    }

    private async Task RunMonitoringLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_interval);
        _activeTimer = timer;

        // Immediate first refresh
        await ExecuteMonitoringCycleSafelyAsync(cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!await timer.WaitForNextTickAsync(cancellationToken))
                    break;

                await ExecuteMonitoringCycleSafelyAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in monitoring timer loop.");
            }
        }

        _activeTimer = null;
    }

    private async Task ExecuteMonitoringCycleSafelyAsync(CancellationToken cancellationToken)
    {
        // Cycle overlap guard: if previous collection is still running, skip this tick
        if (!await _cycleLock.WaitAsync(0, cancellationToken))
        {
            _logger.LogDebug("Monitoring cycle skipped due to active collection in progress.");
            return;
        }

        try
        {
            // Concurrently query hardware monitors with isolation
            var cpuTask = SafeQueryAsync(_cpuMonitor.GetSnapshotAsync, new CpuSnapshot(0, Array.Empty<double>(), 0, Environment.ProcessorCount, 1, 1, TimeSpan.Zero, DateTime.UtcNow), cancellationToken);
            var memoryTask = SafeQueryAsync(_memoryMonitor.GetSnapshotAsync, new MemorySnapshot(0, 0, 0, 0, 0, 0, 0, 0, DateTime.UtcNow), cancellationToken);
            var diskTask = SafeQueryAsync(_diskMonitor.GetSnapshotAsync, (IReadOnlyList<DiskSnapshot>)Array.Empty<DiskSnapshot>(), cancellationToken);
            var networkTask = SafeQueryAsync(_networkMonitor.GetSnapshotAsync, (IReadOnlyList<NetworkSnapshot>)Array.Empty<NetworkSnapshot>(), cancellationToken);
            var gpuTask = SafeQueryAsync(_gpuMonitor.GetSnapshotAsync, (GpuSnapshot?)null, cancellationToken);
            var processTask = SafeQueryAsync(_processMonitor.GetSnapshotAsync, new ProcessDeltaSnapshot(Array.Empty<ProcessSnapshot>(), Array.Empty<int>(), Array.Empty<int>(), DateTime.UtcNow), cancellationToken);

            await Task.WhenAll(cpuTask, memoryTask, diskTask, networkTask, gpuTask, processTask);

            var cpu = await cpuTask;
            var memory = await memoryTask;
            var disks = await diskTask;
            var networks = await networkTask;
            var gpu = await gpuTask;
            var processDelta = await processTask;

            int totalHandles = 0;
            int totalThreads = 0;
            foreach (var p in processDelta.ActiveProcesses)
            {
                totalHandles += p.HandleCount;
                totalThreads += p.ThreadCount;
            }

            var overview = new SystemOverviewSnapshot(
                Cpu: cpu,
                Memory: memory,
                Disks: disks,
                Networks: networks,
                Gpu: gpu,
                TotalProcessCount: processDelta.ActiveProcesses.Count,
                TotalThreadCount: totalThreads,
                TotalHandleCount: totalHandles,
                Timestamp: DateTime.UtcNow);

            // Publish thread-safe notifications
            try { SystemOverviewUpdated?.Invoke(overview); } catch (Exception ex) { _logger.LogWarning(ex, "Subscriber error on SystemOverviewUpdated."); }
            try { ProcessesUpdated?.Invoke(processDelta); } catch (Exception ex) { _logger.LogWarning(ex, "Subscriber error on ProcessesUpdated."); }

            // Periodically persist metrics
            if (_scopeFactory != null && (DateTime.UtcNow - _lastHistoryPersist) >= TimeSpan.FromSeconds(2))
            {
                _lastHistoryPersist = DateTime.UtcNow;
                _ = PersistHistoryRecordAsync(overview, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during monitoring collection cycle.");
        }
        finally
        {
            _cycleLock.Release();
        }
    }

    private async Task PersistHistoryRecordAsync(SystemOverviewSnapshot overview, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory!.CreateScope();
            var repo = scope.ServiceProvider.GetService<IMetricHistoryRepository>();
            if (repo == null) return;

            double diskActive = overview.Disks.Count > 0 ? overview.Disks.Max(d => d.ActiveTimePercent) : 0;
            double netMbps = overview.Networks.Sum(n => (n.SendSpeedBytesPerSec + n.ReceiveSpeedBytesPerSec) * 8.0 / 1_000_000.0);

            var record = new MetricHistoryRecord(
                overview.Timestamp,
                overview.Cpu.OverallUtilizationPercent,
                overview.Memory.UtilizationPercent,
                diskActive,
                netMbps);

            await repo.AddRecordAsync(record, ct);

            // Periodic pruning every 5 minutes
            if ((DateTime.UtcNow - _lastPruneTime) >= TimeSpan.FromMinutes(5))
            {
                _lastPruneTime = DateTime.UtcNow;
                var settingsService = scope.ServiceProvider.GetService<ISettingsService>();
                int retentionHours = settingsService?.CurrentSettings.HistoryRetentionHours ?? 24;
                await repo.PruneRecordsOlderThanAsync(DateTime.UtcNow.AddHours(-retentionHours), ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Failed to persist periodic history record.");
        }
    }

    private async Task<T> SafeQueryAsync<T>(Func<CancellationToken, ValueTask<T>> providerCall, T fallback, CancellationToken cancellationToken)
    {
        try
        {
            return await providerCall(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Monitoring provider failed for {Type}; using fallback.", typeof(T).Name);
            return fallback;
        }
    }

    public async ValueTask<SystemOverviewSnapshot> RefreshOverviewAsync(CancellationToken cancellationToken = default)
    {
        var cpu = await SafeQueryAsync(_cpuMonitor.GetSnapshotAsync, new CpuSnapshot(0, Array.Empty<double>(), 0, Environment.ProcessorCount, 1, 1, TimeSpan.Zero, DateTime.UtcNow), cancellationToken);
        var memory = await SafeQueryAsync(_memoryMonitor.GetSnapshotAsync, new MemorySnapshot(0, 0, 0, 0, 0, 0, 0, 0, DateTime.UtcNow), cancellationToken);
        var disks = await SafeQueryAsync(_diskMonitor.GetSnapshotAsync, Array.Empty<DiskSnapshot>(), cancellationToken);
        var networks = await SafeQueryAsync(_networkMonitor.GetSnapshotAsync, Array.Empty<NetworkSnapshot>(), cancellationToken);
        var gpu = await SafeQueryAsync(_gpuMonitor.GetSnapshotAsync, null, cancellationToken);

        return new SystemOverviewSnapshot(
            Cpu: cpu,
            Memory: memory,
            Disks: disks,
            Networks: networks,
            Gpu: gpu,
            TotalProcessCount: Process.GetProcesses().Length,
            TotalThreadCount: 0,
            TotalHandleCount: 0,
            Timestamp: DateTime.UtcNow);
    }

    public async ValueTask<ProcessDeltaSnapshot> RefreshProcessesAsync(CancellationToken cancellationToken = default)
    {
        return await _processMonitor.GetSnapshotAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _cycleLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
