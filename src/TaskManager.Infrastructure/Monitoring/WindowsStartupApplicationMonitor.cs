using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using TaskManager.Application.Monitoring.Interfaces;
using TaskManager.Application.Monitoring.Models;

namespace TaskManager.Infrastructure.Monitoring;

[SupportedOSPlatform("windows")]
public class WindowsStartupApplicationMonitor : IStartupApplicationMonitor
{
    private readonly ILogger<WindowsStartupApplicationMonitor> _logger;

    private static readonly (RegistryHive Hive, string SubKey, string LocationName)[] RegistryLocations = new[]
    {
        (RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run", "Registry: HKCU Run"),
        (RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\RunOnce", "Registry: HKCU RunOnce"),
        (RegistryHive.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run", "Registry: HKLM Run"),
        (RegistryHive.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\RunOnce", "Registry: HKLM RunOnce"),
    };

    public WindowsStartupApplicationMonitor(ILogger<WindowsStartupApplicationMonitor> logger)
    {
        _logger = logger;
    }

    public ValueTask<IReadOnlyList<StartupAppSnapshot>> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var list = new List<StartupAppSnapshot>();
        var now = DateTime.UtcNow;

        // 1. Registry Run / RunOnce keys
        foreach (var (hive, subKey, locationName) in RegistryLocations)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
                using var key = baseKey.OpenSubKey(subKey, writable: false);
                if (key == null) continue;

                var valueNames = key.GetValueNames();
                foreach (var name in valueNames)
                {
                    var cmdObj = key.GetValue(name);
                    string command = cmdObj?.ToString() ?? string.Empty;
                    string publisher = ExtractPublisher(command);
                    string impact = CalculateImpact(command);

                    string id = $"{locationName}:{name}";

                    list.Add(new StartupAppSnapshot(
                        Id: id,
                        Name: name,
                        Publisher: publisher,
                        Status: "Enabled",
                        StartupImpact: impact,
                        Command: command,
                        Location: locationName,
                        Timestamp: now));
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to read registry startup location: {Location}", locationName);
            }
        }

        // 2. User & Common Startup folders
        ScanFolder(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "User Startup Folder", list, now);
        ScanFolder(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), "All Users Startup Folder", list, now);

        return ValueTask.FromResult<IReadOnlyList<StartupAppSnapshot>>(list);
    }

    private void ScanFolder(string folderPath, string locationName, List<StartupAppSnapshot> list, DateTime now)
    {
        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath)) return;

        try
        {
            var files = Directory.GetFiles(folderPath);
            foreach (var file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                string publisher = ExtractPublisher(file);

                list.Add(new StartupAppSnapshot(
                    Id: $"{locationName}:{name}",
                    Name: name,
                    Publisher: publisher,
                    Status: "Enabled",
                    StartupImpact: "Low",
                    Command: file,
                    Location: locationName,
                    Timestamp: now));
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to scan startup folder: {Folder}", folderPath);
        }
    }

    private static string ExtractPublisher(string command)
    {
        try
        {
            string cleanPath = command.Trim('\"');
            int exeIndex = cleanPath.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            if (exeIndex > 0)
            {
                cleanPath = cleanPath.Substring(0, exeIndex + 4);
            }

            if (File.Exists(cleanPath))
            {
                var vi = FileVersionInfo.GetVersionInfo(cleanPath);
                if (!string.IsNullOrWhiteSpace(vi.CompanyName))
                    return vi.CompanyName;
            }
        }
        catch
        {
            // Ignore path extraction errors
        }

        return "Unknown";
    }

    private static string CalculateImpact(string command)
    {
        if (command.Contains("Update", StringComparison.OrdinalIgnoreCase) ||
            command.Contains("Browser", StringComparison.OrdinalIgnoreCase) ||
            command.Contains("Discord", StringComparison.OrdinalIgnoreCase) ||
            command.Contains("Steam", StringComparison.OrdinalIgnoreCase))
        {
            return "High";
        }
        if (command.Contains("OneDrive", StringComparison.OrdinalIgnoreCase) ||
            command.Contains("Teams", StringComparison.OrdinalIgnoreCase) ||
            command.Contains("Cloud", StringComparison.OrdinalIgnoreCase))
        {
            return "Medium";
        }
        return "Low";
    }

    public ValueTask<bool> SetStartupAppEnabledAsync(string id, bool enabled, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Setting startup application {Id} enabled={Enabled}", id, enabled);
        return ValueTask.FromResult(true);
    }
}
