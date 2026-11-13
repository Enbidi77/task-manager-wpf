using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskManager.Application.Common;
using TaskManager.Application.Monitoring.Interfaces;
using TaskManager.Application.Monitoring.Models;

namespace TaskManager.Application.ViewModels.StartupApps;

public partial class StartupAppsViewModel : ViewModelBase
{
    private readonly IStartupApplicationMonitor _startupMonitor;
    private readonly List<StartupAppSnapshot> _allApps = new();

    [ObservableProperty]
    private string _searchFilter = string.Empty;

    [ObservableProperty]
    private StartupAppSnapshot? _selectedStartupApp;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public ObservableCollection<StartupAppSnapshot> StartupApps { get; } = new();

    public Action<string>? ShowNotification { get; set; }

    public StartupAppsViewModel(IStartupApplicationMonitor startupMonitor)
    {
        _startupMonitor = startupMonitor;
        Title = "Startup Apps";
    }

    public override async Task InitializeAsync()
    {
        await LoadStartupAppsAsync();
    }

    [RelayCommand]
    public async Task LoadStartupAppsAsync()
    {
        IsBusy = true;
        try
        {
            var list = await _startupMonitor.GetSnapshotAsync();
            _allApps.Clear();
            _allApps.AddRange(list);
            ApplyFilter();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load startup applications: {ex.Message}";
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

    private void ApplyFilter()
    {
        StartupApps.Clear();
        var query = _allApps.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchFilter))
        {
            query = query.Where(a =>
                a.Name.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                a.Publisher.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                a.Command.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var app in query)
        {
            StartupApps.Add(app);
        }
    }

    [RelayCommand]
    private async Task ToggleStatusAsync()
    {
        if (SelectedStartupApp == null) return;

        bool targetStatus = SelectedStartupApp.Status != "Enabled";
        bool success = await _startupMonitor.SetStartupAppEnabledAsync(SelectedStartupApp.Id, targetStatus);
        if (success)
        {
            StatusMessage = $"Startup status toggled for {SelectedStartupApp.Name}.";
            ShowNotification?.Invoke(StatusMessage);
            await LoadStartupAppsAsync();
        }
    }

    [RelayCommand]
    private void OpenFileLocation()
    {
        if (SelectedStartupApp == null || string.IsNullOrWhiteSpace(SelectedStartupApp.Command)) return;

        try
        {
            string cleanPath = SelectedStartupApp.Command.Trim('\"');
            int exeIndex = cleanPath.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            if (exeIndex > 0) cleanPath = cleanPath.Substring(0, exeIndex + 4);

            if (File.Exists(cleanPath))
            {
                Process.Start("explorer.exe", $"/select,\"{cleanPath}\"");
            }
            else
            {
                var dir = Path.GetDirectoryName(cleanPath);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                {
                    Process.Start("explorer.exe", dir);
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not open file location: {ex.Message}";
            ShowNotification?.Invoke(StatusMessage);
        }
    }
}
