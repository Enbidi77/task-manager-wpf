using FluentAssertions;
using Moq;
using TaskManager.Application.Common;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.ViewModels.Dashboard;
using TaskManager.Application.ViewModels.Shell;
using TaskManager.Shared.Enums;
using Xunit;

namespace TaskManager.Application.Tests;

public class ShellViewModelTests
{
    private readonly Mock<INavigationService> _mockNavService = new();
    private readonly Mock<IThemeService> _mockThemeService = new();

    [Fact]
    public void ShellViewModel_InitializesWithNavigationItems()
    {
        // Arrange & Act
        _mockThemeService.SetupGet(t => t.CurrentTheme).Returns(AppTheme.Dark);
        var vm = new ShellViewModel(_mockNavService.Object, _mockThemeService.Object);

        // Assert
        vm.MainNavigationItems.Should().HaveCount(6);
        vm.FooterNavigationItems.Should().HaveCount(1);
        vm.SelectedNavigationItem.Should().NotBeNull();
        vm.SelectedNavigationItem!.Page.Should().Be(NavigationPage.Dashboard);
        vm.IsSidebarExpanded.Should().BeTrue();
    }

    [Fact]
    public void ToggleSidebarCommand_TogglesIsSidebarExpanded()
    {
        // Arrange
        _mockThemeService.SetupGet(t => t.CurrentTheme).Returns(AppTheme.Dark);
        var vm = new ShellViewModel(_mockNavService.Object, _mockThemeService.Object);

        // Act
        vm.ToggleSidebarCommand.Execute(null);

        // Assert
        vm.IsSidebarExpanded.Should().BeFalse();

        // Act again
        vm.ToggleSidebarCommand.Execute(null);

        // Assert
        vm.IsSidebarExpanded.Should().BeTrue();
    }

    [Fact]
    public void NavigateCommand_CallsNavigationService_WithSelectedPage()
    {
        // Arrange
        _mockThemeService.SetupGet(t => t.CurrentTheme).Returns(AppTheme.Dark);
        var vm = new ShellViewModel(_mockNavService.Object, _mockThemeService.Object);
        var processesItem = vm.MainNavigationItems.First(x => x.Page == NavigationPage.Processes);

        // Act
        vm.NavigateCommand.Execute(processesItem);

        // Assert
        _mockNavService.Verify(n => n.NavigateTo(NavigationPage.Processes), Times.Once);
        vm.SelectedNavigationItem.Should().Be(processesItem);
    }

    [Fact]
    public void ToggleThemeCommand_CallsThemeService()
    {
        // Arrange
        _mockThemeService.SetupGet(t => t.CurrentTheme).Returns(AppTheme.Dark);
        var vm = new ShellViewModel(_mockNavService.Object, _mockThemeService.Object);

        // Act
        vm.ToggleThemeCommand.Execute(null);

        // Assert
        _mockThemeService.Verify(t => t.ToggleTheme(), Times.Once);
    }
}
