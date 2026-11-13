using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskManager.Domain.Entities;
using TaskManager.Infrastructure.Data;
using TaskManager.Infrastructure.Persistence;
using Xunit;

namespace TaskManager.Infrastructure.Tests;

public class MetricHistoryRepositoryTests
{
    private async Task<TaskManagerDbContext> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<TaskManagerDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        var context = new TaskManagerDbContext(options);
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    [Fact]
    public async Task AddRecordAsync_And_GetRecentRecordsAsync_ReturnsOrderedRecords()
    {
        // Arrange
        await using var context = await CreateContextAsync();
        var repo = new MetricHistoryRepository(context);

        var now = DateTime.UtcNow;
        var r1 = new MetricHistoryRecord(now.AddSeconds(-20), 10.0, 50.0, 5.0, 1.0);
        var r2 = new MetricHistoryRecord(now.AddSeconds(-10), 20.0, 55.0, 10.0, 2.0);
        var r3 = new MetricHistoryRecord(now, 30.0, 60.0, 15.0, 3.0);

        // Act
        await repo.AddRecordsAsync(new[] { r1, r2, r3 });
        var recent = await repo.GetRecentRecordsAsync(2);

        // Assert
        recent.Should().HaveCount(2);
        recent[0].CpuPercent.Should().Be(20.0);
        recent[1].CpuPercent.Should().Be(30.0);
    }

    [Fact]
    public async Task GetHistoryAsync_BoundedByDateRange()
    {
        // Arrange
        await using var context = await CreateContextAsync();
        var repo = new MetricHistoryRepository(context);

        var baseTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var r1 = new MetricHistoryRecord(baseTime.AddHours(-2), 10.0, 50.0, 5.0, 1.0);
        var r2 = new MetricHistoryRecord(baseTime, 20.0, 55.0, 10.0, 2.0);
        var r3 = new MetricHistoryRecord(baseTime.AddHours(2), 30.0, 60.0, 15.0, 3.0);

        await repo.AddRecordsAsync(new[] { r1, r2, r3 });

        // Act
        var history = await repo.GetHistoryAsync(baseTime.AddHours(-1), baseTime.AddHours(1));

        // Assert
        history.Should().HaveCount(1);
        history[0].CpuPercent.Should().Be(20.0);
    }

    [Fact]
    public async Task PruneRecordsOlderThanAsync_DeletesOldRecordsOnly()
    {
        // Arrange
        await using var context = await CreateContextAsync();
        var repo = new MetricHistoryRepository(context);

        var now = DateTime.UtcNow;
        var old1 = new MetricHistoryRecord(now.AddHours(-10), 10.0, 50.0, 5.0, 1.0);
        var old2 = new MetricHistoryRecord(now.AddHours(-5), 20.0, 55.0, 10.0, 2.0);
        var current = new MetricHistoryRecord(now, 30.0, 60.0, 15.0, 3.0);

        await repo.AddRecordsAsync(new[] { old1, old2, current });

        // Act
        var pruned = await repo.PruneRecordsOlderThanAsync(now.AddHours(-2));
        var remaining = await repo.GetRecentRecordsAsync(10);

        // Assert
        pruned.Should().Be(2);
        remaining.Should().HaveCount(1);
        remaining[0].CpuPercent.Should().Be(30.0);
    }

    [Fact]
    public async Task PurgeAllAsync_RemovesAllRecords()
    {
        // Arrange
        await using var context = await CreateContextAsync();
        var repo = new MetricHistoryRepository(context);

        var now = DateTime.UtcNow;
        await repo.AddRecordAsync(new MetricHistoryRecord(now, 10.0, 50.0, 5.0, 1.0));
        await repo.AddRecordAsync(new MetricHistoryRecord(now.AddSeconds(1), 20.0, 50.0, 5.0, 1.0));

        // Act
        await repo.PurgeAllAsync();
        var remaining = await repo.GetRecentRecordsAsync(10);

        // Assert
        remaining.Should().BeEmpty();
    }
}
