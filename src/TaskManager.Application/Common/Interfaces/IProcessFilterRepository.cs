using TaskManager.Domain.Entities;

namespace TaskManager.Application.Common.Interfaces;

public interface IProcessFilterRepository
{
    Task<IReadOnlyList<ProcessFilterEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProcessFilterEntity?> GetByNameAsync(string processName, CancellationToken cancellationToken = default);
    Task AddOrUpdateAsync(ProcessFilterEntity filter, CancellationToken cancellationToken = default);
    Task RemoveAsync(string processName, CancellationToken cancellationToken = default);
}
