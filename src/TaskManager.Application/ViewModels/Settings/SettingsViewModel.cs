using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskManager.Application.Common;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Shared.Enums;

namespace TaskManager.Application.ViewModels.Settings;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly IThemeService _themeService;

    [ObservableProperty]
    private AppTheme _selectedTheme;

    [ObservableProperty]
    private string _updateSpeed = "Normal (1s)";

    [ObservableProperty]
    private bool _alwaysOnTop;

    [ObservableProperty]
    private bool _minimizeOnUse;

    [ObservableProperty]
    private bool _hideWhenMinimized;

    [ObservableProperty]
    private string _appVersion = "1.0.0 (NET 10.0)";

    public SettingsViewModel(IThemeService themeService)
    {
        _themeService = themeService;
        Title = "Settings";
        _selectedTheme = _themeService.CurrentTheme;
        _themeService.ThemeChanged += OnThemeChanged;
    }

    private void OnThemeChanged(AppTheme theme)
    {
        SelectedTheme = theme;
    }

    [RelayCommand]
    private void SwitchTheme(AppTheme theme)
    {
        _themeService.SetTheme(theme);
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        _themeService.ToggleTheme();
    }
}
