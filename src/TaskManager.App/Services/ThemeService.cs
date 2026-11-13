using System.Windows;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Shared.Enums;

namespace TaskManager.App.Services;

public class ThemeService : IThemeService
{
    private readonly ILogger<ThemeService> _logger;
    private AppTheme _currentTheme = AppTheme.Dark;

    public AppTheme CurrentTheme => _currentTheme;
    public event Action<AppTheme>? ThemeChanged;

    public ThemeService(ILogger<ThemeService> logger)
    {
        _logger = logger;
    }

    public void SetTheme(AppTheme theme)
    {
        _currentTheme = theme;
        ApplyTheme(theme);
        ThemeChanged?.Invoke(_currentTheme);
    }

    public void ToggleTheme()
    {
        var target = _currentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
        SetTheme(target);
    }

    private void ApplyTheme(AppTheme theme)
    {
        var app = System.Windows.Application.Current;
        if (app == null) return;

        bool isDark = theme switch
        {
            AppTheme.Dark => true,
            AppTheme.Light => false,
            AppTheme.System => IsWindowsInDarkMode(),
            _ => true
        };

        var themeUri = isDark 
            ? new Uri("pack://application:,,,/TaskManager.App;component/Themes/DarkTheme.xaml", UriKind.Absolute)
            : new Uri("pack://application:,,,/TaskManager.App;component/Themes/LightTheme.xaml", UriKind.Absolute);

        var existingTheme = app.Resources.MergedDictionaries.FirstOrDefault(d => 
            d.Source != null && (d.Source.OriginalString.Contains("DarkTheme.xaml") || d.Source.OriginalString.Contains("LightTheme.xaml")));

        if (existingTheme != null)
        {
            app.Resources.MergedDictionaries.Remove(existingTheme);
        }

        app.Resources.MergedDictionaries.Insert(0, new ResourceDictionary { Source = themeUri });
        _logger.LogInformation("Theme changed to {Theme} (Resolved Dark={IsDark})", theme, isDark);
    }

    private static bool IsWindowsInDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            if (value is int lightThemeInt)
            {
                return lightThemeInt == 0;
            }
        }
        catch
        {
            // Default to dark if unable to detect
        }

        return true;
    }
}
