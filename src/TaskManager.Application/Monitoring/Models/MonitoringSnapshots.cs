namespace TaskManager.Application.Monitoring.Models;

public record CpuSnapshot(
    double OverallUtilizationPercent,
    IReadOnlyList<double> CoreUtilizations,
    double SpeedGhz,
    int LogicalProcessorCount,
    int SocketCount,
    int CoreCount,
    TimeSpan UpTime,
    DateTime Timestamp);

public record MemorySnapshot(
    long TotalPhysicalBytes,
    long AvailablePhysicalBytes,
    long UsedPhysicalBytes,
    double UtilizationPercent,
    long CommittedBytes,
    long CommitLimitBytes,
    long PagedPoolBytes,
    long NonPagedPoolBytes,
    DateTime Timestamp);

public record DiskSnapshot(
    string Name,
    string DiskModel,
    double ActiveTimePercent,
    double ReadSpeedBytesPerSec,
    double WriteSpeedBytesPerSec,
    double ResponseTimeMs,
    long TotalSizeBytes,
    long AvailableFreeBytes,
    DateTime Timestamp);

public record NetworkSnapshot(
    string AdapterName,
    string Description,
    double SendSpeedBytesPerSec,
    double ReceiveSpeedBytesPerSec,
    long TotalBytesSent,
    long TotalBytesReceived,
    string Ipv4Address,
    DateTime Timestamp);

public record GpuSnapshot(
    string Name,
    double UtilizationPercent,
    double TemperatureCelsius,
    long DedicatedMemoryBytes,
    long SharedMemoryBytes,
    long DedicatedMemoryUsedBytes,
    DateTime Timestamp);

public record ProcessSnapshot(
    int ProcessId,
    DateTime StartTimeUtc,
    string ProcessKey,
    string Name,
    string Description,
    string ExecutablePath,
    int? ParentProcessId,
    double CpuPercent,
    long WorkingSetBytes,
    long PrivateMemoryBytes,
    double ReadSpeedBytesPerSec,
    double WriteSpeedBytesPerSec,
    int ThreadCount,
    int HandleCount,
    string UserName,
    string Status,
    DateTime Timestamp);

public record ProcessDeltaSnapshot(
    IReadOnlyList<ProcessSnapshot> ActiveProcesses,
    IReadOnlyList<int> AddedProcessIds,
    IReadOnlyList<int> TerminatedProcessIds,
    DateTime Timestamp);

public record SystemOverviewSnapshot(
    CpuSnapshot Cpu,
    MemorySnapshot Memory,
    IReadOnlyList<DiskSnapshot> Disks,
    IReadOnlyList<NetworkSnapshot> Networks,
    GpuSnapshot? Gpu,
    int TotalProcessCount,
    int TotalThreadCount,
    int TotalHandleCount,
    DateTime Timestamp);

public record ServiceSnapshot(
    string ServiceName,
    string DisplayName,
    string Status,
    string StartupType,
    int? ProcessId,
    string Description,
    DateTime Timestamp);

public record StartupAppSnapshot(
    string Id,
    string Name,
    string Publisher,
    string Status,
    string StartupImpact,
    string Command,
    string Location,
    DateTime Timestamp);
