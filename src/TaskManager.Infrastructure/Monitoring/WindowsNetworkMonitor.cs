using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using TaskManager.Application.Monitoring.Interfaces;
using TaskManager.Application.Monitoring.Models;

namespace TaskManager.Infrastructure.Monitoring;

[SupportedOSPlatform("windows")]
public class WindowsNetworkMonitor : INetworkMonitor
{
    private readonly ILogger<WindowsNetworkMonitor> _logger;
    private readonly Dictionary<string, (long BytesSent, long BytesReceived, DateTime Timestamp)> _previousReadings = new();
    private readonly object _syncRoot = new();

    public WindowsNetworkMonitor(ILogger<WindowsNetworkMonitor> logger)
    {
        _logger = logger;
    }

    public ValueTask<IReadOnlyList<NetworkSnapshot>> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var snapshots = new List<NetworkSnapshot>();
        var now = DateTime.UtcNow;

        lock (_syncRoot)
        {
            try
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(nic => nic.OperationalStatus == OperationalStatus.Up &&
                                  nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .ToList();

                foreach (var nic in interfaces)
                {
                    var stats = nic.GetIPStatistics();
                    long bytesSent = stats.BytesSent;
                    long bytesReceived = stats.BytesReceived;

                    double sendSpeed = 0.0;
                    double receiveSpeed = 0.0;

                    if (_previousReadings.TryGetValue(nic.Id, out var prev))
                    {
                        double elapsedSeconds = (now - prev.Timestamp).TotalSeconds;
                        if (elapsedSeconds > 0.1)
                        {
                            long deltaSent = Math.Max(0, bytesSent - prev.BytesSent);
                            long deltaRecv = Math.Max(0, bytesReceived - prev.BytesReceived);

                            sendSpeed = deltaSent / elapsedSeconds;
                            receiveSpeed = deltaRecv / elapsedSeconds;
                        }
                    }

                    _previousReadings[nic.Id] = (bytesSent, bytesReceived, now);

                    string ipAddress = string.Empty;
                    try
                    {
                        var ipProp = nic.GetIPProperties();
                        var unicast = ipProp.UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);
                        if (unicast != null)
                        {
                            ipAddress = unicast.Address.ToString();
                        }
                    }
                    catch
                    {
                        // Ignore IP query error
                    }

                    snapshots.Add(new NetworkSnapshot(
                        AdapterName: nic.Name,
                        Description: nic.Description,
                        SendSpeedBytesPerSec: Math.Round(sendSpeed, 0),
                        ReceiveSpeedBytesPerSec: Math.Round(receiveSpeed, 0),
                        TotalBytesSent: bytesSent,
                        TotalBytesReceived: bytesReceived,
                        Ipv4Address: ipAddress,
                        Timestamp: now));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to enumerate network interfaces.");
            }
        }

        return ValueTask.FromResult<IReadOnlyList<NetworkSnapshot>>(snapshots);
    }
}
