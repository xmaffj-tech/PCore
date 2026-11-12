# PCore

**PCore** is a Windows desktop application for PC hardware monitoring, performance analysis, and gaming and productivity tools.

> One PC. One Core. Complete Control.

## Current status

PCore is at the initial foundation stage. The repository currently contains a WPF desktop application shell with a dark interface and working section navigation.

Hardware values are intentionally shown as unavailable until real data providers are connected. PCore will not display fabricated monitoring values.

## Technology

- C#
- WPF
- .NET Framework 4.8
- Visual Studio 2022
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
4. Select **Debug** and **Any CPU**.
5. Build the solution and run the PCore project.

## Planned modules

- Dashboard
- CPU, GPU, RAM, disk, and network monitoring
- Process and hardware information
- Gaming Center and PCore Overlay
- Tools and benchmark
- Monitoring history
- Settings and customization

## Project principle

Core monitoring features should remain free. Optional PRO and MAX plans may add extended analytics, longer history, reports, and customization.

---

PCore is an independent project and is not affiliated with Microsoft or hardware manufacturers.
