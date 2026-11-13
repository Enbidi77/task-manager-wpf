namespace TaskManager.Domain.Entities;

public class MetricHistoryRecord
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public double CpuPercent { get; set; }
    public double MemoryPercent { get; set; }
    public double DiskActivePercent { get; set; }
    public double NetworkThroughputMbps { get; set; }

    public MetricHistoryRecord() { }

    public MetricHistoryRecord(
        DateTime timestamp,
        double cpuPercent,
        double memoryPercent,
        double diskActivePercent,
        double networkThroughputMbps)
    {
        Timestamp = timestamp;
        CpuPercent = cpuPercent;
        MemoryPercent = memoryPercent;
        DiskActivePercent = diskActivePercent;
        NetworkThroughputMbps = networkThroughputMbps;
    }
}
