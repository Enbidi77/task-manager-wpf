using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using TaskManager.Application.Monitoring.Interfaces;
using TaskManager.Application.Monitoring.Models;

namespace TaskManager.Infrastructure.Monitoring;

[SupportedOSPlatform("windows")]
public class WindowsProcessMonitor : IProcessMonitor
{
    private readonly ILogger<WindowsProcessMonitor> _logger;
    private readonly Dictionary<string, ProcessHistoricalState> _history = new();
    private readonly object _syncRoot = new();

    private readonly int _processorCount = Environment.ProcessorCount;

    private record ProcessHistoricalState(
        TimeSpan TotalProcessorTime,
        DateTime Timestamp);

    #region Win32 Toolhelp for Parent PID mapping
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct PROCESSENTRY32
    {
        public uint dwSize;
        public uint cntUsage;
        public uint th32ProcessID;
        public IntPtr th32DefaultHeapID;
        public uint th32ModuleID;
        public uint cntThreads;
        public uint th32ParentProcessID;
        public int pcPriClassBase;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szExeFile;
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    private const uint TH32CS_SNAPPROCESS = 0x00000002;
    private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

    private Dictionary<int, int> GetParentProcessMapping()
    {
        var map = new Dictionary<int, int>();
        var handle = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
        if (handle == INVALID_HANDLE_VALUE) return map;

        try
        {
            var entry = new PROCESSENTRY32 { dwSize = (uint)Marshal.SizeOf<PROCESSENTRY32>() };
            if (Process32First(handle, ref entry))
            {
                do
                {
                    map[(int)entry.th32ProcessID] = (int)entry.th32ParentProcessID;
                }
                while (Process32Next(handle, ref entry));
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to capture Toolhelp snapshot for parent process mapping.");
        }
        finally
        {
            CloseHandle(handle);
        }

        return map;
    }
    #endregion

    public WindowsProcessMonitor(ILogger<WindowsProcessMonitor> logger)
    {
        _logger = logger;
    }

    public ValueTask<ProcessDeltaSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var parentMap = GetParentProcessMapping();
        var currentProcesses = Process.GetProcesses();

        var activeSnapshots = new List<ProcessSnapshot>(currentProcesses.Length);
        var currentKeys = new HashSet<string>(currentProcesses.Length);
        var currentPids = new HashSet<int>(currentProcesses.Length);

        lock (_syncRoot)
        {
            foreach (var p in currentProcesses)
            {
                try
                {
                    int pid = p.Id;
                    currentPids.Add(pid);

                    DateTime startTimeUtc;
                    try
                    {
                        startTimeUtc = p.StartTime.ToUniversalTime();
                    }
                    catch
                    {
                        startTimeUtc = DateTime.UnixEpoch;
                    }

                    // Key identity combining PID and start time to guard against PID reuse
                    string processKey = $"{pid}_{startTimeUtc.Ticks}";
                    currentKeys.Add(processKey);

                    string name = p.ProcessName;
                    string desc = string.Empty;
                    string execPath = string.Empty;

                    try
                    {
                        var mainModule = p.MainModule;
                        if (mainModule != null)
                        {
                            execPath = mainModule.FileName;
                            desc = mainModule.FileVersionInfo.FileDescription ?? string.Empty;
                        }
                    }
                    catch
                    {
                        // Protected / system processes throw Win32Exception on MainModule
                    }

                    if (string.IsNullOrEmpty(desc)) desc = name;

                    parentMap.TryGetValue(pid, out int parentPid);

                    // CPU Calculation
                    double cpuPercent = 0.0;
                    TimeSpan totalProcessorTime = TimeSpan.Zero;
                    try
                    {
                        totalProcessorTime = p.TotalProcessorTime;
                        if (_history.TryGetValue(processKey, out var prev))
                        {
                            double elapsedSeconds = (now - prev.Timestamp).TotalSeconds;
                            if (elapsedSeconds > 0.1)
                            {
                                double cpuSeconds = (totalProcessorTime - prev.TotalProcessorTime).TotalSeconds;
                                cpuPercent = Math.Clamp((cpuSeconds / (elapsedSeconds * _processorCount)) * 100.0, 0.0, 100.0);
                            }
                        }
                    }
                    catch
                    {
                        // Access denied on privileged processes
                    }

                    _history[processKey] = new ProcessHistoricalState(totalProcessorTime, now);

                    long workingSet = p.WorkingSet64;
                    long privateMemory = p.PrivateMemorySize64;
                    int threads = p.Threads.Count;
                    int handles = 0;
                    try { handles = p.HandleCount; } catch { }

                    string status = p.Responding ? "Running" : "Not Responding";

                    activeSnapshots.Add(new ProcessSnapshot(
                        ProcessId: pid,
                        StartTimeUtc: startTimeUtc,
                        ProcessKey: processKey,
                        Name: name,
                        Description: desc,
                        ExecutablePath: execPath,
                        ParentProcessId: parentPid > 0 ? parentPid : null,
                        CpuPercent: Math.Round(cpuPercent, 1),
                        WorkingSetBytes: workingSet,
                        PrivateMemoryBytes: privateMemory,
                        ReadSpeedBytesPerSec: 0,
                        WriteSpeedBytesPerSec: 0,
                        ThreadCount: threads,
                        HandleCount: handles,
                        UserName: Environment.UserName,
                        Status: status,
                        Timestamp: now));
                }
                catch (Exception ex)
                {
                    _logger.LogTrace(ex, "Failed to read process state for PID {Pid}", p.Id);
                }
                finally
                {
                    p.Dispose();
                }
            }

            // Prune dead history keys
            var deadKeys = _history.Keys.Where(k => !currentKeys.Contains(k)).ToList();
            var terminatedPids = new List<int>();
            foreach (var deadKey in deadKeys)
            {
                if (int.TryParse(deadKey.Split('_')[0], out int deadPid))
                {
                    terminatedPids.Add(deadPid);
                }
                _history.Remove(deadKey);
            }

            var delta = new ProcessDeltaSnapshot(
                ActiveProcesses: activeSnapshots,
                AddedProcessIds: Array.Empty<int>(),
                TerminatedProcessIds: terminatedPids,
                Timestamp: now);

            return ValueTask.FromResult(delta);
        }
    }

    public ValueTask<bool> TerminateProcessAsync(int processId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var proc = Process.GetProcessById(processId);
            proc.Kill(entireProcessTree: true);
            _logger.LogInformation("Terminated process PID {Pid}", processId);
            return ValueTask.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to terminate process PID {Pid}", processId);
            return ValueTask.FromResult(false);
        }
    }
}
