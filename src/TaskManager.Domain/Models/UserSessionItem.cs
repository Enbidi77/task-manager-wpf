using TaskManager.Domain.Common;

namespace TaskManager.Domain.Models;

public class UserSessionItem : Entity<int>
{
    public string UserName { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public string State { get; set; } = "Active";
    public double CpuPercent { get; set; }
    public long MemoryBytes { get; set; }
    public double DiskBytesPerSec { get; set; }
    public double NetworkBytesPerSec { get; set; }

    public UserSessionItem(int sessionId) : base(sessionId)
    {
    }
}
