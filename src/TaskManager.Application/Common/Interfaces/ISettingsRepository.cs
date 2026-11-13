namespace TaskManager.Application.Common.Interfaces;

public interface ISettingsRepository
{
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);
    Task SetValueAsync(string key, string value, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken cancellationToken = default);
    Task SetValuesAsync(IEnumerable<KeyValuePair<string, string>> settings, CancellationToken cancellationToken = default);
}
