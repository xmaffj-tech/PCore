# PCore

PCore is a Windows desktop control center built with C#, WPF and .NET Framework 4.8.

## Current implementation

- Dashboard: CPU load, RAM usage, aggregate fixed-drive usage, and a best-effort Windows GPU Engine 3D utilization counter.
- Hardware: CPU/GPU names, installed RAM, physical disk models and Windows version through WMI.
- Network: current upload/download rate and session byte counters from Windows network interfaces.
- Processes: process list, PID, working-set memory and accumulated CPU time.
- Storage: ready Windows volumes, file system, used/free capacity and percentage.
- Startup Manager: read-only listing of common Run registry keys and Startup folders.
- Gaming Center: best-effort discovery of registered games and launchers from uninstall registry entries.
- Benchmark: short CPU arithmetic diagnostic. It is not a standardized benchmark score.
- System Alerts: in-app alerts when CPU or RAM usage reaches 90%, with a five-minute cooldown.
- History: local JSON history in %LOCALAPPDATA%\PCore\history.json, sampled every ten seconds.
- Settings: refresh interval and notification preference in local JSON.
- License status: Free mode only until a secure server-side license service and payment provider are configured.

## Important limitations

GPU utilization is best-effort and depends on Windows GPU Engine performance counters. GPU temperature, clocks, fan speed and per-card sensor readings are not yet implemented; unavailable values must remain unavailable. LibreHardwareMonitor integration is still pending.

History is still JSON, not SQLite. Historical graphs, full statistics and retention controls are pending.

Overlay, desktop widgets and global Quick Menu hotkeys are not yet full implementations. The current compact panel is an ordinary always-on-top WPF window, not an in-game overlay.

Startup Manager is read-only. It does not disable or delete startup entries. Storage tools report capacity only and do not delete files.

FREE / PRO / MAX pricing is a product plan draft. Paid activation is intentionally disabled; do not treat a local setting as proof of payment. A production release needs a payment provider, server-side entitlement validation, signing and privacy/security review.

## Technology

- C# 7.3
- WPF
- .NET Framework 4.8
- Windows Performance Counters
- WMI / System.Management
- Windows Registry and NetworkInformation APIs
- GitHub Actions / Windows MSBuild runner

## Build

Open PCore.sln in Visual Studio 2022 with the .NET desktop development workload and .NET Framework 4.8 targeting pack. Select Release and Any CPU, then build.

GitHub Actions builds the solution on pushes and pull requests. Check the Actions tab for the actual build result before treating a commit as verified:
https://github.com/xmaffj-tech/PCore/actions

## Roadmap

- LibreHardwareMonitor integration for real GPU, CPU and board sensors.
- SQLite history migration and query/index layer.
- Time-series charts, aggregates and export.
- Full GPU, CPU, RAM and disk detail pages.
- Real overlay with tested game compatibility and configurable hotkeys.
- Desktop widgets and Quick Menu.
- Storage analysis, cleanup preview and safe user-confirmed actions.
- Startup entry management with confirmation and restore.
- Multi-language resources, theme customization and accessibility.
- Secure licensing backend and payment integration.
- Signed Windows installer and update channel.
- Automated tests for monitoring calculations and data stores.

## Principles

- Never fabricate hardware readings.
- Prefer read-only system inspection by default.
- Require confirmation before destructive actions.
- Keep monitoring history local unless the user explicitly chooses otherwise.
- Do not claim a feature is complete until it has been built and tested.

PCore is an independent project and is not affiliated with Microsoft or hardware manufacturers.
