using System.IO;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManager.Infrastructure.Services;
using Xunit;

namespace TaskManager.Infrastructure.Tests;

public class DiagnosticLogServiceTests : IDisposable
{
    private readonly string _tempLogDir;
    private readonly Mock<ILogger<DiagnosticLogService>> _mockLogger = new();

    public DiagnosticLogServiceTests()
    {
        _tempLogDir = Path.Combine(Path.GetTempPath(), "TaskManagerLogTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempLogDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempLogDir))
            {
                Directory.Delete(_tempLogDir, true);
            }
        }
        catch { }
    }

    [Fact]
    public async Task GetLogEntriesAsync_ParsesSerilogLines_AndFiltersByLevel()
    {
        // Arrange
        var logFile = Path.Combine(_tempLogDir, "taskmanager-20261113.log");
        var lines = new[]
        {
            "2026-11-13 10:00:00.000 +00:00 [INF] Host started successfully",
            "2026-11-13 10:01:00.000 +00:00 [WRN] High CPU usage detected",
            "2026-11-13 10:02:00.000 +00:00 [ERR] Failed to terminate process\r\nSystem.UnauthorizedAccessException: Access denied"
        };
        await File.WriteAllLinesAsync(logFile, lines);

        var service = new DiagnosticLogService(_tempLogDir, _mockLogger.Object);

        // Act 1: Get all
        var all = await service.GetLogEntriesAsync("All", null);

        // Assert 1
        all.Should().HaveCount(3);
        all.Any(l => l.Level == "Information").Should().BeTrue();
        all.Any(l => l.Level == "Warning").Should().BeTrue();
        all.Any(l => l.Level == "Error").Should().BeTrue();

        // Act 2: Filter by Warning
        var warnings = await service.GetLogEntriesAsync("Warning", null);
        warnings.Should().HaveCount(1);
        warnings.First().Message.Should().Contain("High CPU");

        // Act 3: Filter by Search query
        var searchResult = await service.GetLogEntriesAsync("All", "Access denied");
        searchResult.Should().HaveCount(1);
        searchResult.First().Exception.Should().Contain("UnauthorizedAccessException");
    }

    [Fact]
    public async Task ClearLogsAsync_ClearsLogFileContents()
    {
        // Arrange
        var logFile = Path.Combine(_tempLogDir, "taskmanager-20261113.log");
        await File.WriteAllTextAsync(logFile, "2026-11-13 10:00:00.000 +00:00 [INF] Test line\r\n");

        var service = new DiagnosticLogService(_tempLogDir, _mockLogger.Object);

        // Act
        await service.ClearLogsAsync();
        var entries = await service.GetLogEntriesAsync("All", null);

        // Assert
        entries.Should().BeEmpty();
    }
}
