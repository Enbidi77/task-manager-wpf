using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using TaskManager.Application.Monitoring.Interfaces;
using TaskManager.Application.Monitoring.Models;

namespace TaskManager.Infrastructure.Monitoring;

[SupportedOSPlatform("windows")]
public class WindowsDiskMonitor : IDiskMonitor, IDisposable
{
    private readonly ILogger<WindowsDiskMonitor> _logger;
    private readonly List<PerformanceCounter> _readCounters = new();
    private readonly List<PerformanceCounter> _writeCounters = new();
    private readonly List<PerformanceCounter> _timeCounters = new();
    private readonly object _syncRoot = new();
    private bool _initialized;

    public WindowsDiskMonitor(ILogger<WindowsDiskMonitor> logger)
    {
        _logger = logger;
        InitializeCounters();
    }

    private void InitializeCounters()
    {
        try
        {
            var category = new PerformanceCounterCategory("LogicalDisk");
            var instances = category.GetInstanceNames()
                .Where(name => !name.Equals("_Total", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var instance in instances)
            {
                try
                {
                    _readCounters.Add(new PerformanceCounter("LogicalDisk", "Disk Read Bytes/sec", instance, true));
                    _writeCounters.Add(new PerformanceCounter("LogicalDisk", "Disk Write Bytes/sec", instance, true));
                    _timeCounters.Add(new PerformanceCounter("LogicalDisk", "% Disk Time", instance, true));
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to initialize performance counter for disk instance {Instance}", instance);
                }
            }

            _initialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "LogicalDisk performance counter category is unavailable. Fallback will be used.");
        }
    }

    public ValueTask<IReadOnlyList<DiskSnapshot>> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var snapshots = new List<DiskSnapshot>();
        var readyDrives = DriveInfo.GetDrives()
            .Where(d => d.IsReady && (d.DriveType == DriveType.Fixed || d.DriveType == DriveType.Removable))
            .ToList();

        lock (_syncRoot)
        {
            foreach (var drive in readyDrives)
            {
                string driveLetter = drive.Name.TrimEnd('\\');
                double readSpeed = 0.0;
                double writeSpeed = 0.0;
                double activeTime = 0.0;

                if (_initialized)
                {
                    try
                    {
                        var readCounter = _readCounters.FirstOrDefault(c => c.InstanceName.StartsWith(driveLetter, StringComparison.OrdinalIgnoreCase));
                        var writeCounter = _writeCounters.FirstOrDefault(c => c.InstanceName.StartsWith(driveLetter, StringComparison.OrdinalIgnoreCase));
                        var timeCounter = _timeCounters.FirstOrDefault(c => c.InstanceName.StartsWith(driveLetter, StringComparison.OrdinalIgnoreCase));

                        if (readCounter != null) readSpeed = Math.Max(0, readCounter.NextValue());
                        if (writeCounter != null) writeSpeed = Math.Max(0, writeCounter.NextValue());
                        if (timeCounter != null) activeTime = Math.Clamp(timeCounter.NextValue(), 0.0, 100.0);
                    }
                    catch
                    {
                        // Performance counter reading error ignored; fallback values
                    }
                }

                snapshots.Add(new DiskSnapshot(
                    Name: drive.Name,
                    DiskModel: string.IsNullOrEmpty(drive.VolumeLabel) ? "Local Fixed Disk" : drive.VolumeLabel,
                    ActiveTimePercent: Math.Round(activeTime, 1),
                    ReadSpeedBytesPerSec: Math.Round(readSpeed, 0),
                    WriteSpeedBytesPerSec: Math.Round(writeSpeed, 0),
                    ResponseTimeMs: activeTime > 0 ? Math.Round(activeTime * 0.15, 1) : 0.0,
                    TotalSizeBytes: drive.TotalSize,
                    AvailableFreeBytes: drive.AvailableFreeSpace,
                    Timestamp: DateTime.UtcNow));
            }
        }

        return ValueTask.FromResult<IReadOnlyList<DiskSnapshot>>(snapshots);
    }

    public void Dispose()
    {
        lock (_syncRoot)
        {
            foreach (var c in _readCounters) c.Dispose();
            foreach (var c in _writeCounters) c.Dispose();
            foreach (var c in _timeCounters) c.Dispose();
            _readCounters.Clear();
            _writeCounters.Clear();
            _timeCounters.Clear();
        }
    }
}
