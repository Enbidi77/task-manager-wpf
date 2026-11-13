using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManager.App.Navigation;
using TaskManager.Application.ViewModels.Dashboard;
using TaskManager.Application.ViewModels.Processes;
using TaskManager.Shared.Enums;
using Xunit;

namespace TaskManager.App.Tests;

public class NavigationServiceTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider = new();
    private readonly Mock<ILogger<NavigationService>> _mockLogger = new();

    [Fact]
    public void NavigateTo_ChangesCurrentViewModelAndPage()
    {
        // Arrange
        var dashboardVm = new DashboardViewModel();
        var processesVm = new ProcessesViewModel();

        _mockServiceProvider.Setup(sp => sp.GetService(typeof(DashboardViewModel))).Returns(dashboardVm);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(ProcessesViewModel))).Returns(processesVm);

        var navService = new NavigationService(_mockServiceProvider.Object, _mockLogger.Object);

        // Act
        navService.NavigateTo(NavigationPage.Dashboard);

        // Assert
        navService.CurrentPage.Should().Be(NavigationPage.Dashboard);
        navService.CurrentViewModel.Should().Be(dashboardVm);
        navService.CanGoBack.Should().BeFalse();

        // Act 2: Navigate to Processes
        navService.NavigateTo(NavigationPage.Processes);

        // Assert 2
        navService.CurrentPage.Should().Be(NavigationPage.Processes);
        navService.CurrentViewModel.Should().Be(processesVm);
        navService.CanGoBack.Should().BeTrue();

        // Act 3: Go back
        navService.GoBack();

        // Assert 3
        navService.CurrentPage.Should().Be(NavigationPage.Dashboard);
        navService.CurrentViewModel.Should().Be(dashboardVm);
        navService.CanGoBack.Should().BeFalse();
    }
}
