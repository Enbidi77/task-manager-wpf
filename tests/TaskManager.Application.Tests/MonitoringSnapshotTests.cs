using FluentAssertions;
using TaskManager.Application.Monitoring.Models;
using Xunit;

namespace TaskManager.Application.Tests;

public class MonitoringSnapshotTests
{
    [Fact]
    public void CpuSnapshot_InstantiatesWithExpectedValues()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var coreUtils = new List<double> { 12.5, 24.0, 8.0, 15.2 };

        // Act
        var cpu = new CpuSnapshot(
            OverallUtilizationPercent: 14.9,
            CoreUtilizations: coreUtils,
            SpeedGhz: 3.5,
            LogicalProcessorCount: 4,
            SocketCount: 1,
            CoreCount: 2,
            UpTime: TimeSpan.FromHours(5),
            Timestamp: now);

        // Assert
        cpu.OverallUtilizationPercent.Should().Be(14.9);
        cpu.CoreUtilizations.Should().HaveCount(4);
        cpu.SpeedGhz.Should().Be(3.5);
        cpu.LogicalProcessorCount.Should().Be(4);
        cpu.SocketCount.Should().Be(1);
        cpu.CoreCount.Should().Be(2);
        cpu.UpTime.Should().Be(TimeSpan.FromHours(5));
        cpu.Timestamp.Should().Be(now);
    }

    [Fact]
    public void MemorySnapshot_InstantiatesWithExpectedValues()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // Act
        var mem = new MemorySnapshot(
            TotalPhysicalBytes: 32L * 1024 * 1024 * 1024,
            AvailablePhysicalBytes: 16L * 1024 * 1024 * 1024,
            UsedPhysicalBytes: 16L * 1024 * 1024 * 1024,
            UtilizationPercent: 50.0,
            CommittedBytes: 20L * 1024 * 1024 * 1024,
            CommitLimitBytes: 40L * 1024 * 1024 * 1024,
            PagedPoolBytes: 500 * 1024 * 1024,
            NonPagedPoolBytes: 400 * 1024 * 1024,
            Timestamp: now);

        // Assert
        mem.TotalPhysicalBytes.Should().Be(32L * 1024 * 1024 * 1024);
        mem.UtilizationPercent.Should().Be(50.0);
        mem.UsedPhysicalBytes.Should().Be(16L * 1024 * 1024 * 1024);
    }

    [Fact]
    public void ProcessSnapshot_GeneratesProcessKey_AndGuardsIdentity()
    {
        // Arrange
        var startTime = new DateTime(2026, 11, 13, 12, 0, 0, DateTimeKind.Utc);
        var key = $"1234_{startTime.Ticks}";

        // Act
        var proc = new ProcessSnapshot(
            ProcessId: 1234,
            StartTimeUtc: startTime,
            ProcessKey: key,
            Name: "taskmgr",
            Description: "Task Manager",
            ExecutablePath: @"C:\Windows\System32\Taskmgr.exe",
            ParentProcessId: 1000,
            CpuPercent: 2.1,
            WorkingSetBytes: 50_000_000,
            PrivateMemoryBytes: 30_000_000,
            ReadSpeedBytesPerSec: 1024,
            WriteSpeedBytesPerSec: 2048,
            ThreadCount: 15,
            HandleCount: 420,
            UserName: "User",
            Status: "Running",
            Timestamp: DateTime.UtcNow);

        // Assert
        proc.ProcessId.Should().Be(1234);
        proc.ProcessKey.Should().Be(key);
        proc.ParentProcessId.Should().Be(1000);
        proc.Status.Should().Be("Running");
    }
}
