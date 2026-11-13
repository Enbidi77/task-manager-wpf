using FluentAssertions;
using Moq;
using TaskManager.Application.Common;
using TaskManager.Application.Monitoring.Interfaces;
using TaskManager.Application.Monitoring.Models;
using TaskManager.Application.ViewModels.Dashboard;
using TaskManager.Application.ViewModels.Performance;
using TaskManager.Application.ViewModels.Services;
using TaskManager.Application.ViewModels.StartupApps;
using TaskManager.Application.ViewModels.Users;
using Xunit;

namespace TaskManager.Application.Tests;

public class FeatureViewModelsTests
{
    private readonly Mock<IMonitoringCoordinator> _mockCoordinator = new();
    private readonly Mock<IServiceMonitor> _mockServiceMonitor = new();
    private readonly Mock<IStartupApplicationMonitor> _mockStartupMonitor = new();
    private readonly Mock<IUserSessionMonitor> _mockUserMonitor = new();

    [Fact]
    public async Task DashboardViewModel_Initialize_PopulatesMetricsAndHistory()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var overview = new SystemOverviewSnapshot(
            Cpu: new CpuSnapshot(85.0, new[] { 85.0 }, 3.5, 8, 1, 4, TimeSpan.FromHours(2), now),
            Memory: new MemorySnapshot(16000000000, 2000000000, 14000000000, 87.5, 15000000000, 20000000000, 0, 0, now),
            Disks: new List<DiskSnapshot> { new("C:", "SSD", 15.0, 1000, 2000, 0.5, 500000000, 200000000, now) },
            Networks: new List<NetworkSnapshot> { new("Ethernet", "GbE", 10000, 20000, 5000, 15000, "192.168.1.5", now) },
            Gpu: new GpuSnapshot("NVIDIA RTX", 30.0, 50.0, 8000000000, 8000000000, 2000000000, now),
            TotalProcessCount: 150,
            TotalThreadCount: 2000,
            TotalHandleCount: 50000,
            Timestamp: now);

        _mockCoordinator.Setup(c => c.RefreshOverviewAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(overview);

        var vm = new DashboardViewModel(_mockCoordinator.Object, new ImmediateDispatcherService());

        // Act
        await vm.InitializeAsync();

        // Assert
        vm.CpuUsagePercent.Should().Be(85.0);
        vm.MemoryUsagePercent.Should().Be(87.5);
        vm.IsCpuHighAlert.Should().BeTrue();
        vm.IsMemoryHighAlert.Should().BeTrue();
        vm.ProcessCount.Should().Be(150);
        vm.ThreadCount.Should().Be(2000);
        vm.HandleCount.Should().Be(50000);
        vm.CpuHistory.Should().NotBeEmpty();
        vm.MemoryHistory.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PerformanceViewModel_SelectTab_UpdatesSelectedTabProperty()
    {
        // Arrange
        var vm = new PerformanceViewModel(_mockCoordinator.Object, new ImmediateDispatcherService());
        vm.SelectedTab.Should().Be("CPU");

        // Act
        vm.SelectTabCommand.Execute("Memory");

        // Assert
        vm.SelectedTab.Should().Be("Memory");

        // Act 2
        vm.SelectTabCommand.Execute("Disk");

        // Assert 2
        vm.SelectedTab.Should().Be("Disk");
    }

    [Fact]
    public async Task ServicesViewModel_LoadServices_FiltersByName()
    {
        // Arrange
        var sampleServices = new List<ServiceSnapshot>
        {
            new("wuauserv", "Windows Update", "Running", "Manual", null, "Updates", DateTime.UtcNow),
            new("Spooler", "Print Spooler", "Running", "Automatic", null, "Printing", DateTime.UtcNow),
            new("Themes", "Themes", "Running", "Automatic", null, "Themes", DateTime.UtcNow)
        };

        _mockServiceMonitor.Setup(m => m.GetSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sampleServices);

        var vm = new ServicesViewModel(_mockServiceMonitor.Object);
        await vm.InitializeAsync();

        // Assert all loaded
        vm.Services.Should().HaveCount(3);

        // Act: Filter by "spool"
        vm.SearchFilter = "spool";

        // Assert: Only print spooler matches
        vm.Services.Should().HaveCount(1);
        vm.Services.First().ServiceName.Should().Be("Spooler");
    }

    [Fact]
    public async Task StartupAppsViewModel_ToggleStatus_CallsMonitor()
    {
        // Arrange
        var sampleApps = new List<StartupAppSnapshot>
        {
            new("HKCU:TestApp", "TestApp", "Test Publisher", "Enabled", "Low", "test.exe", "HKCU", DateTime.UtcNow)
        };

        _mockStartupMonitor.Setup(m => m.GetSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sampleApps);
        _mockStartupMonitor.Setup(m => m.SetStartupAppEnabledAsync("HKCU:TestApp", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var vm = new StartupAppsViewModel(_mockStartupMonitor.Object);
        await vm.InitializeAsync();

        vm.StartupApps.Should().HaveCount(1);
        vm.SelectedStartupApp = vm.StartupApps.First();

        // Act
        await vm.ToggleStatusCommand.ExecuteAsync(null);

        // Assert
        _mockStartupMonitor.Verify(m => m.SetStartupAppEnabledAsync("HKCU:TestApp", false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UsersViewModel_LoadSessions_PopulatesUserSessions()
    {
        // Arrange
        var sessions = new List<UserSessionSnapshot>
        {
            new(1, "TestUser", "Console", "Active", "Local", 45, 0.0, 100000000, DateTime.UtcNow)
        };

        _mockUserMonitor.Setup(m => m.GetSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        var vm = new UsersViewModel(_mockUserMonitor.Object);

        // Act
        await vm.InitializeAsync();

        // Assert
        vm.UserSessions.Should().HaveCount(1);
        vm.UserSessions.First().UserName.Should().Be("TestUser");
    }
}
