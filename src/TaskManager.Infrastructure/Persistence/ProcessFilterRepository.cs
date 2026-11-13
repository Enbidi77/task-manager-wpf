using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Domain.Entities;
using TaskManager.Infrastructure.Data;

namespace TaskManager.Infrastructure.Persistence;

public class ProcessFilterRepository : IProcessFilterRepository
{
    private readonly TaskManagerDbContext _dbContext;

    public ProcessFilterRepository(TaskManagerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ProcessFilterEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProcessFilters
            .AsNoTracking()
            .OrderBy(f => f.ProcessName)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProcessFilterEntity?> GetByNameAsync(string processName, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProcessFilters
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.ProcessName.ToLower() == processName.ToLower(), cancellationToken);
    }

    public async Task AddOrUpdateAsync(ProcessFilterEntity filter, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.ProcessFilters
            .FirstOrDefaultAsync(f => f.ProcessName.ToLower() == filter.ProcessName.ToLower(), cancellationToken);

        if (existing != null)
        {
            existing.IsFavorite = filter.IsFavorite;
            existing.IsExcluded = filter.IsExcluded;
            existing.Description = filter.Description;
        }
        else
        {
            await _dbContext.ProcessFilters.AddAsync(filter, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(string processName, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.ProcessFilters
            .FirstOrDefaultAsync(f => f.ProcessName.ToLower() == processName.ToLower(), cancellationToken);

        if (existing != null)
        {
            _dbContext.ProcessFilters.Remove(existing);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
