using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskManager.Infrastructure.Data;
using TaskManager.Infrastructure.Persistence;
using Xunit;

namespace TaskManager.Infrastructure.Tests;

public class SettingsRepositoryTests
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
    public async Task SetValueAsync_And_GetValueAsync_PersistsValue()
    {
        // Arrange
        await using var context = await CreateContextAsync();
        var repo = new SettingsRepository(context);

        // Act
        await repo.SetValueAsync("Theme", "Dark");
        var value = await repo.GetValueAsync("Theme");

        // Assert
        value.Should().Be("Dark");
    }

    [Fact]
    public async Task SetValueAsync_OverwritesExistingKey()
    {
        // Arrange
        await using var context = await CreateContextAsync();
        var repo = new SettingsRepository(context);

        // Act
        await repo.SetValueAsync("RefreshIntervalMs", "1000");
        await repo.SetValueAsync("RefreshIntervalMs", "500");
        var value = await repo.GetValueAsync("RefreshIntervalMs");

        // Assert
        value.Should().Be("500");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllStoredSettings()
    {
        // Arrange
        await using var context = await CreateContextAsync();
        var repo = new SettingsRepository(context);

        await repo.SetValueAsync("Theme", "Light");
        await repo.SetValueAsync("AlwaysOnTop", "True");

        // Act
        var all = await repo.GetAllAsync();

        // Assert
        all.Should().HaveCount(2);
        all["Theme"].Should().Be("Light");
        all["AlwaysOnTop"].Should().Be("True");
    }

    [Fact]
    public async Task SetValuesAsync_BulkInsertsAndUpdates()
    {
        // Arrange
        await using var context = await CreateContextAsync();
        var repo = new SettingsRepository(context);

        await repo.SetValueAsync("Key1", "Initial");

        var batch = new Dictionary<string, string>
        {
            ["Key1"] = "Updated",
            ["Key2"] = "New"
        };

        // Act
        await repo.SetValuesAsync(batch);
        var all = await repo.GetAllAsync();

        // Assert
        all.Should().HaveCount(2);
        all["Key1"].Should().Be("Updated");
        all["Key2"].Should().Be("New");
    }
}
