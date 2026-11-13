using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Domain.Entities;
using TaskManager.Infrastructure.Data;

namespace TaskManager.Infrastructure.Persistence;

public class MetricHistoryRepository : IMetricHistoryRepository
{
    private readonly TaskManagerDbContext _dbContext;

    public MetricHistoryRepository(TaskManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddRecordAsync(MetricHistoryRecord record, CancellationToken cancellationToken = default)
    {
        await _dbContext.MetricHistory.AddAsync(record, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRecordsAsync(IEnumerable<MetricHistoryRecord> records, CancellationToken cancellationToken = default)
    {
        await _dbContext.MetricHistory.AddRangeAsync(records, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MetricHistoryRecord>> GetRecentRecordsAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MetricHistory
            .AsNoTracking()
            .OrderByDescending(r => r.Timestamp)
            .Take(count)
            .OrderBy(r => r.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MetricHistoryRecord>> GetHistoryAsync(
        DateTime fromUtc,
        DateTime toUtc,
        int? maxCount = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.MetricHistory
            .AsNoTracking()
            .Where(r => r.Timestamp >= fromUtc && r.Timestamp <= toUtc)
            .OrderBy(r => r.Timestamp);

        if (maxCount.HasValue && maxCount.Value > 0)
        {
            return await query.Take(maxCount.Value).ToListAsync(cancellationToken);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<int> PruneRecordsOlderThanAsync(DateTime thresholdUtc, CancellationToken cancellationToken = default)
    {
        var oldRecords = await _dbContext.MetricHistory
            .Where(r => r.Timestamp < thresholdUtc)
            .ToListAsync(cancellationToken);

        if (oldRecords.Count == 0)
            return 0;

        _dbContext.MetricHistory.RemoveRange(oldRecords);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return oldRecords.Count;
    }

    public async Task PurgeAllAsync(CancellationToken cancellationToken = default)
    {
        var allRecords = await _dbContext.MetricHistory.ToListAsync(cancellationToken);
        if (allRecords.Count > 0)
        {
            _dbContext.MetricHistory.RemoveRange(allRecords);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
