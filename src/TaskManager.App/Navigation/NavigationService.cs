using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskManager.Application.Common;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.ViewModels.Dashboard;
using TaskManager.Application.ViewModels.Performance;
using TaskManager.Application.ViewModels.Processes;
using TaskManager.Application.ViewModels.Services;
using TaskManager.Application.ViewModels.Settings;
using TaskManager.Application.ViewModels.StartupApps;
using TaskManager.Application.ViewModels.Users;
using TaskManager.Shared.Enums;

namespace TaskManager.App.Navigation;

public class NavigationService : ObservableObject, INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NavigationService> _logger;
    private readonly Stack<(NavigationPage Page, Type ViewModelType)> _backStack = new();
    private static readonly Dictionary<NavigationPage, Type> PageToViewModelMap = new()
    {
        { NavigationPage.Dashboard, typeof(DashboardViewModel) },
        { NavigationPage.Processes, typeof(ProcessesViewModel) },
        { NavigationPage.Performance, typeof(PerformanceViewModel) },
        { NavigationPage.Services, typeof(ServicesViewModel) },
        { NavigationPage.StartupApps, typeof(StartupAppsViewModel) },
        { NavigationPage.Users, typeof(UsersViewModel) },
        { NavigationPage.Settings, typeof(SettingsViewModel) },
    };

    private ObservableObject? _currentViewModel;
    private NavigationPage _currentPage = NavigationPage.Dashboard;

    public ObservableObject? CurrentViewModel
    {
        get => _currentViewModel;
        private set
        {
            if (SetProperty(ref _currentViewModel, value))
            {
                CurrentViewModelChanged?.Invoke(_currentViewModel);
            }
        }
    }

    public NavigationPage CurrentPage
    {
        get => _currentPage;
        private set => SetProperty(ref _currentPage, value);
    }

    public bool CanGoBack => _backStack.Count > 0;

    public event Action<ObservableObject?>? CurrentViewModelChanged;

    public NavigationService(IServiceProvider serviceProvider, ILogger<NavigationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void NavigateTo<TViewModel>() where TViewModel : ObservableObject
    {
        NavigateTo(typeof(TViewModel));
    }

    public void NavigateTo(Type viewModelType)
    {
        var page = PageToViewModelMap.FirstOrDefault(x => x.Value == viewModelType).Key;
        NavigateInternal(page, viewModelType);
    }

    public void NavigateTo(NavigationPage page)
    {
        if (PageToViewModelMap.TryGetValue(page, out var vmType))
        {
            NavigateInternal(page, vmType);
        }
        else
        {
            _logger.LogWarning("No ViewModel mapped for page: {Page}", page);
        }
    }

    public void GoBack()
    {
        if (_backStack.TryPop(out var previous))
        {
            NavigateInternal(previous.Page, previous.ViewModelType, isBackNavigation: true);
        }
    }

    private void NavigateInternal(NavigationPage page, Type viewModelType, bool isBackNavigation = false)
    {
        if (!isBackNavigation && _currentViewModel != null)
        {
            _backStack.Push((_currentPage, _currentViewModel.GetType()));
        }

        if (_currentViewModel is ViewModelBase prevVm)
        {
            prevVm.OnNavigatedFrom();
        }

        _logger.LogInformation("Navigating to {Page} ({ViewModelType})", page, viewModelType.Name);

        var nextViewModel = (ObservableObject)_serviceProvider.GetRequiredService(viewModelType);
        CurrentPage = page;
        CurrentViewModel = nextViewModel;
        OnPropertyChanged(nameof(CanGoBack));

        if (nextViewModel is ViewModelBase nextVm)
        {
            nextVm.OnNavigatedTo();
            _ = nextVm.InitializeAsync();
        }
    }
}
