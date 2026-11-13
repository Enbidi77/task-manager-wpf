namespace TaskManager.Application.Monitoring.Interfaces;

public record UserSessionSnapshot(
    int SessionId,
    string UserName,
    string SessionName,
    string State,
    string ClientName,
    int ProcessCount,
    double CpuPercent,
    long MemoryBytes,
    DateTime Timestamp);

public interface IUserSessionMonitor
{
    ValueTask<IReadOnlyList<UserSessionSnapshot>> GetSnapshotAsync(CancellationToken cancellationToken = default);
    ValueTask<bool> DisconnectSessionAsync(int sessionId, CancellationToken cancellationToken = default);
}
