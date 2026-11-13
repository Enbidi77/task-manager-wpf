using FluentAssertions;
using TaskManager.Application.ViewModels.Processes;
using Xunit;

namespace TaskManager.Application.Tests;

public class ProcessesViewModelTests
{
    [Fact]
    public void EndTaskCommand_RemovesSelectedProcess()
    {
        // Arrange
        var vm = new ProcessesViewModel();
        vm.Processes.Should().NotBeEmpty();
        var initialCount = vm.Processes.Count;
        var target = vm.Processes.First();
        vm.SelectedProcess = target;

        // Act
        vm.EndTaskCommand.Execute(null);

        // Assert
        vm.Processes.Should().HaveCount(initialCount - 1);
        vm.Processes.Should().NotContain(target);
        vm.SelectedProcess.Should().BeNull();
    }

    [Fact]
    public void RefreshCommand_ReloadsProcessList()
    {
        // Arrange
        var vm = new ProcessesViewModel();
        vm.Processes.Clear();
        vm.Processes.Should().BeEmpty();

        // Act
        vm.RefreshCommand.Execute(null);

        // Assert
        vm.Processes.Should().NotBeEmpty();
    }
}
