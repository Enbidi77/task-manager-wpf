using FluentAssertions;
using Moq;
using TaskManager.Application.Common;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Diagnostics.Models;
using TaskManager.Application.Monitoring.Interfaces;
using TaskManager.Application.Settings.Models;
using TaskManager.Application.ViewModels.Settings;
using TaskManager.Shared.Enums;
using Xunit;

namespace TaskManager.Application.Tests;

public class SettingsViewModelTests
{
    private readonly Mock<ISettingsService> _mockSettingsService = new();
    private readonly Mock<IThemeService> _mockThemeService = new();
    private readonly Mock<IMonitoringCoordinator> _mockCoordinator = new();
    private readonly Mock<IMetricHistoryRepository> _mockHistoryRepo = new();
    private readonly Mock<IDiagnosticLogService> _mockLogService = new();

    private SettingsViewModel CreateViewModel()
    {
        _mockThemeService.Setup(t => t.CurrentTheme).Returns(AppTheme.Dark);

        var initialSettings = new UserSettings
        {
            Theme = "Dark",
            RefreshIntervalMs = 1000,
            ConfirmDestructiveActions = true,
            AlwaysOnTop = false
        };

        _mockSettingsService.Setup(s => s.LoadSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(initialSettings);

        _mockLogService.Setup(l => l.GetLogEntriesAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LogEntryModel>
            {
                new(DateTime.UtcNow, "Information", "Test log 1")
            });

        return new SettingsViewModel(
            _mockSettingsService.Object,
            _mockThemeService.Object,
            _mockCoordinator.Object,
            _mockHistoryRepo.Object,
            _mockLogService.Object,
            new ImmediateDispatcherService());
    }

    [Fact]
    public async Task InitializeAsync_LoadsSettingsAndLogs()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        await vm.InitializeAsync();

        // Assert
        vm.SelectedTheme.Should().Be(AppTheme.Dark);
        vm.SelectedRefreshIntervalMs.Should().Be(1000);
        vm.ConfirmDestructiveActions.Should().BeTrue();
        vm.AlwaysOnTop.Should().BeFalse();
        vm.IsDirty.Should().BeFalse();
        vm.LogEntries.Should().HaveCount(1);
    }

    [Fact]
    public async Task ChangingProperty_SetsIsDirtyTrue()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.InitializeAsync();
        vm.IsDirty.Should().BeFalse();

        // Act
        vm.AlwaysOnTop = true;

        // Assert
        vm.IsDirty.Should().BeTrue();
    }

    [Fact]
    public async Task SaveSettingsAsync_PersistsAndClearsDirty()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.InitializeAsync();
        vm.AlwaysOnTop = true;
        vm.SelectedRefreshIntervalMs = 500;
        vm.IsDirty.Should().BeTrue();

        // Act
        await vm.SaveSettingsAsync();

        // Assert
        _mockSettingsService.Verify(s => s.SaveSettingsAsync(
            It.Is<UserSettings>(u => u.AlwaysOnTop == true && u.RefreshIntervalMs == 500),
            It.IsAny<CancellationToken>()), Times.Once);

        vm.IsDirty.Should().BeFalse();
        vm.StatusMessage.Should().Contain("successfully");
    }

    [Fact]
    public async Task RevertSettings_RestoresSavedValuesAndClearsDirty()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.InitializeAsync();
        vm.AlwaysOnTop = true;
        vm.SelectedRefreshIntervalMs = 5000;
        vm.IsDirty.Should().BeTrue();

        // Act
        vm.RevertSettings();

        // Assert
        vm.AlwaysOnTop.Should().BeFalse();
        vm.SelectedRefreshIntervalMs.Should().Be(1000);
        vm.IsDirty.Should().BeFalse();
    }

    [Fact]
    public async Task PurgeHistoryAsync_CallsMetricHistoryRepository()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        await vm.PurgeHistoryAsync();

        // Assert
        _mockHistoryRepo.Verify(r => r.PurgeAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        vm.StatusMessage.Should().Contain("purged");
    }

    [Fact]
    public async Task ClearLogsAsync_InvokesDiagnosticLogService()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        await vm.ClearLogsAsync();

        // Assert
        _mockLogService.Verify(l => l.ClearLogsAsync(It.IsAny<CancellationToken>()), Times.Once);
        vm.StatusMessage.Should().Contain("cleared");
    }
}
