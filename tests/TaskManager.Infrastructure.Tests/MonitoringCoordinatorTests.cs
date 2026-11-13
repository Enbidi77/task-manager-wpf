using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManager.Application.Monitoring.Interfaces;
using TaskManager.Application.Monitoring.Models;
using TaskManager.Infrastructure.Monitoring;
using Xunit;

namespace TaskManager.Infrastructure.Tests;

public class MonitoringCoordinatorTests
{
    private readonly Mock<ICpuMonitor> _mockCpu = new();
    private readonly Mock<IMemoryMonitor> _mockMemory = new();
    private readonly Mock<IDiskMonitor> _mockDisk = new();
    private readonly Mock<INetworkMonitor> _mockNetwork = new();
    private readonly Mock<IGpuMonitor> _mockGpu = new();
    private readonly Mock<IProcessMonitor> _mockProcess = new();
    private readonly Mock<ILogger<MonitoringCoordinator>> _mockLogger = new();

    private MonitoringCoordinator CreateCoordinator()
    {
        var now = DateTime.UtcNow;
        _mockCpu.Setup(m => m.GetSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CpuSnapshot(15.0, new[] { 15.0 }, 3.2, 1, 1, 1, TimeSpan.FromHours(1), now));

        _mockMemory.Setup(m => m.GetSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemorySnapshot(16000000000, 8000000000, 8000000000, 50.0, 10000000000, 20000000000, 0, 0, now));

        _mockDisk.Setup(m => m.GetSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DiskSnapshot>
            {
                new("C:", "NVMe SSD", 5.0, 1000, 2000, 0.5, 500000000000, 250000000000, now)
            });

        _mockNetwork.Setup(m => m.GetSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NetworkSnapshot>
            {
                new("Ethernet", "GbE Controller", 5000, 15000, 1000000, 5000000, "192.168.1.100", now)
            });

        _mockGpu.Setup(m => m.GetSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GpuSnapshot("NVIDIA RTX", 20.0, 55.0, 8000000000, 8000000000, 2000000000, now));

        _mockProcess.Setup(m => m.GetSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessDeltaSnapshot(new List<ProcessSnapshot>(), Array.Empty<int>(), Array.Empty<int>(), now));

        return new MonitoringCoordinator(
            _mockCpu.Object,
            _mockMemory.Object,
            _mockDisk.Object,
            _mockNetwork.Object,
            _mockGpu.Object,
            _mockProcess.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task StartAndStopAsync_UpdatesRunningState()
    {
        // Arrange
        await using var coordinator = CreateCoordinator();
        coordinator.Interval = TimeSpan.FromMilliseconds(500);

        // Act & Assert 1: Initial state
        coordinator.IsRunning.Should().BeFalse();

        // Act 2: Start
        await coordinator.StartAsync();
        coordinator.IsRunning.Should().BeTrue();

        // Act 3: Stop
        await coordinator.StopAsync();
        coordinator.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshOverviewAsync_AggregatesMonitorsCorrectly()
    {
        // Arrange
        await using var coordinator = CreateCoordinator();

        // Act
        var overview = await coordinator.RefreshOverviewAsync();

        // Assert
        overview.Should().NotBeNull();
        overview.Cpu.OverallUtilizationPercent.Should().Be(15.0);
        overview.Memory.UtilizationPercent.Should().Be(50.0);
        overview.Disks.Should().HaveCount(1);
        overview.Networks.Should().HaveCount(1);
        overview.Gpu.Should().NotBeNull();
        overview.Gpu!.Name.Should().Be("NVIDIA RTX");
    }

    [Fact]
    public async Task RefreshOverviewAsync_IsolatesProviderExceptions_UsingFallback()
    {
        // Arrange
        await using var coordinator = CreateCoordinator();
        // Make GPU monitor throw exception
        _mockGpu.Setup(m => m.GetSnapshotAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("GPU driver fault"));

        // Act
        var overview = await coordinator.RefreshOverviewAsync();

        // Assert: overview still returned successfully, GPU falls back to null
        overview.Should().NotBeNull();
        overview.Gpu.Should().BeNull();
        overview.Cpu.OverallUtilizationPercent.Should().Be(15.0);
        overview.Memory.UtilizationPercent.Should().Be(50.0);
    }
}
