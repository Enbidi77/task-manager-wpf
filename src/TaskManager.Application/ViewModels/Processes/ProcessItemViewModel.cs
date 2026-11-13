using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using TaskManager.Application.Monitoring.Models;

namespace TaskManager.Application.ViewModels.Processes;

public partial class ProcessItemViewModel : ObservableObject
{
    public int Id { get; }
    public string ProcessKey { get; }

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _executablePath = string.Empty;

    [ObservableProperty]
    private int? _parentProcessId;

    [ObservableProperty]
    private double _cpuPercent;

    [ObservableProperty]
    private long _workingSetBytes;

    [ObservableProperty]
    private long _privateMemoryBytes;

    [ObservableProperty]
    private double _diskSpeedBytesPerSec;

    [ObservableProperty]
    private double _networkSpeedBytesPerSec;

    [ObservableProperty]
    private int _threadCount;

    [ObservableProperty]
    private int _handleCount;

    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private string _status = "Running";

    [ObservableProperty]
    private string _architecture = "x64";

    [ObservableProperty]
    private string _processType = "Background";

    public ProcessItemViewModel(ProcessSnapshot snapshot)
    {
        Id = snapshot.ProcessId;
        ProcessKey = snapshot.ProcessKey;
        UpdateFromSnapshot(snapshot);
    }

    public void UpdateFromSnapshot(ProcessSnapshot snapshot)
    {
        Name = snapshot.Name;
        Description = string.IsNullOrWhiteSpace(snapshot.Description) ? snapshot.Name : snapshot.Description;
        ExecutablePath = snapshot.ExecutablePath;
        ParentProcessId = snapshot.ParentProcessId;
        CpuPercent = snapshot.CpuPercent;
        WorkingSetBytes = snapshot.WorkingSetBytes;
        PrivateMemoryBytes = snapshot.PrivateMemoryBytes;
        DiskSpeedBytesPerSec = snapshot.ReadSpeedBytesPerSec + snapshot.WriteSpeedBytesPerSec;
        ThreadCount = snapshot.ThreadCount;
        HandleCount = snapshot.HandleCount;
        UserName = snapshot.UserName;
        Status = snapshot.Status;

        // Categorization
        if (ExecutablePath.Contains(@"\Windows\", StringComparison.OrdinalIgnoreCase) ||
            ExecutablePath.Contains(@"\System32\", StringComparison.OrdinalIgnoreCase))
        {
            ProcessType = "Windows";
        }
        else if (ThreadCount > 1 && !string.IsNullOrEmpty(ExecutablePath) &&
                 (ExecutablePath.Contains(@"\Program Files", StringComparison.OrdinalIgnoreCase) ||
                  ExecutablePath.Contains(@"\AppData\", StringComparison.OrdinalIgnoreCase)))
        {
            ProcessType = "App";
        }
        else
        {
            ProcessType = "Background";
        }

        Architecture = Environment.Is64BitOperatingSystem ? "x64" : "x86";
    }
}
