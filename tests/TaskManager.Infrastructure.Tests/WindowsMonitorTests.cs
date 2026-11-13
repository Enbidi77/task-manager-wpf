using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManager.Infrastructure.Monitoring;
using Xunit;

namespace TaskManager.Infrastructure.Tests;

public class WindowsMonitorTests
{
    [Fact]
    public async Task WindowsCpuMonitor_ReturnsValidSnapshot()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<WindowsCpuMonitor>>();
        var monitor = new WindowsCpuMonitor(mockLogger.Object);

        // Act
        var snapshot = await monitor.GetSnapshotAsync();

        // Assert
        snapshot.Should().NotBeNull();
        snapshot.LogicalProcessorCount.Should().BeGreaterThan(0);
        snapshot.CoreCount.Should().BeGreaterThan(0);
        snapshot.CoreUtilizations.Should().HaveCount(snapshot.LogicalProcessorCount);
        snapshot.SpeedGhz.Should().BeGreaterThan(0.0);
        snapshot.UpTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task WindowsMemoryMonitor_ReturnsValidSnapshot()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<WindowsMemoryMonitor>>();
        var monitor = new WindowsMemoryMonitor(mockLogger.Object);

        // Act
        var snapshot = await monitor.GetSnapshotAsync();

        // Assert
        snapshot.Should().NotBeNull();
        snapshot.TotalPhysicalBytes.Should().BeGreaterThan(0);
        snapshot.AvailablePhysicalBytes.Should().BeGreaterThan(0);
        snapshot.UsedPhysicalBytes.Should().BeGreaterThan(0);
        snapshot.UtilizationPercent.Should().BeInRange(0.0, 100.0);
        snapshot.CommittedBytes.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task WindowsProcessMonitor_EnumeratesCurrentProcesses()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<WindowsProcessMonitor>>();
        var monitor = new WindowsProcessMonitor(mockLogger.Object);

        // Act
        var delta = await monitor.GetSnapshotAsync();

        // Assert
        delta.Should().NotBeNull();
        delta.ActiveProcesses.Should().NotBeEmpty();
        delta.ActiveProcesses.Should().Contain(p => p.Name.Length > 0);
        delta.ActiveProcesses.Should().OnlyContain(p => p.ProcessKey.Length > 0);
    }

    [Fact]
    public async Task WindowsStartupApplicationMonitor_ScansWithoutCrashing()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<WindowsStartupApplicationMonitor>>();
        var monitor = new WindowsStartupApplicationMonitor(mockLogger.Object);

        // Act
        var snapshot = await monitor.GetSnapshotAsync();

        // Assert
        snapshot.Should().NotBeNull();
        foreach (var item in snapshot)
        {
            item.Id.Should().NotBeNullOrWhiteSpace();
            item.Name.Should().NotBeNullOrWhiteSpace();
            item.Location.Should().NotBeNullOrWhiteSpace();
        }
    }
}
