using CommunityToolkit.Mvvm.ComponentModel;
using TaskManager.Shared.Enums;

namespace TaskManager.Application.Common.Interfaces;

public interface INavigationService
{
    ObservableObject? CurrentViewModel { get; }
    NavigationPage CurrentPage { get; }
    bool CanGoBack { get; }

    void NavigateTo<TViewModel>() where TViewModel : ObservableObject;
    void NavigateTo(Type viewModelType);
    void NavigateTo(NavigationPage page);
    void GoBack();

    event Action<ObservableObject?>? CurrentViewModelChanged;
}
