using TaskManager.Domain.Common;

namespace TaskManager.Domain.Models;

public class ProcessModel : Entity<int>
{
    public string Name { get; set; } = string.Empty;
    public double CpuPercent { get; set; }
    public long WorkingSetBytes { get; set; }
    public double DiskReadBytesPerSec { get; set; }
    public double DiskWriteBytesPerSec { get; set; }
    public double NetworkBytesPerSec { get; set; }
    public string Status { get; set; } = "Running";
    public string UserName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ExecutablePath { get; set; }

    public ProcessModel(int processId, string name) : base(processId)
    {
        Name = name;
    }
}
