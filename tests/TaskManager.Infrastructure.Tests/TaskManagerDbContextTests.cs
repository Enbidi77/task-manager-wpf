using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskManager.Domain.Entities;
using TaskManager.Infrastructure.Data;
using Xunit;

namespace TaskManager.Infrastructure.Tests;

public class TaskManagerDbContextTests
{
    private DbContextOptions<TaskManagerDbContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<TaskManagerDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
    }

    [Fact]
    public async Task DbContext_CanAddAndRetrieveMetricHistoryRecord()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        await using var context = new TaskManagerDbContext(options);
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();

        var record = new MetricHistoryRecord(
            DateTime.UtcNow,
            cpuPercent: 42.5,
            memoryPercent: 68.0,
            diskActivePercent: 12.3,
            networkThroughputMbps: 15.8);

        // Act
        await context.MetricHistory.AddAsync(record);
        await context.SaveChangesAsync();

        // Assert
        var retrieved = await context.MetricHistory.FirstOrDefaultAsync();
        retrieved.Should().NotBeNull();
        retrieved!.CpuPercent.Should().Be(42.5);
        retrieved.MemoryPercent.Should().Be(68.0);
        retrieved.DiskActivePercent.Should().Be(12.3);
        retrieved.NetworkThroughputMbps.Should().Be(15.8);
    }

    [Fact]
    public async Task DbContext_CanAddAndRetrieveUserSettingEntity()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        await using var context = new TaskManagerDbContext(options);
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();

        var setting = new UserSettingEntity("Theme", "Light");

        // Act
        await context.UserSettings.AddAsync(setting);
        await context.SaveChangesAsync();

        // Assert
        var retrieved = await context.UserSettings.FindAsync("Theme");
        retrieved.Should().NotBeNull();
        retrieved!.Value.Should().Be("Light");
    }

    [Fact]
    public async Task DbContext_CanAddAndRetrieveProcessFilterEntity()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        await using var context = new TaskManagerDbContext(options);
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();

        var filter = new ProcessFilterEntity("notepad.exe", isFavorite: true, isExcluded: false, "User editor");

        // Act
        await context.ProcessFilters.AddAsync(filter);
        await context.SaveChangesAsync();

        // Assert
        var retrieved = await context.ProcessFilters.FirstOrDefaultAsync(f => f.ProcessName == "notepad.exe");
        retrieved.Should().NotBeNull();
        retrieved!.IsFavorite.Should().BeTrue();
        retrieved.Description.Should().Be("User editor");
    }
}
