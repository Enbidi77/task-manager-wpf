using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Settings.Models;

namespace TaskManager.Infrastructure.Services;

public class SettingsService : ISettingsService
{
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly ISettingsRepository? _directRepository;
    private readonly ILogger<SettingsService> _logger;
    private UserSettings _currentSettings = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public UserSettings CurrentSettings => _currentSettings.Clone();

    public event Action<UserSettings>? SettingsChanged;

    public SettingsService(IServiceScopeFactory scopeFactory, ILogger<SettingsService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public SettingsService(ISettingsRepository directRepository, ILogger<SettingsService> logger)
    {
        _directRepository = directRepository;
        _logger = logger;
    }

    private (ISettingsRepository Repo, IDisposable? Scope) ResolveRepository()
    {
        if (_directRepository != null)
        {
            return (_directRepository, null);
        }

        if (_scopeFactory != null)
        {
            var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
            return (repo, scope);
        }

        throw new InvalidOperationException("Neither repository nor scope factory is available.");
    }

    public async Task<UserSettings> LoadSettingsAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var (repo, scope) = ResolveRepository();
            IReadOnlyDictionary<string, string> dict;
            try
            {
                dict = await repo.GetAllAsync(cancellationToken);
            }
            finally
            {
                scope?.Dispose();
            }

            var settings = new UserSettings();

            if (dict.TryGetValue(nameof(UserSettings.Theme), out var theme) && !string.IsNullOrWhiteSpace(theme))
            {
                settings.Theme = theme;
            }

            if (dict.TryGetValue(nameof(UserSettings.RefreshIntervalMs), out var intervalStr) &&
                int.TryParse(intervalStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var interval))
            {
                settings.RefreshIntervalMs = interval;
            }

            if (dict.TryGetValue(nameof(UserSettings.ConfirmDestructiveActions), out var confirmStr) &&
                bool.TryParse(confirmStr, out var confirm))
            {
                settings.ConfirmDestructiveActions = confirm;
            }

            if (dict.TryGetValue(nameof(UserSettings.MinimizeToTray), out var trayStr) &&
                bool.TryParse(trayStr, out var tray))
            {
                settings.MinimizeToTray = tray;
            }

            if (dict.TryGetValue(nameof(UserSettings.StartMinimized), out var startMinStr) &&
                bool.TryParse(startMinStr, out var startMin))
            {
                settings.StartMinimized = startMin;
            }

            if (dict.TryGetValue(nameof(UserSettings.AlwaysOnTop), out var alwaysOnTopStr) &&
                bool.TryParse(alwaysOnTopStr, out var alwaysOnTop))
            {
                settings.AlwaysOnTop = alwaysOnTop;
            }

            if (dict.TryGetValue(nameof(UserSettings.HistoryRetentionHours), out var retentionStr) &&
                int.TryParse(retentionStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var retention))
            {
                settings.HistoryRetentionHours = retention;
            }

            if (dict.TryGetValue(nameof(UserSettings.ChartHistoryLimit), out var chartLimitStr) &&
                int.TryParse(chartLimitStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var chartLimit))
            {
                settings.ChartHistoryLimit = chartLimit;
            }

            _currentSettings = settings;
            _logger.LogInformation("UserSettings successfully loaded from repository.");
            return _currentSettings.Clone();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load user settings; falling back to default values.");
            return _currentSettings.Clone();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveSettingsAsync(UserSettings settings, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var dict = new Dictionary<string, string>
            {
                [nameof(UserSettings.Theme)] = settings.Theme,
                [nameof(UserSettings.RefreshIntervalMs)] = settings.RefreshIntervalMs.ToString(CultureInfo.InvariantCulture),
                [nameof(UserSettings.ConfirmDestructiveActions)] = settings.ConfirmDestructiveActions.ToString(),
                [nameof(UserSettings.MinimizeToTray)] = settings.MinimizeToTray.ToString(),
                [nameof(UserSettings.StartMinimized)] = settings.StartMinimized.ToString(),
                [nameof(UserSettings.AlwaysOnTop)] = settings.AlwaysOnTop.ToString(),
                [nameof(UserSettings.HistoryRetentionHours)] = settings.HistoryRetentionHours.ToString(CultureInfo.InvariantCulture),
                [nameof(UserSettings.ChartHistoryLimit)] = settings.ChartHistoryLimit.ToString(CultureInfo.InvariantCulture)
            };

            var (repo, scope) = ResolveRepository();
            try
            {
                await repo.SetValuesAsync(dict, cancellationToken);
            }
            finally
            {
                scope?.Dispose();
            }

            _currentSettings = settings.Clone();
            _logger.LogInformation("UserSettings saved successfully.");
        }
        finally
        {
            _lock.Release();
        }

        try
        {
            SettingsChanged?.Invoke(_currentSettings.Clone());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception thrown by SettingsChanged subscriber.");
        }
    }

    public async Task UpdateSettingAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        var strValue = value?.ToString() ?? string.Empty;
        var (repo, scope) = ResolveRepository();
        try
        {
            await repo.SetValueAsync(key, strValue, cancellationToken);
        }
        finally
        {
            scope?.Dispose();
        }

        await LoadSettingsAsync(cancellationToken);
    }
}
