using TaskManager.Shared.Enums;

namespace TaskManager.Application.Common.Interfaces;

public interface IThemeService
{
    AppTheme CurrentTheme { get; }
    void SetTheme(AppTheme theme);
    void ToggleTheme();
    event Action<AppTheme>? ThemeChanged;
}
