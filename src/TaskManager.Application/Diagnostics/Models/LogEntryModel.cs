namespace TaskManager.Application.Diagnostics.Models;

public record LogEntryModel(
    DateTime Timestamp,
    string Level,
    string Message,
    string? Exception = null,
    string RawLine = "");
