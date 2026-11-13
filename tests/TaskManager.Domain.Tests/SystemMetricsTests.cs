using FluentAssertions;
using TaskManager.Domain.Models;
using Xunit;

namespace TaskManager.Domain.Tests;

public class SystemMetricsTests
{
    [Fact]
    public void MemoryUsagePercent_CalculatesCorrectly_WhenTotalMemoryGreaterThanZero()
    {
        // Arrange
        var metrics = new SystemMetrics
        {
            TotalPhysicalMemoryBytes = 16_000_000_000,
            AvailableMemoryBytes = 8_000_000_000
        };

        // Act & Assert
        metrics.MemoryUsagePercent.Should().BeApproximately(50.0, 0.01);
    }

    [Fact]
    public void MemoryUsagePercent_ReturnsZero_WhenTotalMemoryIsZero()
    {
        // Arrange
        var metrics = new SystemMetrics
        {
            TotalPhysicalMemoryBytes = 0,
            AvailableMemoryBytes = 0
        };

        // Act & Assert
        metrics.MemoryUsagePercent.Should().Be(0.0);
    }
}
