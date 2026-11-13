namespace TaskManager.Application.Settings.Models;

public class UserSettings
{
    public string Theme { get; set; } = "Dark";
    public int RefreshIntervalMs { get; set; } = 1000;
    public bool ConfirmDestructiveActions { get; set; } = true;
    public bool MinimizeToTray { get; set; } = false;
    public bool StartMinimized { get; set; } = false;
    public bool AlwaysOnTop { get; set; } = false;
    public int HistoryRetentionHours { get; set; } = 24;
    public int ChartHistoryLimit { get; set; } = 60;

    public UserSettings Clone()
    {
        return new UserSettings
        {
            Theme = Theme,
            RefreshIntervalMs = RefreshIntervalMs,
            ConfirmDestructiveActions = ConfirmDestructiveActions,
            MinimizeToTray = MinimizeToTray,
            StartMinimized = StartMinimized,
            AlwaysOnTop = AlwaysOnTop,
            HistoryRetentionHours = HistoryRetentionHours,
            ChartHistoryLimit = ChartHistoryLimit
        };
    }
}
