using CommunityToolkit.Mvvm.ComponentModel;
using TaskManager.Application.Common;

namespace TaskManager.Application.ViewModels.Performance;

public partial class PerformanceViewModel : ViewModelBase
{
    [ObservableProperty]
    private double _currentCpuUsage = 28.4;

    [ObservableProperty]
    private double _cpuSpeedGhz = 3.60;

    [ObservableProperty]
    private int _cpuProcesses = 192;

    [ObservableProperty]
    private int _cpuThreads = 2950;

    [ObservableProperty]
    private int _cpuHandles = 98120;

    [ObservableProperty]
    private double _memoryInUseGb = 9.4;

    [ObservableProperty]
    private double _memoryAvailableGb = 6.6;

    [ObservableProperty]
    private double _memoryTotalGb = 16.0;

    [ObservableProperty]
    private double _diskActivePercent = 8.5;

    [ObservableProperty]
    private double _diskReadSpeedKbps = 120.5;

    [ObservableProperty]
    private double _diskWriteSpeedKbps = 450.2;

    [ObservableProperty]
    private double _networkSendKbps = 32.4;

    [ObservableProperty]
    private double _networkReceiveKbps = 1420.8;

    [ObservableProperty]
    private string _selectedMetric = "CPU";

    public PerformanceViewModel()
    {
        Title = "Performance";
    }
}
