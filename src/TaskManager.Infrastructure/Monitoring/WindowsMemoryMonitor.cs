using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using TaskManager.Application.Monitoring.Interfaces;
using TaskManager.Application.Monitoring.Models;

namespace TaskManager.Infrastructure.Monitoring;

[SupportedOSPlatform("windows")]
public class WindowsMemoryMonitor : IMemoryMonitor
{
    private readonly ILogger<WindowsMemoryMonitor> _logger;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;

        public static MEMORYSTATUSEX Create()
        {
            return new MEMORYSTATUSEX
            {
                dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>()
            };
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    public WindowsMemoryMonitor(ILogger<WindowsMemoryMonitor> logger)
    {
        _logger = logger;
    }

    public ValueTask<MemorySnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var memStatus = MEMORYSTATUSEX.Create();
        if (GlobalMemoryStatusEx(ref memStatus))
        {
            long totalPhys = (long)memStatus.ullTotalPhys;
            long availPhys = (long)memStatus.ullAvailPhys;
            long usedPhys = Math.Max(0, totalPhys - availPhys);

            double utilizationPercent = totalPhys > 0
                ? Math.Round(((double)usedPhys / totalPhys) * 100.0, 1)
                : 0.0;

            long commitLimit = (long)memStatus.ullTotalPageFile;
            long commitAvail = (long)memStatus.ullAvailPageFile;
            long committed = Math.Max(0, commitLimit - commitAvail);

            // Approximate kernel pools (typically ~400MB to 1GB on modern Windows)
            long nonPagedPool = 512L * 1024 * 1024;
            long pagedPool = 640L * 1024 * 1024;

            var snapshot = new MemorySnapshot(
                TotalPhysicalBytes: totalPhys,
                AvailablePhysicalBytes: availPhys,
                UsedPhysicalBytes: usedPhys,
                UtilizationPercent: utilizationPercent,
                CommittedBytes: committed,
                CommitLimitBytes: commitLimit,
                PagedPoolBytes: pagedPool,
                NonPagedPoolBytes: nonPagedPool,
                Timestamp: DateTime.UtcNow);

            return ValueTask.FromResult(snapshot);
        }

        _logger.LogWarning("GlobalMemoryStatusEx failed; falling back to default memory snapshot.");
        return ValueTask.FromResult(new MemorySnapshot(0, 0, 0, 0, 0, 0, 0, 0, DateTime.UtcNow));
    }
}
