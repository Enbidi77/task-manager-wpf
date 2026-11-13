namespace TaskManager.Domain.Entities;

public class UserSettingEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public UserSettingEntity() { }

    public UserSettingEntity(string key, string value)
    {
        Key = key;
        Value = value;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
