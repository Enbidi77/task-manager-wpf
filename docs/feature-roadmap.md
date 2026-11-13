# Feature Roadmap

This roadmap outlines near-term, mid-term, and long-term milestones for the Modern Windows Task Manager.

---

## 1. Near-Term Milestones (v1.1 - v1.2)

### 1.1 Notification & Custom Thresholds Engine
- **Configurable Thresholds**: Define user alerts when CPU, Memory, or Disk metrics exceed customizable limits for sustained durations (e.g., CPU > 90% for 30 seconds).
- **Windows Toast Notifications**: Dispatch native Windows 11 action center toasts for hardware threshold breaches.

### 1.2 System Tray & Mini-Window Mode
- **Taskbar Tray Icon**: Render real-time miniature CPU usage graph directly in the Windows taskbar notification tray.
- **Compact Overlay / Always-On-Top Mini Bar**: Floating widget mode displaying core hardware gauges (CPU, RAM, GPU) with minimal desktop footprint.

### 1.3 Battery & Power Consumption Tracking
- **Battery Health Telemetry**: Charge percentage, discharge rate (mW), time remaining estimation, cycle count, and designed vs. actual full capacity.
- **Process Power Impact Estimation**: Attribute active CPU cycles and GPU rendering to calculate per-process estimated battery draw.

---

## 2. Mid-Term Milestones (v1.3 - v2.0)

### 2.1 Process Deep Inspection
- **Loaded Modules & DLLs**: Enumerate dynamic link libraries loaded by selected processes with digital signatures and version details.
- **Open Handles**: Search and inspect open files, registry keys, and synchronization mutexes held by specific processes.
- **Thread Inspector**: View active thread stack traces, wait reasons, context switches, and individual thread CPU usage.

### 2.2 Network Sockets & Active Connections
- **Active TCP/UDP Endpoints**: Real-time display of active network connections, local addresses, remote endpoints, state (Established, Listen, TimeWait), and associated PID.
- **Geo-IP Resolution**: Map remote IPs to countries and domain names for outgoing network connections.

### 2.3 Process Tree & Dependency Visualizer
- **Interactive Tree Visualizer**: Visual graph hierarchy rendering parent/child process chains, process lineages, and child process trees.
- **Batch Group Actions**: Suspend, resume, or throttle entire process trees simultaneously.

---

## 3. Long-Term Milestones (v2.1+)

### 3.1 Event Tracing for Windows (ETW) Integration
- **Kernel-Level File & Disk I/O Tracing**: Capture microsecond disk access spikes and file access latencies using Microsoft.Diagnostics.Tracing.
- **Page Fault Tracking**: Granular analysis of hard vs. soft memory page faults per application.

### 3.2 Remote Host Telemetry
- **Agentic / gRPC Remote Monitoring**: Monitor remote Windows workstations, Hyper-V VMs, or Windows Server instances over secure gRPC or SSH tunnels.
- **Multi-Host Switching**: Switch dashboard views seamlessly between local and remote machines.

### 3.3 AI-Powered Anomaly & Runaway Detection
- **Local On-Device Analysis**: Identify abnormally behaving processes, potential crypto-miners, memory leaks, or erratic handle consumption patterns using baseline telemetry comparisons.
- **Intelligent Recommendations**: Suggest optimal affinity masks or priority adjustments for background developer builds.
