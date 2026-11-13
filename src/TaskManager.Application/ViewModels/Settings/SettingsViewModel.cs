using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskManager.Application.Common;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Diagnostics.Models;
using TaskManager.Application.Monitoring.Interfaces;
using TaskManager.Application.Settings.Models;
using TaskManager.Shared.Enums;

namespace TaskManager.Application.ViewModels.Settings;

public record RefreshIntervalOption(int Milliseconds, string Label);
public record RetentionOption(int Hours, string Label);

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IThemeService _themeService;
    private readonly IMonitoringCoordinator _monitoringCoordinator;
    private readonly IMetricHistoryRepository _metricHistoryRepository;
    private readonly IDiagnosticLogService _diagnosticLogService;
    private readonly IDispatcherService _dispatcherService;

    private UserSettings _savedSettings = new();

    [ObservableProperty]
    private int _selectedTabIndex;

    // --- General Settings Properties ---
    [ObservableProperty]
    private AppTheme _selectedTheme;

    [ObservableProperty]
    private int _selectedRefreshIntervalMs = 1000;

    [ObservableProperty]
    private bool _confirmDestructiveActions = true;

    [ObservableProperty]
    private bool _minimizeToTray;

    [ObservableProperty]
    private bool _startMinimized;

    [ObservableProperty]
    private bool _alwaysOnTop;

    [ObservableProperty]
    private int _historyRetentionHours = 24;

    [ObservableProperty]
    private int _chartHistoryLimit = 60;

    [ObservableProperty]
    private bool _isDirty;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private string _appVersion = "1.0.0 (.NET 10 LTS)";

    public IReadOnlyList<RefreshIntervalOption> AvailableIntervals { get; } = new List<RefreshIntervalOption>
    {
        new(500, "Fast (500 ms)"),
        new(1000, "Normal (1000 ms)"),
        new(2000, "Medium (2000 ms)"),
        new(5000, "Slow (5000 ms)")
    };

    public IReadOnlyList<RetentionOption> AvailableRetentions { get; } = new List<RetentionOption>
    {
        new(6, "6 hours"),
        new(12, "12 hours"),
        new(24, "24 hours (1 day)"),
        new(72, "72 hours (3 days)"),
        new(168, "168 hours (7 days)")
    };

    // --- Diagnostics / Log Viewer Properties ---
    public ObservableCollection<LogEntryModel> LogEntries { get; } = new();

    public IReadOnlyList<string> AvailableLogLevels { get; } = new List<string>
    {
        "All",
        "Information",
        "Warning",
        "Error",
        "Debug"
    };

    [ObservableProperty]
    private string _selectedLogLevel = "All";

    [ObservableProperty]
    private string _logSearchQuery = string.Empty;

    [ObservableProperty]
    private bool _isLoadingLogs;

    [ObservableProperty]
    private int _totalLogCount;

    public SettingsViewModel(
        ISettingsService settingsService,
        IThemeService themeService,
        IMonitoringCoordinator monitoringCoordinator,
        IMetricHistoryRepository metricHistoryRepository,
        IDiagnosticLogService diagnosticLogService,
        IDispatcherService? dispatcherService = null)
    {
        _settingsService = settingsService;
        _themeService = themeService;
        _monitoringCoordinator = monitoringCoordinator;
        _metricHistoryRepository = metricHistoryRepository;
        _diagnosticLogService = diagnosticLogService;
        _dispatcherService = dispatcherService ?? new ImmediateDispatcherService();

        Title = "Settings";
        _selectedTheme = _themeService.CurrentTheme;
        _themeService.ThemeChanged += OnThemeChanged;
    }

    public override async Task InitializeAsync()
    {
        await LoadSettingsFromServiceAsync();
        await RefreshLogsAsync();
    }

    public async Task LoadSettingsFromServiceAsync()
    {
        var settings = await _settingsService.LoadSettingsAsync();
        _savedSettings = settings.Clone();

        _dispatcherService.Invoke(() =>
        {
            if (Enum.TryParse<AppTheme>(settings.Theme, true, out var theme))
            {
                SelectedTheme = theme;
            }
            else
            {
                SelectedTheme = AppTheme.Dark;
            }

            SelectedRefreshIntervalMs = settings.RefreshIntervalMs;
            ConfirmDestructiveActions = settings.ConfirmDestructiveActions;
            MinimizeToTray = settings.MinimizeToTray;
            StartMinimized = settings.StartMinimized;
            AlwaysOnTop = settings.AlwaysOnTop;
            HistoryRetentionHours = settings.HistoryRetentionHours;
            ChartHistoryLimit = settings.ChartHistoryLimit;

            IsDirty = false;
        });
    }

    private void OnThemeChanged(AppTheme theme)
    {
        _dispatcherService.Invoke(() =>
        {
            SelectedTheme = theme;
        });
    }

    partial void OnSelectedThemeChanged(AppTheme value) => CheckDirty();
    partial void OnSelectedRefreshIntervalMsChanged(int value) => CheckDirty();
    partial void OnConfirmDestructiveActionsChanged(bool value) => CheckDirty();
    partial void OnMinimizeToTrayChanged(bool value) => CheckDirty();
    partial void OnStartMinimizedChanged(bool value) => CheckDirty();
    partial void OnAlwaysOnTopChanged(bool value) => CheckDirty();
    partial void OnHistoryRetentionHoursChanged(int value) => CheckDirty();
    partial void OnChartHistoryLimitChanged(int value) => CheckDirty();

    private void CheckDirty()
    {
        IsDirty = SelectedTheme.ToString() != _savedSettings.Theme ||
                  SelectedRefreshIntervalMs != _savedSettings.RefreshIntervalMs ||
                  ConfirmDestructiveActions != _savedSettings.ConfirmDestructiveActions ||
                  MinimizeToTray != _savedSettings.MinimizeToTray ||
                  StartMinimized != _savedSettings.StartMinimized ||
                  AlwaysOnTop != _savedSettings.AlwaysOnTop ||
                  HistoryRetentionHours != _savedSettings.HistoryRetentionHours ||
                  ChartHistoryLimit != _savedSettings.ChartHistoryLimit;
    }

    [RelayCommand]
    private void SwitchTheme(AppTheme theme)
    {
        SelectedTheme = theme;
        _themeService.SetTheme(theme);
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        _themeService.ToggleTheme();
        SelectedTheme = _themeService.CurrentTheme;
    }

    [RelayCommand]
    public async Task SaveSettingsAsync()
    {
        var settings = new UserSettings
        {
            Theme = SelectedTheme.ToString(),
            RefreshIntervalMs = SelectedRefreshIntervalMs,
            ConfirmDestructiveActions = ConfirmDestructiveActions,
            MinimizeToTray = MinimizeToTray,
            StartMinimized = StartMinimized,
            AlwaysOnTop = AlwaysOnTop,
            HistoryRetentionHours = HistoryRetentionHours,
            ChartHistoryLimit = ChartHistoryLimit
        };

        await _settingsService.SaveSettingsAsync(settings);
        _savedSettings = settings.Clone();

        _monitoringCoordinator.Interval = TimeSpan.FromMilliseconds(SelectedRefreshIntervalMs);
        _themeService.SetTheme(SelectedTheme);

        IsDirty = false;
        StatusMessage = "Settings successfully saved.";
    }

    [RelayCommand]
    public void RevertSettings()
    {
        if (Enum.TryParse<AppTheme>(_savedSettings.Theme, true, out var theme))
        {
            SelectedTheme = theme;
        }

        SelectedRefreshIntervalMs = _savedSettings.RefreshIntervalMs;
        ConfirmDestructiveActions = _savedSettings.ConfirmDestructiveActions;
        MinimizeToTray = _savedSettings.MinimizeToTray;
        StartMinimized = _savedSettings.StartMinimized;
        AlwaysOnTop = _savedSettings.AlwaysOnTop;
        HistoryRetentionHours = _savedSettings.HistoryRetentionHours;
        ChartHistoryLimit = _savedSettings.ChartHistoryLimit;

        _themeService.SetTheme(SelectedTheme);
        _monitoringCoordinator.Interval = TimeSpan.FromMilliseconds(SelectedRefreshIntervalMs);

        IsDirty = false;
        StatusMessage = "Settings reverted to saved values.";
    }

    [RelayCommand]
    public async Task PurgeHistoryAsync()
    {
        await _metricHistoryRepository.PurgeAllAsync();
        StatusMessage = "Metric history cache purged successfully.";
    }

    [RelayCommand]
    public async Task RefreshLogsAsync()
    {
        IsLoadingLogs = true;
        try
        {
            var logs = await _diagnosticLogService.GetLogEntriesAsync(SelectedLogLevel, LogSearchQuery);
            _dispatcherService.Invoke(() =>
            {
                LogEntries.Clear();
                foreach (var log in logs)
                {
                    LogEntries.Add(log);
                }
                TotalLogCount = LogEntries.Count;
            });
        }
        finally
        {
            IsLoadingLogs = false;
        }
    }

    [RelayCommand]
    public async Task ClearLogsAsync()
    {
        await _diagnosticLogService.ClearLogsAsync();
        await RefreshLogsAsync();
        StatusMessage = "Diagnostic log files cleared.";
    }

    [RelayCommand]
    public void OpenLogDirectory()
    {
        _diagnosticLogService.OpenLogDirectory();
    }

    partial void OnSelectedLogLevelChanged(string value)
    {
        _ = RefreshLogsAsync();
    }
}
