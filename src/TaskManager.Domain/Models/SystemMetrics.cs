namespace TaskManager.Domain.Models;

public class SystemMetrics
{
    public double TotalCpuUsagePercent { get; set; }
    public double CpuSpeedGhz { get; set; }
    public int ProcessCount { get; set; }
    public int ThreadCount { get; set; }
    public int HandleCount { get; set; }
    public TimeSpan UpTime { get; set; }

    public long TotalPhysicalMemoryBytes { get; set; }
    public long AvailableMemoryBytes { get; set; }
    public double MemoryUsagePercent => TotalPhysicalMemoryBytes > 0 
        ? ((double)(TotalPhysicalMemoryBytes - AvailableMemoryBytes) / TotalPhysicalMemoryBytes) * 100.0 
        : 0;

    public double DiskActiveTimePercent { get; set; }
    public double DiskResponseTimeMs { get; set; }
    public double DiskReadSpeedBytesPerSec { get; set; }
    public double DiskWriteSpeedBytesPerSec { get; set; }

    public double NetworkSendBytesPerSec { get; set; }
    public double NetworkReceiveBytesPerSec { get; set; }
    public double GpuUsagePercent { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
