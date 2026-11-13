using TaskManager.Application.Monitoring.Models;

namespace TaskManager.Application.Monitoring.Interfaces;

public interface ICpuMonitor
{
    ValueTask<CpuSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}

public interface IMemoryMonitor
{
    ValueTask<MemorySnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}

public interface IDiskMonitor
{
    ValueTask<IReadOnlyList<DiskSnapshot>> GetSnapshotAsync(CancellationToken cancellationToken = default);
}

public interface INetworkMonitor
{
    ValueTask<IReadOnlyList<NetworkSnapshot>> GetSnapshotAsync(CancellationToken cancellationToken = default);
}

public interface IGpuMonitor
{
    ValueTask<GpuSnapshot?> GetSnapshotAsync(CancellationToken cancellationToken = default);
}

public interface IProcessMonitor
{
    ValueTask<ProcessDeltaSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
    ValueTask<bool> TerminateProcessAsync(int processId, CancellationToken cancellationToken = default);
}

public interface IServiceMonitor
{
    ValueTask<IReadOnlyList<ServiceSnapshot>> GetSnapshotAsync(CancellationToken cancellationToken = default);
    ValueTask<bool> StartServiceAsync(string serviceName, CancellationToken cancellationToken = default);
    ValueTask<bool> StopServiceAsync(string serviceName, CancellationToken cancellationToken = default);
    ValueTask<bool> RestartServiceAsync(string serviceName, CancellationToken cancellationToken = default);
}

public interface IStartupApplicationMonitor
{
    ValueTask<IReadOnlyList<StartupAppSnapshot>> GetSnapshotAsync(CancellationToken cancellationToken = default);
    ValueTask<bool> SetStartupAppEnabledAsync(string id, bool enabled, CancellationToken cancellationToken = default);
}

public interface IMonitoringCoordinator
{
    bool IsRunning { get; }
    TimeSpan Interval { get; set; }

    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);

    event Action<SystemOverviewSnapshot>? SystemOverviewUpdated;
    event Action<ProcessDeltaSnapshot>? ProcessesUpdated;

    ValueTask<SystemOverviewSnapshot> RefreshOverviewAsync(CancellationToken cancellationToken = default);
    ValueTask<ProcessDeltaSnapshot> RefreshProcessesAsync(CancellationToken cancellationToken = default);
}
