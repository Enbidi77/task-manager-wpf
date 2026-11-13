# Modern Windows Task Manager

A high-performance, modern Windows Task Manager built with **WPF**, **.NET 10 LTS**, and **Windows 11 Fluent Design System**. Engineered with **Clean Architecture**, asynchronous reactive monitoring, in-place differential rendering, and local SQLite persistence.

---

## Key Features

- **Dashboard**:
  - Real-time telemetry metric cards: Overall CPU %, Memory (Used / Available / Total), Disk Active %, Network Throughput (Upload / Download), GPU %, System Uptime, Process & Thread count.
  - 60-second rolling historical line/area charts powered by SkiaSharp.
  - High-consumption warning indicators triggered automatically when CPU or RAM exceeds 80%.
  - High-level system statistics: Process, Thread, and Handle counts.

- **Processes**:
  - Virtualized DataGrid (`VirtualizationMode="Recycling"`) for smooth scrolling through hundreds of processes.
  - Columns: Name, PID, Status, CPU %, Memory Working Set, Disk Throughput, Network Mbps, Architecture, Path.
  - Real-time search by Process Name or PID.
  - In-place differential updates (`_processMap` with PID and start-time compound key) preventing selection or scroll loss during refresh ticks.
  - Context menu & actions:
    - End Process & End Process Tree (integrated with `ConfirmationDialog` and access-denied handling).
    - Set Priority Class (RealTime, High, AboveNormal, Normal, BelowNormal, Idle).
    - Open File Location in Windows Explorer.
    - Copy Process Details to clipboard.
    - Manual Refresh.

- **Performance**:
  - Master-detail hardware telemetry for **CPU**, **Memory**, **Disk**, **Network**, and **GPU**.
  - CPU: Overall utilization %, per-core utilization meters, core & logical processor count, base clock, uptime.
  - Memory: Physical used/available, committed limit, paged/non-paged pool, 60s historical chart.
  - Disk: Active time %, read/write throughput (MB/s), model identification, total and available capacity.
  - Network: Real-time send/receive throughput (Mbps), adapter name, IPv4 address, total bytes sent/received.
  - GPU: Utilization %, temperature (°C), dedicated video memory, and shared memory.

- **Services**:
  - Windows Services listing: Name, Display Name, Status, Startup Type, Description.
  - Search & filter by service name or status.
  - Start, Stop, and Restart commands with real-time feedback and elevation error handling.

- **Startup Apps**:
  - Detection of startup applications across registry hives (`HKCU`, `HKLM`) and Startup folders.
  - Search & filter by name or publisher.
  - Enable / Disable toggling of startup entries.
  - Open File Location in Explorer.

- **Users**:
  - Active user session enumeration via Win32 Terminal Services (`WTSEnumerateSessions`, `WTSQuerySessionInformation`).
  - Active session stats: User name, Session ID, State, Process count, and Total memory.
  - Disconnect session command.

- **Settings & Persistence**:
  - Theme switching: Fluent **Dark**, **Light**, and **System** default modes.
  - Monitoring Refresh Interval picker: 500ms (Fast), 1000ms (Normal), 2000ms (Medium), 5000ms (Slow) with dynamic timer adjustment.
  - Application Behavior: Always on top, Confirm before ending processes, Minimize to tray, Start minimized.
  - History Retention: Configurable SQLite metric cache retention (6h, 12h, 24h, 3d, 7d) and immediate purge.
  - Diagnostic Log Viewer: Embedded log viewer reading Serilog files, with log level filtering, search querying, file clearing, and folder opening.

---

## Tech Stack & Architecture

- **Platform**: .NET 10 LTS (`net10.0`, `net10.0-windows`)
- **UI Framework**: Windows Presentation Foundation (WPF) with Windows 11 Fluent Design styles
- **Architectural Pattern**: Clean Architecture (Domain, Application, Infrastructure, App) + MVVM (`CommunityToolkit.Mvvm`)
- **Hosting & DI**: `Microsoft.Extensions.Hosting`, `Microsoft.Extensions.DependencyInjection`
- **Persistence**: `Microsoft.EntityFrameworkCore.Sqlite` (`%APPDATA%/TaskManager/taskmanager.db`)
- **Charts & Visualization**: `LiveChartsCore.SkiaSharpView.WPF`
- **Logging**: `Serilog.Extensions.Hosting` & `Serilog.Sinks.File`
- **Native Interop**: Windows P/Invoke, `System.Management` (WMI), `System.Diagnostics.PerformanceCounter`, `System.ServiceProcess.ServiceController`

---

## Project Structure

```
task-manager-wpf/
├── src/
│   ├── TaskManager.Domain/         # Enterprise business entities, domain models, common base classes
│   ├── TaskManager.Shared/         # Cross-cutting primitives, enums, Result pattern
│   ├── TaskManager.Application/    # Monitoring interfaces, repository contracts, view models, DTOs
│   ├── TaskManager.Infrastructure/ # Win32/WMI monitors, EF Core DbContext, SQLite repositories, Serilog services
│   └── TaskManager.App/            # WPF UI, Windows 11 Fluent Themes, Custom Controls, Converters, Views
├── tests/
│   ├── TaskManager.Domain.Tests/
│   ├── TaskManager.Application.Tests/
│   ├── TaskManager.Infrastructure.Tests/
│   └── TaskManager.App.Tests/
├── scripts/
│   └── publish.ps1                 # Single-file self-contained win-x64 release build script
└── docs/
    ├── architecture.md             # Clean architecture, concurrency, and persistence guide
    ├── development-guide.md        # Dev setup, coding standards, and extension instructions
    └── feature-roadmap.md          # Near, mid, and long-term milestones
```

---

## Building & Running

### Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows 10 (version 1809+) or Windows 11

### Build the Solution
```powershell
dotnet restore TaskManager.sln
dotnet build TaskManager.sln -c Debug
```

### Run the Application
```powershell
dotnet run --project src/TaskManager.App/TaskManager.App.csproj
```

### Publish Self-Contained Single-File Executable
```powershell
powershell -ExecutionPolicy Bypass -File scripts/publish.ps1
```
The output executable will be created at `publish/win-x64/TaskManager.App.exe`.
