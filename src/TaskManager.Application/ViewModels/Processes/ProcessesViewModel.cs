using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskManager.Application.Common;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Monitoring.Interfaces;
using TaskManager.Application.Monitoring.Models;

namespace TaskManager.Application.ViewModels.Processes;

public partial class ProcessesViewModel : ViewModelBase
{
    private readonly IMonitoringCoordinator _monitoringCoordinator;
    private readonly IProcessMonitor _processMonitor;
    private readonly IDispatcherService _dispatcherService;
    private readonly Dictionary<string, ProcessItemViewModel> _processMap = new();
    private readonly object _syncRoot = new();

    [ObservableProperty]
    private string _searchFilter = string.Empty;

    [ObservableProperty]
    private string _selectedTypeFilter = "All";

    [ObservableProperty]
    private ProcessItemViewModel? _selectedProcess;

    [ObservableProperty]
    private int _totalProcesses;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public ObservableCollection<ProcessItemViewModel> Processes { get; } = new();

    // Confirmation request event for UI dialog
    public Func<string, string, Task<bool>>? ConfirmActionAsync { get; set; }
    public Action<string>? ShowNotification { get; set; }
    public Action<string>? SetClipboardText { get; set; }

    public ProcessesViewModel(
        IMonitoringCoordinator monitoringCoordinator,
        IProcessMonitor processMonitor,
        IDispatcherService? dispatcherService = null)
    {
        _monitoringCoordinator = monitoringCoordinator;
        _processMonitor = processMonitor;
        _dispatcherService = dispatcherService ?? new ImmediateDispatcherService();
        Title = "Processes";

        _monitoringCoordinator.ProcessesUpdated += OnProcessesUpdated;
    }

    public override async Task InitializeAsync()
    {
        try
        {
            var delta = await _monitoringCoordinator.RefreshProcessesAsync();
            OnProcessesUpdated(delta);
        }
        catch
        {
            // Handled safely
        }
    }

    private void OnProcessesUpdated(ProcessDeltaSnapshot delta)
    {
        _dispatcherService.Invoke(() =>
        {
            lock (_syncRoot)
            {
                var incomingKeys = new HashSet<string>(delta.ActiveProcesses.Count);

                foreach (var snapshot in delta.ActiveProcesses)
                {
                    incomingKeys.Add(snapshot.ProcessKey);

                    if (_processMap.TryGetValue(snapshot.ProcessKey, out var existingItem))
                    {
                        // Differential update: update in place so selection / scroll position stays intact
                        existingItem.UpdateFromSnapshot(snapshot);
                    }
                    else
                    {
                        // New process
                        var newItem = new ProcessItemViewModel(snapshot);
                        _processMap[snapshot.ProcessKey] = newItem;
                        Processes.Add(newItem);
                    }
                }

                // Remove terminated processes
                var deadKeys = _processMap.Keys.Where(k => !incomingKeys.Contains(k)).ToList();
                foreach (var key in deadKeys)
                {
                    if (_processMap.TryGetValue(key, out var deadItem))
                    {
                        _processMap.Remove(key);
                        Processes.Remove(deadItem);
                    }
                }

                TotalProcesses = Processes.Count;
            }
        });
    }

    [RelayCommand]
    private async Task EndTaskAsync()
    {
        if (SelectedProcess == null) return;

        int pid = SelectedProcess.Id;
        string name = SelectedProcess.Name;

        bool confirm = true;
        if (ConfirmActionAsync != null)
        {
            confirm = await ConfirmActionAsync(
                $"End process '{name}' (PID: {pid})?",
                "Ending this process may cause unsaved data to be lost or system instability.");
        }

        if (!confirm) return;

        bool success = await _processMonitor.TerminateProcessAsync(pid);
        if (success)
        {
            StatusMessage = $"Process {name} ({pid}) terminated.";
            ShowNotification?.Invoke(StatusMessage);
            RemoveProcessById(pid);
        }
        else
        {
            StatusMessage = $"Unable to terminate {name} ({pid}). Access denied or system protected.";
            ShowNotification?.Invoke(StatusMessage);
        }
    }

    [RelayCommand]
    private async Task EndProcessTreeAsync()
    {
        if (SelectedProcess == null) return;

        int pid = SelectedProcess.Id;
        string name = SelectedProcess.Name;

        bool confirm = true;
        if (ConfirmActionAsync != null)
        {
            confirm = await ConfirmActionAsync(
                $"End process tree for '{name}' (PID: {pid})?",
                "This will end this process and all child processes created by it.");
        }

        if (!confirm) return;

        bool success = await _processMonitor.TerminateProcessAsync(pid);
        if (success)
        {
            StatusMessage = $"Process tree for {name} ({pid}) terminated.";
            ShowNotification?.Invoke(StatusMessage);
            RemoveProcessById(pid);
        }
        else
        {
            StatusMessage = $"Unable to terminate process tree for {name}.";
            ShowNotification?.Invoke(StatusMessage);
        }
    }

    [RelayCommand]
    private void SetPriority(string priorityName)
    {
        if (SelectedProcess == null) return;

        try
        {
            using var proc = Process.GetProcessById(SelectedProcess.Id);
            if (Enum.TryParse<ProcessPriorityClass>(priorityName, true, out var priorityClass))
            {
                proc.PriorityClass = priorityClass;
                StatusMessage = $"Priority for {SelectedProcess.Name} set to {priorityName}.";
                ShowNotification?.Invoke(StatusMessage);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to set priority: {ex.Message}";
            ShowNotification?.Invoke(StatusMessage);
        }
    }

    [RelayCommand]
    private void OpenFileLocation()
    {
        if (SelectedProcess == null || string.IsNullOrEmpty(SelectedProcess.ExecutablePath))
        {
            StatusMessage = "File location unavailable for this process.";
            ShowNotification?.Invoke(StatusMessage);
            return;
        }

        try
        {
            if (File.Exists(SelectedProcess.ExecutablePath))
            {
                Process.Start("explorer.exe", $"/select,\"{SelectedProcess.ExecutablePath}\"");
            }
            else
            {
                var dir = Path.GetDirectoryName(SelectedProcess.ExecutablePath);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                {
                    Process.Start("explorer.exe", dir);
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to open file location: {ex.Message}";
            ShowNotification?.Invoke(StatusMessage);
        }
    }

    [RelayCommand]
    private void CopyDetails()
    {
        if (SelectedProcess == null) return;

        var text = $"PID: {SelectedProcess.Id}\nName: {SelectedProcess.Name}\nDescription: {SelectedProcess.Description}\nCPU: {SelectedProcess.CpuPercent:F1}%\nMemory: {SelectedProcess.WorkingSetBytes:N0} bytes\nPath: {SelectedProcess.ExecutablePath}\nStatus: {SelectedProcess.Status}";
        SetClipboardText?.Invoke(text);
        StatusMessage = "Process details copied to clipboard.";
        ShowNotification?.Invoke(StatusMessage);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        var delta = await _monitoringCoordinator.RefreshProcessesAsync();
        OnProcessesUpdated(delta);
    }

    private void RemoveProcessById(int pid)
    {
        lock (_syncRoot)
        {
            var match = Processes.FirstOrDefault(p => p.Id == pid);
            if (match != null)
            {
                _processMap.Remove(match.ProcessKey);
                Processes.Remove(match);
                if (SelectedProcess == match)
                    SelectedProcess = null;
            }
        }
    }
}
