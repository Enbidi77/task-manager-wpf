namespace TaskManager.Domain.Entities;

public class ProcessFilterEntity
{
    public int Id { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public bool IsFavorite { get; set; }
    public bool IsExcluded { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ProcessFilterEntity() { }

    public ProcessFilterEntity(
        string processName,
        bool isFavorite = false,
        bool isExcluded = false,
        string? description = null)
    {
        ProcessName = processName;
        IsFavorite = isFavorite;
        IsExcluded = isExcluded;
        Description = description;
        CreatedAtUtc = DateTime.UtcNow;
    }
}
