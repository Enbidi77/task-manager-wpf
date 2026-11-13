using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskManager.Application.Common;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Shared.Enums;

namespace TaskManager.Application.ViewModels.Shell;

public partial class ShellViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IThemeService _themeService;

    [ObservableProperty]
    private bool _isSidebarExpanded = true;

    [ObservableProperty]
    private NavigationItem? _selectedNavigationItem;

    [ObservableProperty]
    private ObservableObject? _currentViewModel;

    [ObservableProperty]
    private AppTheme _currentTheme;

    public ObservableCollection<NavigationItem> MainNavigationItems { get; } = new();
    public ObservableCollection<NavigationItem> FooterNavigationItems { get; } = new();

    public ShellViewModel(INavigationService navigationService, IThemeService themeService)
    {
        _navigationService = navigationService;
        _themeService = themeService;
        Title = "Task Manager";
        CurrentTheme = _themeService.CurrentTheme;

        _navigationService.CurrentViewModelChanged += HandleNavigationChanged;
        _themeService.ThemeChanged += OnThemeChanged;

        InitializeNavigationItems();
    }

    private void InitializeNavigationItems()
    {
        // Fluent Segoe Fluent Icons / Segoe MDL2 Assets glyphs
        MainNavigationItems.Add(new NavigationItem("Dashboard", "\uE80F", NavigationPage.Dashboard));
        MainNavigationItems.Add(new NavigationItem("Processes", "\uE71D", NavigationPage.Processes));
        MainNavigationItems.Add(new NavigationItem("Performance", "\uE9D9", NavigationPage.Performance));
        MainNavigationItems.Add(new NavigationItem("Services", "\uE713", NavigationPage.Services));
        MainNavigationItems.Add(new NavigationItem("Startup Apps", "\uE7B5", NavigationPage.StartupApps));
        MainNavigationItems.Add(new NavigationItem("Users", "\uE779", NavigationPage.Users));

        FooterNavigationItems.Add(new NavigationItem("Settings", "\uE713", NavigationPage.Settings));

        // Default selection
        SelectedNavigationItem = MainNavigationItems[0];
    }

    private void HandleNavigationChanged(ObservableObject? viewModel)
    {
        CurrentViewModel = viewModel;
        UpdateSelectedItemForPage(_navigationService.CurrentPage);
    }

    private void OnThemeChanged(AppTheme theme)
    {
        CurrentTheme = theme;
    }

    private void UpdateSelectedItemForPage(NavigationPage page)
    {
        foreach (var item in MainNavigationItems)
        {
            item.IsSelected = item.Page == page;
            if (item.IsSelected)
                SelectedNavigationItem = item;
        }

        foreach (var item in FooterNavigationItems)
        {
            item.IsSelected = item.Page == page;
            if (item.IsSelected)
                SelectedNavigationItem = item;
        }

        OnPropertyChanged(nameof(SelectedNavigationItem));
    }

    [RelayCommand]
    private void Navigate(NavigationItem? item)
    {
        if (item == null) return;
        SelectedNavigationItem = item;
        _navigationService.NavigateTo(item.Page);
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarExpanded = !IsSidebarExpanded;
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        _themeService.ToggleTheme();
    }
}
