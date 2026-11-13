using TaskManager.Domain.Common;

namespace TaskManager.Domain.Models;

public class StartupAppItem : Entity<string>
{
    public string Name { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string Status { get; set; } = "Enabled";
    public string StartupImpact { get; set; } = "Medium";
    public string Command { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;

    public StartupAppItem(string id) : base(id)
    {
    }
}
