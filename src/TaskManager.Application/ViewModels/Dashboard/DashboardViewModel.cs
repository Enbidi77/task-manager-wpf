using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TaskManager.Application.Common;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Monitoring.Interfaces;
using TaskManager.Application.Monitoring.Models;

namespace TaskManager.Application.ViewModels.Dashboard;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly IMonitoringCoordinator _monitoringCoordinator;
    private readonly IDispatcherService _dispatcherService;
    private const int MaxHistoryPoints = 60;

    [ObservableProperty]
    private double _cpuUsagePercent;

    [ObservableProperty]
    private double _memoryUsagePercent;

    [ObservableProperty]
    private double _diskUsagePercent;

    [ObservableProperty]
    private double _networkUsageMbps;

    [ObservableProperty]
    private double _gpuUsagePercent;

    [ObservableProperty]
    private string _cpuDetailText = "Multi-core processor";

    [ObservableProperty]
    private string _memoryDetailText = "0 / 0 GB in use";

    [ObservableProperty]
    private string _diskDetailText = "0 KB/s transfer rate";

    [ObservableProperty]
    private string _networkDetailText = "S: 0 Kbps / R: 0 Kbps";

    [ObservableProperty]
    private string _gpuDetailText = "Dedicated GPU";

    [ObservableProperty]
    private int _processCount;

    [ObservableProperty]
    private int _threadCount;

    [ObservableProperty]
    private int _handleCount;

    [ObservableProperty]
    private string _uptimeString = "00h 00m 00s";

    [ObservableProperty]
    private bool _isCpuHighAlert;

    [ObservableProperty]
    private bool _isMemoryHighAlert;

    public ObservableCollection<double> CpuHistory { get; } = new();
    public ObservableCollection<double> MemoryHistory { get; } = new();
    public ObservableCollection<double> DiskHistory { get; } = new();
    public ObservableCollection<double> NetworkHistory { get; } = new();

    public DashboardViewModel(
        IMonitoringCoordinator monitoringCoordinator,
        IDispatcherService? dispatcherService = null)
    {
        _monitoringCoordinator = monitoringCoordinator;
        _dispatcherService = dispatcherService ?? new ImmediateDispatcherService();
        Title = "Dashboard";

        InitializeHistoryBuffers();
        _monitoringCoordinator.SystemOverviewUpdated += OnSystemOverviewUpdated;
    }

    private void InitializeHistoryBuffers()
    {
        for (int i = 0; i < MaxHistoryPoints; i++)
        {
            CpuHistory.Add(0);
            MemoryHistory.Add(0);
            DiskHistory.Add(0);
            NetworkHistory.Add(0);
        }
    }

    public override async Task InitializeAsync()
    {
        try
        {
            var overview = await _monitoringCoordinator.RefreshOverviewAsync();
            OnSystemOverviewUpdated(overview);
        }
        catch
        {
            // Initial read error handled safely
        }
    }

    private void OnSystemOverviewUpdated(SystemOverviewSnapshot snapshot)
    {
        _dispatcherService.Invoke(() =>
        {
            CpuUsagePercent = snapshot.Cpu.OverallUtilizationPercent;
            MemoryUsagePercent = snapshot.Memory.UtilizationPercent;

            double diskActive = snapshot.Disks.Count > 0 ? snapshot.Disks.Max(d => d.ActiveTimePercent) : 0.0;
            DiskUsagePercent = diskActive;

            double netTotalBytesPerSec = snapshot.Networks.Sum(n => n.SendSpeedBytesPerSec + n.ReceiveSpeedBytesPerSec);
            NetworkUsageMbps = Math.Round((netTotalBytesPerSec * 8.0) / 1_000_000.0, 2);

            GpuUsagePercent = snapshot.Gpu?.UtilizationPercent ?? 0.0;

            // Details
            CpuDetailText = $"{snapshot.Cpu.SpeedGhz:F2} GHz ({snapshot.Cpu.CoreCount}C/{snapshot.Cpu.LogicalProcessorCount}T)";
            double usedGb = snapshot.Memory.UsedPhysicalBytes / (1024.0 * 1024.0 * 1024.0);
            double totalGb = snapshot.Memory.TotalPhysicalBytes / (1024.0 * 1024.0 * 1024.0);
            MemoryDetailText = $"{usedGb:F1} / {totalGb:F1} GB in use ({snapshot.Memory.UtilizationPercent:F0}%)";

            double diskReadKb = snapshot.Disks.Sum(d => d.ReadSpeedBytesPerSec) / 1024.0;
            double diskWriteKb = snapshot.Disks.Sum(d => d.WriteSpeedBytesPerSec) / 1024.0;
            DiskDetailText = $"R: {diskReadKb:F0} KB/s, W: {diskWriteKb:F0} KB/s";

            double netSendKbps = snapshot.Networks.Sum(n => n.SendSpeedBytesPerSec * 8) / 1000.0;
            double netRecvKbps = snapshot.Networks.Sum(n => n.ReceiveSpeedBytesPerSec * 8) / 1000.0;
            NetworkDetailText = $"S: {netSendKbps:F0} Kbps / R: {netRecvKbps:F0} Kbps";

            GpuDetailText = snapshot.Gpu != null ? snapshot.Gpu.Name : "GPU unavailable";

            ProcessCount = snapshot.TotalProcessCount;
            ThreadCount = snapshot.TotalThreadCount;
            HandleCount = snapshot.TotalHandleCount;

            var uptime = snapshot.Cpu.UpTime;
            UptimeString = uptime.TotalDays >= 1
                ? $"{(int)uptime.TotalDays}d {uptime.Hours:D2}h {uptime.Minutes:D2}m {uptime.Seconds:D2}s"
                : $"{uptime.Hours:D2}h {uptime.Minutes:D2}m {uptime.Seconds:D2}s";

            // High consumption alerts
            IsCpuHighAlert = CpuUsagePercent >= 80.0;
            IsMemoryHighAlert = MemoryUsagePercent >= 80.0;

            // Rolling history buffers
            UpdateHistoryBuffer(CpuHistory, CpuUsagePercent);
            UpdateHistoryBuffer(MemoryHistory, MemoryUsagePercent);
            UpdateHistoryBuffer(DiskHistory, DiskUsagePercent);
            UpdateHistoryBuffer(NetworkHistory, Math.Clamp(NetworkUsageMbps, 0, 100));
        });
    }

    private static void UpdateHistoryBuffer(ObservableCollection<double> buffer, double value)
    {
        if (buffer.Count >= MaxHistoryPoints)
        {
            buffer.RemoveAt(0);
        }
        buffer.Add(value);
    }
}
