using TaskManager.Domain.Entities;

namespace TaskManager.Application.Common.Interfaces;

public interface IMetricHistoryRepository
{
    Task AddRecordAsync(MetricHistoryRecord record, CancellationToken cancellationToken = default);
    Task AddRecordsAsync(IEnumerable<MetricHistoryRecord> records, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MetricHistoryRecord>> GetRecentRecordsAsync(int count, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MetricHistoryRecord>> GetHistoryAsync(DateTime fromUtc, DateTime toUtc, int? maxCount = null, CancellationToken cancellationToken = default);
    Task<int> PruneRecordsOlderThanAsync(DateTime thresholdUtc, CancellationToken cancellationToken = default);
    Task PurgeAllAsync(CancellationToken cancellationToken = default);
}
