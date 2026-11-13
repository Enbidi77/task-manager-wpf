using FluentAssertions;
using Moq;
using TaskManager.Application.Common;
using TaskManager.Application.Monitoring.Interfaces;
using TaskManager.Application.Monitoring.Models;
using TaskManager.Application.ViewModels.Processes;
using Xunit;

namespace TaskManager.Application.Tests;

public class ProcessesViewModelTests
{
    private readonly Mock<IMonitoringCoordinator> _mockCoordinator = new();
    private readonly Mock<IProcessMonitor> _mockProcessMonitor = new();

    private ProcessesViewModel CreateViewModel()
    {
        var sampleProcess = new ProcessSnapshot(
            ProcessId: 42,
            StartTimeUtc: DateTime.UtcNow.AddHours(-1),
            ProcessKey: "42_123456",
            Name: "notepad",
            Description: "Notepad Editor",
            ExecutablePath: @"C:\Windows\notepad.exe",
            ParentProcessId: 100,
            CpuPercent: 1.5,
            WorkingSetBytes: 25000000,
            PrivateMemoryBytes: 15000000,
            ReadSpeedBytesPerSec: 0,
            WriteSpeedBytesPerSec: 0,
            ThreadCount: 4,
            HandleCount: 150,
            UserName: "User",
            Status: "Running",
            Timestamp: DateTime.UtcNow);

        var delta = new ProcessDeltaSnapshot(
            ActiveProcesses: new List<ProcessSnapshot> { sampleProcess },
            AddedProcessIds: new List<int> { 42 },
            TerminatedProcessIds: Array.Empty<int>(),
            Timestamp: DateTime.UtcNow);

        _mockCoordinator.Setup(c => c.RefreshProcessesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(delta);

        _mockProcessMonitor.Setup(m => m.TerminateProcessAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        return new ProcessesViewModel(
            _mockCoordinator.Object,
            _mockProcessMonitor.Object,
            new ImmediateDispatcherService());
    }

    [Fact]
    public async Task EndTaskCommand_TerminatesProcess_AndRemovesFromList()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.InitializeAsync();

        vm.Processes.Should().HaveCount(1);
        var target = vm.Processes.First();
        vm.SelectedProcess = target;

        // Act
        await vm.EndTaskCommand.ExecuteAsync(null);

        // Assert
        _mockProcessMonitor.Verify(m => m.TerminateProcessAsync(42, It.IsAny<CancellationToken>()), Times.Once);
        vm.Processes.Should().BeEmpty();
        vm.SelectedProcess.Should().BeNull();
    }

    [Fact]
    public async Task RefreshCommand_UpdatesProcessListDifferentially()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.InitializeAsync();
        vm.Processes.Should().HaveCount(1);

        var itemBefore = vm.Processes.First();
        itemBefore.CpuPercent.Should().Be(1.5);

        // Setup second delta with updated CPU on same process key
        var updatedSnapshot = new ProcessSnapshot(
            ProcessId: 42,
            StartTimeUtc: itemBefore.Id == 42 ? DateTime.UtcNow.AddHours(-1) : DateTime.UtcNow,
            ProcessKey: "42_123456",
            Name: "notepad",
            Description: "Notepad Editor",
            ExecutablePath: @"C:\Windows\notepad.exe",
            ParentProcessId: 100,
            CpuPercent: 8.5, // updated
            WorkingSetBytes: 30000000,
            PrivateMemoryBytes: 20000000,
            ReadSpeedBytesPerSec: 0,
            WriteSpeedBytesPerSec: 0,
            ThreadCount: 4,
            HandleCount: 160,
            UserName: "User",
            Status: "Running",
            Timestamp: DateTime.UtcNow);

        _mockCoordinator.Setup(c => c.RefreshProcessesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessDeltaSnapshot(
                new List<ProcessSnapshot> { updatedSnapshot },
                Array.Empty<int>(),
                Array.Empty<int>(),
                DateTime.UtcNow));

        // Act: Refresh
        await vm.RefreshCommand.ExecuteAsync(null);

        // Assert: Instance maintained, in-place update
        vm.Processes.Should().HaveCount(1);
        vm.Processes.First().Should().BeSameAs(itemBefore);
        vm.Processes.First().CpuPercent.Should().Be(8.5);
    }
}
