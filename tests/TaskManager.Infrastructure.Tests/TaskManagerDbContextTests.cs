using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskManager.Infrastructure.Data;
using Xunit;

namespace TaskManager.Infrastructure.Tests;

public class TaskManagerDbContextTests
{
    [Fact]
    public async Task DbContext_CanAddAndRetrievePerformanceSnapshot()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TaskManagerDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        await using var context = new TaskManagerDbContext(options);
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();

        var snapshot = new PerformanceSnapshotEntity
        {
            Timestamp = DateTime.UtcNow,
            CpuPercent = 35.5,
            MemoryPercent = 60.2,
            DiskPercent = 10.0,
            NetworkMbps = 25.0
        };

        // Act
        context.PerformanceSnapshots.Add(snapshot);
        await context.SaveChangesAsync();

        // Assert
        var retrieved = await context.PerformanceSnapshots.FirstOrDefaultAsync();
        retrieved.Should().NotBeNull();
        retrieved!.CpuPercent.Should().Be(35.5);
    }
}
