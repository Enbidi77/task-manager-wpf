using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Diagnostics.Models;

namespace TaskManager.Infrastructure.Services;

public partial class DiagnosticLogService : IDiagnosticLogService
{
    private readonly ILogger<DiagnosticLogService> _logger;
    private readonly string _logDirectory;

    [GeneratedRegex(@"^(\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}\.\d{3}\s+[+-]\d{2}:\d{2})\s+\[([A-Z]{3})\]\s+(.*)$")]
    private static partial Regex LogLineRegex();

    public DiagnosticLogService(ILogger<DiagnosticLogService> logger)
    {
        _logger = logger;
        _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
    }

    public DiagnosticLogService(string customLogDirectory, ILogger<DiagnosticLogService> logger)
    {
        _logger = logger;
        _logDirectory = customLogDirectory;
    }

    public string GetLogDirectory() => _logDirectory;

    public bool OpenLogDirectory()
    {
        try
        {
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = _logDirectory,
                UseShellExecute = true
            });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open log directory: {Path}", _logDirectory);
            return false;
        }
    }

    public async Task<IReadOnlyList<LogEntryModel>> GetLogEntriesAsync(
        string? levelFilter = null,
        string? searchFilter = null,
        int maxEntries = 500,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var entries = new List<LogEntryModel>();

            if (!Directory.Exists(_logDirectory))
                return entries;

            var logFiles = Directory.GetFiles(_logDirectory, "taskmanager-*.log")
                .OrderByDescending(f => f)
                .ToList();

            var regex = LogLineRegex();

            foreach (var filePath in logFiles)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(stream);

                    LogEntryModel? currentEntry = null;
                    var exceptionLines = new List<string>();

                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var match = regex.Match(line);
                        if (match.Success)
                        {
                            if (currentEntry != null)
                            {
                                var finalEntry = exceptionLines.Count > 0
                                    ? currentEntry with { Exception = string.Join(Environment.NewLine, exceptionLines) }
                                    : currentEntry;

                                if (MatchesFilter(finalEntry, levelFilter, searchFilter))
                                {
                                    entries.Add(finalEntry);
                                }
                                exceptionLines.Clear();
                            }

                            var timeStr = match.Groups[1].Value;
                            var levelCode = match.Groups[2].Value;
                            var message = match.Groups[3].Value;

                            if (!DateTimeOffset.TryParse(timeStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto))
                            {
                                dto = DateTimeOffset.UtcNow;
                            }

                            var fullLevel = MapLevelCode(levelCode);
                            currentEntry = new LogEntryModel(dto.LocalDateTime, fullLevel, message, null, line);
                        }
                        else
                        {
                            if (currentEntry != null && !string.IsNullOrWhiteSpace(line))
                            {
                                exceptionLines.Add(line);
                            }
                        }
                    }

                    if (currentEntry != null)
                    {
                        var finalEntry = exceptionLines.Count > 0
                            ? currentEntry with { Exception = string.Join(Environment.NewLine, exceptionLines) }
                            : currentEntry;

                        if (MatchesFilter(finalEntry, levelFilter, searchFilter))
                        {
                            entries.Add(finalEntry);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read log file {Path}", filePath);
                }

                if (entries.Count >= maxEntries * 2)
                    break;
            }

            return entries
                .OrderByDescending(e => e.Timestamp)
                .Take(maxEntries)
                .ToList();
        }, cancellationToken);
    }

    public async Task ClearLogsAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            if (!Directory.Exists(_logDirectory))
                return;

            var logFiles = Directory.GetFiles(_logDirectory, "taskmanager-*.log");
            foreach (var file in logFiles)
            {
                try
                {
                    // Attempt truncate in case file is currently locked for writing by Serilog
                    using (var fs = new FileStream(file, FileMode.Truncate, FileAccess.Write, FileShare.ReadWrite))
                    {
                        // Truncated successfully
                    }
                }
                catch
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Unable to delete or truncate log file {File}", file);
                    }
                }
            }
        }, cancellationToken);
    }

    private static bool MatchesFilter(LogEntryModel entry, string? levelFilter, string? searchFilter)
    {
        if (!string.IsNullOrWhiteSpace(levelFilter) && !levelFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            if (!entry.Level.Equals(levelFilter, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (!string.IsNullOrWhiteSpace(searchFilter))
        {
            var matchMsg = entry.Message.Contains(searchFilter, StringComparison.OrdinalIgnoreCase);
            var matchEx = entry.Exception?.Contains(searchFilter, StringComparison.OrdinalIgnoreCase) ?? false;
            var matchLvl = entry.Level.Contains(searchFilter, StringComparison.OrdinalIgnoreCase);

            if (!matchMsg && !matchEx && !matchLvl)
                return false;
        }

        return true;
    }

    private static string MapLevelCode(string code) => code.ToUpperInvariant() switch
    {
        "VRB" => "Verbose",
        "DBG" => "Debug",
        "INF" => "Information",
        "WRN" => "Warning",
        "ERR" => "Error",
        "FTL" => "Fatal",
        _ => code
    };
}
