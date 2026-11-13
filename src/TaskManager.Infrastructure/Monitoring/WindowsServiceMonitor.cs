using System.Management;
using System.Runtime.Versioning;
using System.ServiceProcess;
using Microsoft.Extensions.Logging;
using TaskManager.Application.Monitoring.Interfaces;
using TaskManager.Application.Monitoring.Models;

namespace TaskManager.Infrastructure.Monitoring;

[SupportedOSPlatform("windows")]
public class WindowsServiceMonitor : IServiceMonitor
{
    private readonly ILogger<WindowsServiceMonitor> _logger;
    private readonly Dictionary<string, string> _descriptionCache = new(StringComparer.OrdinalIgnoreCase);
    private DateTime _lastDescriptionCacheTime = DateTime.MinValue;

    public WindowsServiceMonitor(ILogger<WindowsServiceMonitor> logger)
    {
        _logger = logger;
    }

    private void EnsureDescriptionCache()
    {
        if ((DateTime.UtcNow - _lastDescriptionCacheTime).TotalMinutes < 15 && _descriptionCache.Count > 0)
            return;

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name, Description FROM Win32_Service");
            foreach (var obj in searcher.Get())
            {
                if (obj["Name"] is string name && obj["Description"] is string desc)
                {
                    _descriptionCache[name] = desc;
                }
            }
            _lastDescriptionCacheTime = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to populate service description cache from WMI.");
        }
    }

    public ValueTask<IReadOnlyList<ServiceSnapshot>> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        EnsureDescriptionCache();

        var list = new List<ServiceSnapshot>();
        var now = DateTime.UtcNow;

        try
        {
            var services = ServiceController.GetServices();
            foreach (var sc in services)
            {
                using (sc)
                {
                    string name = sc.ServiceName;
                    string displayName = sc.DisplayName;
                    string status = sc.Status.ToString();
                    string startupType = sc.StartType.ToString();

                    _descriptionCache.TryGetValue(name, out var desc);

                    list.Add(new ServiceSnapshot(
                        ServiceName: name,
                        DisplayName: string.IsNullOrEmpty(displayName) ? name : displayName,
                        Status: status,
                        StartupType: startupType,
                        ProcessId: null,
                        Description: desc ?? string.Empty,
                        Timestamp: now));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enumerating Windows services.");
        }

        return ValueTask.FromResult<IReadOnlyList<ServiceSnapshot>>(list);
    }

    public async ValueTask<bool> StartServiceAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        try
        {
            using var sc = new ServiceController(serviceName);
            if (sc.Status == ServiceControllerStatus.Running || sc.Status == ServiceControllerStatus.StartPending)
                return true;

            sc.Start();
            await Task.Run(() => sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10)), cancellationToken);
            _logger.LogInformation("Service {ServiceName} started successfully.", serviceName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to start service {ServiceName}", serviceName);
            return false;
        }
    }

    public async ValueTask<bool> StopServiceAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        try
        {
            using var sc = new ServiceController(serviceName);
            if (sc.Status == ServiceControllerStatus.Stopped || sc.Status == ServiceControllerStatus.StopPending)
                return true;

            if (!sc.CanStop)
            {
                _logger.LogWarning("Service {ServiceName} cannot be stopped.", serviceName);
                return false;
            }

            sc.Stop();
            await Task.Run(() => sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10)), cancellationToken);
            _logger.LogInformation("Service {ServiceName} stopped successfully.", serviceName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to stop service {ServiceName}", serviceName);
            return false;
        }
    }

    public async ValueTask<bool> RestartServiceAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        bool stopped = await StopServiceAsync(serviceName, cancellationToken);
        if (!stopped) return false;

        return await StartServiceAsync(serviceName, cancellationToken);
    }
}
