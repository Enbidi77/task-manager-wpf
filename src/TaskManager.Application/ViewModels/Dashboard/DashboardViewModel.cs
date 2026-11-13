using CommunityToolkit.Mvvm.ComponentModel;
using TaskManager.Application.Common;

namespace TaskManager.Application.ViewModels.Dashboard;

public partial class DashboardViewModel : ViewModelBase
{
    [ObservableProperty]
    private double _cpuUsagePercent = 24.5;

    [ObservableProperty]
    private double _memoryUsagePercent = 58.2;

    [ObservableProperty]
    private double _diskUsagePercent = 12.0;

    [ObservableProperty]
    private double _networkUsageMbps = 15.4;

    [ObservableProperty]
    private int _processCount = 184;

    [ObservableProperty]
    private int _threadCount = 2840;

    [ObservableProperty]
    private int _handleCount = 94230;

    [ObservableProperty]
    private string _uptimeString = "1d 04h 32m";

    public DashboardViewModel()
    {
        Title = "Dashboard";
    }
}
