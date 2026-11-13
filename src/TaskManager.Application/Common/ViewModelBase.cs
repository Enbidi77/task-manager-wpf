using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskManager.Application.Common;

public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public virtual Task InitializeAsync() => Task.CompletedTask;
    public virtual void OnNavigatedTo() { }
    public virtual void OnNavigatedFrom() { }
}
