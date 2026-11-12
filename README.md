# PCore

**PCore** is a Windows desktop application for PC hardware monitoring, performance analysis, and gaming and productivity tools.

> One PC. One Core. Complete Control.

## Current status

PCore is in active development. The repository currently contains a WPF desktop application shell with a dark interface, section navigation, and a Hardware page that reads system information through Windows Management Instrumentation (WMI).

The Dashboard now reads:
- CPU load through the Windows `Processor / % Processor Time / _Total` performance counter.
- Used and total physical memory through Windows performance counters and WMI.
- Used and total capacity across ready fixed drives.
- Values refresh once per second while the application is open.

The first CPU sample is intentionally treated as a warm-up sample. GPU load, temperatures, fan speeds, and clock frequencies are not shown until a supported hardware sensor provider is connected. Unavailable data is never replaced with fabricated values.

## Technology

- C#
- WPF
- .NET Framework 4.8
- Visual Studio 2022
- Windows Performance Counters and WMI for current system readings
- Planned: LibreHardwareMonitor for supported hardware sensors
- Planned: SQLite for local monitoring history

## Requirements

- Windows 10
- Visual Studio 2022 Community with the .NET desktop development workload
- .NET Framework 4.8 Developer Pack / Targeting Pack

## Build

1. Clone or download this repository.
2. Open `PCore.sln` in Visual Studio 2022.
3. Restore any prompted components or install the .NET Framework 4.8 Developer Pack if needed.
4. Select **Release** or **Debug** and **Any CPU**.
5. Build the solution and run the PCore project.

GitHub Actions is configured to build the solution on pushes and pull requests. A successful cloud build must be confirmed in the Actions tab before a release is considered verified.

## Planned modules

- Dashboard with live system readings
- CPU and GPU details, temperatures, clocks, and sensor support
- RAM, disk, and network monitoring
- Process information and process management
- Hardware information from Windows WMI
- Gaming Center and PCore Overlay
- PCore Widgets and Quick Menu
- Tools, benchmark, and system alerts
- Local monitoring history in a JSON file (SQLite migration remains planned)
- Initial settings for refresh interval and notification preference; themes, colors, and language customization remain planned
- PRO and MAX optional subscription features
- Windows installer and update mechanism

## Project principles

- Core monitoring features should remain free.
- Optional PRO and MAX plans may add extended analytics, longer history, reports, and customization.
- Store monitoring history locally unless the user explicitly chooses otherwise.
- Never display fabricated hardware readings.
- Keep the interface responsive and handle unavailable sensors safely.

---

PCore is an independent project and is not affiliated with Microsoft or hardware manufacturers.
