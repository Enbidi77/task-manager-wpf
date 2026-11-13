using TaskManager.Application.Settings.Models;

namespace TaskManager.Application.Common.Interfaces;

public interface ISettingsService
{
    UserSettings CurrentSettings { get; }
    event Action<UserSettings>? SettingsChanged;

    Task<UserSettings> LoadSettingsAsync(CancellationToken cancellationToken = default);
    Task SaveSettingsAsync(UserSettings settings, CancellationToken cancellationToken = default);
    Task UpdateSettingAsync<T>(string key, T value, CancellationToken cancellationToken = default);
}
