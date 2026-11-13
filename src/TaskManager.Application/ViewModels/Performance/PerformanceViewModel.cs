using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskManager.Application.Common;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Monitoring.Interfaces;
using TaskManager.Application.Monitoring.Models;

namespace TaskManager.Application.ViewModels.Performance;

public partial class PerformanceViewModel : ViewModelBase
{
    private readonly IMonitoringCoordinator _monitoringCoordinator;
    private readonly IDispatcherService _dispatcherService;
    private const int MaxHistoryPoints = 60;

    [ObservableProperty]
    private string _selectedTab = "CPU";

    // CPU Metrics
    [ObservableProperty]
    private double _cpuUsagePercent;

    [ObservableProperty]
    private double _cpuSpeedGhz = 3.60;

    [ObservableProperty]
    private int _cpuProcesses;

    [ObservableProperty]
    private int _cpuThreads;

    [ObservableProperty]
    private int _cpuHandles;

    [ObservableProperty]
    private int _cpuCores = 8;

    [ObservableProperty]
    private int _cpuLogicalProcessors = 16;

    [ObservableProperty]
    private string _cpuUptime = "00h 00m 00s";

    // Memory Metrics
    [ObservableProperty]
    private double _memoryUsagePercent;

    [ObservableProperty]
    private double _memoryInUseGb;

    [ObservableProperty]
    private double _memoryAvailableGb;

    [ObservableProperty]
    private double _memoryTotalGb;

    [ObservableProperty]
    private double _memoryCommittedGb;

    [ObservableProperty]
    private double _memoryCommitLimitGb;

    // Disk Metrics
    [ObservableProperty]
    private double _diskActivePercent;

    [ObservableProperty]
    private double _diskReadSpeedKbps;

    [ObservableProperty]
    private double _diskWriteSpeedKbps;

    [ObservableProperty]
    private string _diskName = "C:";

    [ObservableProperty]
    private string _diskModel = "NVMe SSD";

    [ObservableProperty]
    private double _diskCapacityGb;

    [ObservableProperty]
    private double _diskFreeGb;

    // Network Metrics
    [ObservableProperty]
    private double _networkSendKbps;

    [ObservableProperty]
    private double _networkReceiveKbps;

    [ObservableProperty]
    private string _networkAdapterName = "Ethernet";

    [ObservableProperty]
    private string _networkIpv4Address = "127.0.0.1";

    [ObservableProperty]
    private long _networkTotalBytesSent;

    [ObservableProperty]
    private long _networkTotalBytesReceived;

    // GPU Metrics
    [ObservableProperty]
    private double _gpuUsagePercent;

    [ObservableProperty]
    private string _gpuName = "GPU";

    [ObservableProperty]
    private double _gpuDedicatedUsedGb;

    [ObservableProperty]
    private double _gpuDedicatedTotalGb;

    [ObservableProperty]
    private double _gpuTemperatureC = 48.0;

    // Real-time History Buffers
    public ObservableCollection<double> CpuHistory { get; } = new();
    public ObservableCollection<double> MemoryHistory { get; } = new();
    public ObservableCollection<double> DiskHistory { get; } = new();
    public ObservableCollection<double> NetworkHistory { get; } = new();
    public ObservableCollection<double> GpuHistory { get; } = new();

    public PerformanceViewModel(
        IMonitoringCoordinator monitoringCoordinator,
        IDispatcherService? dispatcherService = null)
    {
        _monitoringCoordinator = monitoringCoordinator;
        _dispatcherService = dispatcherService ?? new ImmediateDispatcherService();
        Title = "Performance";

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
            GpuHistory.Add(0);
        }
    }

    public override async Task InitializeAsync()
    {
        try
        {
            var overview = await _monitoringCoordinator.RefreshOverviewAsync();
            OnSystemOverviewUpdated(overview);
        }
        catch { }
    }

    private void OnSystemOverviewUpdated(SystemOverviewSnapshot snapshot)
    {
        _dispatcherService.Invoke(() =>
        {
            // CPU
            CpuUsagePercent = snapshot.Cpu.OverallUtilizationPercent;
            CpuSpeedGhz = snapshot.Cpu.SpeedGhz;
            CpuCores = snapshot.Cpu.CoreCount;
            CpuLogicalProcessors = snapshot.Cpu.LogicalProcessorCount;
            CpuProcesses = snapshot.TotalProcessCount;
            CpuThreads = snapshot.TotalThreadCount;
            CpuHandles = snapshot.TotalHandleCount;

            var uptime = snapshot.Cpu.UpTime;
            CpuUptime = uptime.TotalDays >= 1
                ? $"{(int)uptime.TotalDays}d {uptime.Hours:D2}h {uptime.Minutes:D2}m {uptime.Seconds:D2}s"
                : $"{uptime.Hours:D2}h {uptime.Minutes:D2}m {uptime.Seconds:D2}s";

            // Memory
            MemoryUsagePercent = snapshot.Memory.UtilizationPercent;
            MemoryInUseGb = Math.Round(snapshot.Memory.UsedPhysicalBytes / (1024.0 * 1024.0 * 1024.0), 1);
            MemoryAvailableGb = Math.Round(snapshot.Memory.AvailablePhysicalBytes / (1024.0 * 1024.0 * 1024.0), 1);
            MemoryTotalGb = Math.Round(snapshot.Memory.TotalPhysicalBytes / (1024.0 * 1024.0 * 1024.0), 1);
            MemoryCommittedGb = Math.Round(snapshot.Memory.CommittedBytes / (1024.0 * 1024.0 * 1024.0), 1);
            MemoryCommitLimitGb = Math.Round(snapshot.Memory.CommitLimitBytes / (1024.0 * 1024.0 * 1024.0), 1);

            // Disk
            if (snapshot.Disks.Count > 0)
            {
                var primaryDisk = snapshot.Disks[0];
                DiskName = primaryDisk.Name;
                DiskModel = primaryDisk.DiskModel;
                DiskActivePercent = primaryDisk.ActiveTimePercent;
                DiskReadSpeedKbps = Math.Round(primaryDisk.ReadSpeedBytesPerSec / 1024.0, 1);
                DiskWriteSpeedKbps = Math.Round(primaryDisk.WriteSpeedBytesPerSec / 1024.0, 1);
                DiskCapacityGb = Math.Round(primaryDisk.TotalSizeBytes / (1024.0 * 1024.0 * 1024.0), 1);
                DiskFreeGb = Math.Round(primaryDisk.AvailableFreeBytes / (1024.0 * 1024.0 * 1024.0), 1);
            }

            // Network
            if (snapshot.Networks.Count > 0)
            {
                var primaryNet = snapshot.Networks[0];
                NetworkAdapterName = primaryNet.AdapterName;
                NetworkIpv4Address = string.IsNullOrEmpty(primaryNet.Ipv4Address) ? "127.0.0.1" : primaryNet.Ipv4Address;
                NetworkSendKbps = Math.Round((primaryNet.SendSpeedBytesPerSec * 8) / 1000.0, 1);
                NetworkReceiveKbps = Math.Round((primaryNet.ReceiveSpeedBytesPerSec * 8) / 1000.0, 1);
                NetworkTotalBytesSent = primaryNet.TotalBytesSent;
                NetworkTotalBytesReceived = primaryNet.TotalBytesReceived;
            }

            // GPU
            if (snapshot.Gpu != null)
            {
                GpuName = snapshot.Gpu.Name;
                GpuUsagePercent = snapshot.Gpu.UtilizationPercent;
                GpuTemperatureC = snapshot.Gpu.TemperatureCelsius;
                GpuDedicatedUsedGb = Math.Round(snapshot.Gpu.DedicatedMemoryUsedBytes / (1024.0 * 1024.0 * 1024.0), 1);
                GpuDedicatedTotalGb = Math.Round(snapshot.Gpu.DedicatedMemoryBytes / (1024.0 * 1024.0 * 1024.0), 1);
            }

            // History
            UpdateHistoryBuffer(CpuHistory, CpuUsagePercent);
            UpdateHistoryBuffer(MemoryHistory, MemoryUsagePercent);
            UpdateHistoryBuffer(DiskHistory, DiskActivePercent);
            double netCombinedPct = Math.Clamp((NetworkSendKbps + NetworkReceiveKbps) / 1000.0, 0, 100);
            UpdateHistoryBuffer(NetworkHistory, netCombinedPct);
            UpdateHistoryBuffer(GpuHistory, GpuUsagePercent);
        });
    }

    [RelayCommand]
    private void SelectTab(string tabName)
    {
        SelectedTab = tabName;
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
