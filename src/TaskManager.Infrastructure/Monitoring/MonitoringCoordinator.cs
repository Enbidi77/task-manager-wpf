using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TaskManager.Application.Monitoring.Interfaces;
using TaskManager.Application.Monitoring.Models;

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

    private readonly SemaphoreSlim _cycleLock = new(1, 1);
    private CancellationTokenSource? _cts;
    private Task? _executionTask;
    private TimeSpan _interval = TimeSpan.FromSeconds(1);
    private int _isRunning;

    public bool IsRunning => Volatile.Read(ref _isRunning) == 1;

    public TimeSpan Interval
    {
        get => _interval;
        set
        {
            if (value < TimeSpan.FromMilliseconds(200))
                value = TimeSpan.FromMilliseconds(200);
            _interval = value;
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
        ILogger<MonitoringCoordinator> logger)
    {
        _cpuMonitor = cpuMonitor;
        _memoryMonitor = memoryMonitor;
        _diskMonitor = diskMonitor;
        _networkMonitor = networkMonitor;
        _gpuMonitor = gpuMonitor;
        _processMonitor = processMonitor;
        _logger = logger;
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
