using TaskManager.Application.Diagnostics.Models;

namespace TaskManager.Application.Common.Interfaces;

public interface IDiagnosticLogService
{
    Task<IReadOnlyList<LogEntryModel>> GetLogEntriesAsync(
        string? levelFilter = null,
        string? searchFilter = null,
        int maxEntries = 500,
        CancellationToken cancellationToken = default);

    Task ClearLogsAsync(CancellationToken cancellationToken = default);
    string GetLogDirectory();
    bool OpenLogDirectory();
}
