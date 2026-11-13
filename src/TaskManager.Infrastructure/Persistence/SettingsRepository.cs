using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Domain.Entities;
using TaskManager.Infrastructure.Data;

namespace TaskManager.Infrastructure.Persistence;

public class SettingsRepository : ISettingsRepository
{
    private readonly TaskManagerDbContext _dbContext;

    public SettingsRepository(TaskManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        var setting = await _dbContext.UserSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

        return setting?.Value;
    }

    public async Task SetValueAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.UserSettings
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

        if (existing != null)
        {
            existing.Value = value;
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }
        else
        {
            await _dbContext.UserSettings.AddAsync(new UserSettingEntity
            {
                Key = key,
                Value = value,
                UpdatedAtUtc = DateTime.UtcNow
            }, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _dbContext.UserSettings
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return list.ToDictionary(s => s.Key, s => s.Value);
    }

    public async Task SetValuesAsync(IEnumerable<KeyValuePair<string, string>> settings, CancellationToken cancellationToken = default)
    {
        var keys = settings.Select(s => s.Key).ToList();
        var existingEntities = await _dbContext.UserSettings
            .Where(s => keys.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, cancellationToken);

        foreach (var kvp in settings)
        {
            if (existingEntities.TryGetValue(kvp.Key, out var entity))
            {
                entity.Value = kvp.Value;
                entity.UpdatedAtUtc = DateTime.UtcNow;
            }
            else
            {
                await _dbContext.UserSettings.AddAsync(new UserSettingEntity
                {
                    Key = kvp.Key,
                    Value = kvp.Value,
                    UpdatedAtUtc = DateTime.UtcNow
                }, cancellationToken);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
