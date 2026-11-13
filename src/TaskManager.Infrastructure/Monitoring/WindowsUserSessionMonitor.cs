using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using TaskManager.Application.Monitoring.Interfaces;

namespace TaskManager.Infrastructure.Monitoring;

[SupportedOSPlatform("windows")]
public class WindowsUserSessionMonitor : IUserSessionMonitor
{
    private readonly ILogger<WindowsUserSessionMonitor> _logger;

    #region Win32 WTS APIs
    private static readonly IntPtr WTS_CURRENT_SERVER_HANDLE = IntPtr.Zero;

    private enum WTS_CONNECTSTATE_CLASS
    {
        WTSActive,
        WTSConnected,
        WTSConnectQuery,
        WTSShadow,
        WTSDisconnected,
        WTSIdle,
        WTSListen,
        WTSReset,
        WTSDown,
        WTSInit
    }

    private enum WTS_INFO_CLASS
    {
        WTSInitialProgram,
        WTSApplicationName,
        WTSWorkingDirectory,
        WTSOEMId,
        WTSSessionId,
        WTSUserName,
        WTSWinStationName,
        WTSDomainName,
        WTSConnectState,
        WTSClientBuildNumber,
        WTSClientName,
        WTSClientDirectory,
        WTSClientProductId,
        WTSClientHardwareId,
        WTSClientAddress,
        WTSClientDisplay,
        WTSClientProtocolType
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WTS_SESSION_INFO
    {
        public int SessionId;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string pWinStationName;
        public WTS_CONNECTSTATE_CLASS State;
    }

    [DllImport("wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSEnumerateSessions(
        IntPtr hServer,
        int Reserved,
        int Version,
        ref IntPtr ppSessionInfo,
        ref int pCount);

    [DllImport("wtsapi32.dll")]
    private static extern void WTSFreeMemory(IntPtr pMemory);

    [DllImport("wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSQuerySessionInformation(
        IntPtr hServer,
        int sessionId,
        WTS_INFO_CLASS wtsInfoClass,
        out IntPtr ppBuffer,
        out int pBytesReturned);

    [DllImport("wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSDisconnectSession(IntPtr hServer, int sessionId, bool bWait);
    #endregion

    public WindowsUserSessionMonitor(ILogger<WindowsUserSessionMonitor> logger)
    {
        _logger = logger;
    }

    public ValueTask<IReadOnlyList<UserSessionSnapshot>> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var list = new List<UserSessionSnapshot>();
        var now = DateTime.UtcNow;

        IntPtr pSessionInfo = IntPtr.Zero;
        int count = 0;

        try
        {
            if (WTSEnumerateSessions(WTS_CURRENT_SERVER_HANDLE, 0, 1, ref pSessionInfo, ref count))
            {
                int structSize = Marshal.SizeOf<WTS_SESSION_INFO>();
                long currentPtr = pSessionInfo.ToInt64();

                var processes = Process.GetProcesses();
                var processesBySession = processes.GroupBy(p =>
                {
                    try { return p.SessionId; }
                    catch { return -1; }
                }).ToDictionary(g => g.Key, g => g.ToList());

                for (int i = 0; i < count; i++)
                {
                    var sessionInfo = Marshal.PtrToStructure<WTS_SESSION_INFO>(new IntPtr(currentPtr));
                    currentPtr += structSize;

                    // Query user name
                    string userName = string.Empty;
                    if (WTSQuerySessionInformation(WTS_CURRENT_SERVER_HANDLE, sessionInfo.SessionId, WTS_INFO_CLASS.WTSUserName, out var pUserName, out _))
                    {
                        userName = Marshal.PtrToStringAuto(pUserName) ?? string.Empty;
                        WTSFreeMemory(pUserName);
                    }

                    // Query client name
                    string clientName = string.Empty;
                    if (WTSQuerySessionInformation(WTS_CURRENT_SERVER_HANDLE, sessionInfo.SessionId, WTS_INFO_CLASS.WTSClientName, out var pClientName, out _))
                    {
                        clientName = Marshal.PtrToStringAuto(pClientName) ?? string.Empty;
                        WTSFreeMemory(pClientName);
                    }

                    if (string.IsNullOrWhiteSpace(userName))
                    {
                        // Check if console session
                        if (sessionInfo.State == WTS_CONNECTSTATE_CLASS.WTSActive)
                            userName = Environment.UserName;
                        else
                            continue; // Skip headless system services listener sessions
                    }

                    int procCount = 0;
                    long memBytes = 0;
                    if (processesBySession.TryGetValue(sessionInfo.SessionId, out var sessionProcs))
                    {
                        procCount = sessionProcs.Count;
                        foreach (var p in sessionProcs)
                        {
                            try { memBytes += p.WorkingSet64; } catch { }
                        }
                    }

                    list.Add(new UserSessionSnapshot(
                        SessionId: sessionInfo.SessionId,
                        UserName: userName,
                        SessionName: sessionInfo.pWinStationName ?? "Console",
                        State: sessionInfo.State.ToString().Replace("WTS", ""),
                        ClientName: string.IsNullOrEmpty(clientName) ? "Local" : clientName,
                        ProcessCount: procCount,
                        CpuPercent: 0.0,
                        MemoryBytes: memBytes,
                        Timestamp: now));
                }

                foreach (var p in processes) p.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enumerate WTS user sessions. Falling back to current user.");
        }
        finally
        {
            if (pSessionInfo != IntPtr.Zero)
            {
                WTSFreeMemory(pSessionInfo);
            }
        }

        if (list.Count == 0)
        {
            list.Add(new UserSessionSnapshot(
                SessionId: Process.GetCurrentProcess().SessionId,
                UserName: Environment.UserName,
                SessionName: "Console",
                State: "Active",
                ClientName: "Local",
                ProcessCount: Process.GetProcesses().Length,
                CpuPercent: 0.0,
                MemoryBytes: 0,
                Timestamp: now));
        }

        return ValueTask.FromResult<IReadOnlyList<UserSessionSnapshot>>(list);
    }

    public ValueTask<bool> DisconnectSessionAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            bool result = WTSDisconnectSession(WTS_CURRENT_SERVER_HANDLE, sessionId, false);
            return ValueTask.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to disconnect session {SessionId}", sessionId);
            return ValueTask.FromResult(false);
        }
    }
}
