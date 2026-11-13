using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskManager.Application.Common;
using TaskManager.Domain.Models;

namespace TaskManager.Application.ViewModels.StartupApps;

public partial class StartupAppsViewModel : ViewModelBase
{
    [ObservableProperty]
    private StartupAppItem? _selectedStartupApp;

    public ObservableCollection<StartupAppItem> StartupApps { get; } = new();

    public StartupAppsViewModel()
    {
        Title = "Startup Apps";
        LoadSampleStartupApps();
    }

    [RelayCommand]
    private void ToggleStatus()
    {
        if (SelectedStartupApp != null)
        {
            SelectedStartupApp.Status = SelectedStartupApp.Status == "Enabled" ? "Disabled" : "Enabled";
            // Trigger refresh
            var index = StartupApps.IndexOf(SelectedStartupApp);
            var item = SelectedStartupApp;
            StartupApps.RemoveAt(index);
            StartupApps.Insert(index, item);
            SelectedStartupApp = item;
        }
    }

    private void LoadSampleStartupApps()
    {
        StartupApps.Clear();
        StartupApps.Add(new StartupAppItem("1") { Name = "Microsoft OneDrive", Publisher = "Microsoft Corporation", Status = "Enabled", StartupImpact = "Medium", Command = "OneDrive.exe /background", Location = "Startup Folder" });
        StartupApps.Add(new StartupAppItem("2") { Name = "Spotify", Publisher = "Spotify AB", Status = "Disabled", StartupImpact = "High", Command = "Spotify.exe --autostart", Location = "Registry Run" });
        StartupApps.Add(new StartupAppItem("3") { Name = "Discord", Publisher = "Discord Inc.", Status = "Enabled", StartupImpact = "High", Command = "Update.exe --processStart Discord.exe", Location = "Registry Run" });
        StartupApps.Add(new StartupAppItem("4") { Name = "Windows Security notification icon", Publisher = "Microsoft Corporation", Status = "Enabled", StartupImpact = "Low", Command = "SecurityHealthSystray.exe", Location = "Registry Run" });
    }
}
