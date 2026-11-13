using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskManager.Application.Common;
using TaskManager.Application.Monitoring.Interfaces;
using TaskManager.Application.Monitoring.Models;

namespace TaskManager.Application.ViewModels.Services;

public partial class ServicesViewModel : ViewModelBase
{
    private readonly IServiceMonitor _serviceMonitor;
    private readonly List<ServiceSnapshot> _allServices = new();

    [ObservableProperty]
    private string _searchFilter = string.Empty;

    [ObservableProperty]
    private string _selectedStatusFilter = "All";

    [ObservableProperty]
    private ServiceSnapshot? _selectedService;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public ObservableCollection<ServiceSnapshot> Services { get; } = new();

    public Action<string>? ShowNotification { get; set; }

    public ServicesViewModel(IServiceMonitor serviceMonitor)
    {
        _serviceMonitor = serviceMonitor;
        Title = "Services";
    }

    public override async Task InitializeAsync()
    {
        await LoadServicesAsync();
    }

    [RelayCommand]
    public async Task LoadServicesAsync()
    {
        IsBusy = true;
        try
        {
            var list = await _serviceMonitor.GetSnapshotAsync();
            _allServices.Clear();
            _allServices.AddRange(list);
            ApplyFilter();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load services: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSearchFilterChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSelectedStatusFilterChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Services.Clear();
        var query = _allServices.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchFilter))
        {
            query = query.Where(s =>
                s.ServiceName.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                s.DisplayName.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                s.Description.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedStatusFilter != "All")
        {
            query = query.Where(s => s.Status.Equals(SelectedStatusFilter, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var s in query)
        {
            Services.Add(s);
        }
    }

    [RelayCommand]
    private async Task StartServiceAsync()
    {
        if (SelectedService == null) return;

        string name = SelectedService.ServiceName;
        StatusMessage = $"Starting service {name}...";
        bool success = await _serviceMonitor.StartServiceAsync(name);
        if (success)
        {
            StatusMessage = $"Service {name} started.";
            ShowNotification?.Invoke(StatusMessage);
            await LoadServicesAsync();
        }
        else
        {
            StatusMessage = $"Failed to start {name}. Administrator privileges may be required.";
            ShowNotification?.Invoke(StatusMessage);
        }
    }

    [RelayCommand]
    private async Task StopServiceAsync()
    {
        if (SelectedService == null) return;

        string name = SelectedService.ServiceName;
        StatusMessage = $"Stopping service {name}...";
        bool success = await _serviceMonitor.StopServiceAsync(name);
        if (success)
        {
            StatusMessage = $"Service {name} stopped.";
            ShowNotification?.Invoke(StatusMessage);
            await LoadServicesAsync();
        }
        else
        {
            StatusMessage = $"Failed to stop {name}. The service might be protected or requires administrator rights.";
            ShowNotification?.Invoke(StatusMessage);
        }
    }

    [RelayCommand]
    private async Task RestartServiceAsync()
    {
        if (SelectedService == null) return;

        string name = SelectedService.ServiceName;
        StatusMessage = $"Restarting service {name}...";
        bool success = await _serviceMonitor.RestartServiceAsync(name);
        if (success)
        {
            StatusMessage = $"Service {name} restarted successfully.";
            ShowNotification?.Invoke(StatusMessage);
            await LoadServicesAsync();
        }
        else
        {
            StatusMessage = $"Failed to restart {name}.";
            ShowNotification?.Invoke(StatusMessage);
        }
    }
}
