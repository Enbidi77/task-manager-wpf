using System.Management;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using TaskManager.Domain.Models;

namespace TaskManager.Infrastructure.Services;

[SupportedOSPlatform("windows")]
public class SystemDiagnosticsService
{
    private readonly ILogger<SystemDiagnosticsService> _logger;

    public SystemDiagnosticsService(ILogger<SystemDiagnosticsService> logger)
    {
        _logger = logger;
    }

    public SystemMetrics GetCurrentSystemMetrics()
    {
        var metrics = new SystemMetrics
        {
            ProcessCount = System.Diagnostics.Process.GetProcesses().Length,
            Timestamp = DateTime.UtcNow
        };

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");
            foreach (var obj in searcher.Get())
            {
                if (obj["TotalVisibleMemorySize"] is ulong totalKb && obj["FreePhysicalMemory"] is ulong freeKb)
                {
                    metrics.TotalPhysicalMemoryBytes = (long)(totalKb * 1024);
                    metrics.AvailableMemoryBytes = (long)(freeKb * 1024);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve WMI memory statistics.");
        }

        return metrics;
    }
}
