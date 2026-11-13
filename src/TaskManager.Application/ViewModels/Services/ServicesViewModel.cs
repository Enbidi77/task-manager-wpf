using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskManager.Application.Common;
using TaskManager.Domain.Models;

namespace TaskManager.Application.ViewModels.Services;

public partial class ServicesViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _searchFilter = string.Empty;

    [ObservableProperty]
    private ServiceItem? _selectedService;

    public ObservableCollection<ServiceItem> Services { get; } = new();

    public ServicesViewModel()
    {
        Title = "Services";
        LoadSampleServices();
    }

    [RelayCommand]
    private void StartService()
    {
        if (SelectedService != null)
        {
            SelectedService.Status = "Running";
        }
    }

    [RelayCommand]
    private void StopService()
    {
        if (SelectedService != null)
        {
            SelectedService.Status = "Stopped";
        }
    }

    [RelayCommand]
    private void RestartService()
    {
        if (SelectedService != null)
        {
            SelectedService.Status = "Running";
        }
    }

    private void LoadSampleServices()
    {
        Services.Clear();
        Services.Add(new ServiceItem("wuauserv") { DisplayName = "Windows Update", Status = "Running", StartupType = "Manual", ProcessId = 1120, Description = "Enables the detection, download, and installation of updates for Windows and other programs." });
        Services.Add(new ServiceItem("Themes") { DisplayName = "Themes", Status = "Running", StartupType = "Automatic", ProcessId = 1532, Description = "Provides user experience theme management." });
        Services.Add(new ServiceItem("Spooler") { DisplayName = "Print Spooler", Status = "Running", StartupType = "Automatic", ProcessId = 2340, Description = "Spools print jobs and handles interaction with the printer." });
        Services.Add(new ServiceItem("BITS") { DisplayName = "Background Intelligent Transfer Service", Status = "Running", StartupType = "Automatic (Delayed)", ProcessId = 1890, Description = "Transfers files in the background using idle network bandwidth." });
        Services.Add(new ServiceItem("W32Time") { DisplayName = "Windows Time", Status = "Stopped", StartupType = "Manual", Description = "Maintains date and time synchronization on all clients and servers in the network." });
    }
}
