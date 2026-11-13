using TaskManager.Domain.Common;

namespace TaskManager.Domain.Models;

public class ServiceItem : Entity<string>
{
    public string DisplayName { get; set; } = string.Empty;
    public string Status { get; set; } = "Stopped";
    public string StartupType { get; set; } = "Manual";
    public int? ProcessId { get; set; }
    public string? Description { get; set; }

    public ServiceItem(string serviceName) : base(serviceName)
    {
    }
}
