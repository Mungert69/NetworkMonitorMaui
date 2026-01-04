# NetworkMonitorMaui

Shared MAUI UI layer for NetworkMonitor clients. This project supplies the UI
controls, view models, and platform services that host the agent and surface
monitor state in the desktop/mobile apps.

## Key folders
- `Controls/` custom UI controls and indicators.
- `ViewModels/` MVVM view models for pages and dialogs.
- `Services/` platform services, background services, dispatchers.
- `Helpers/` asset/config helpers for MAUI bootstrapping.
- `Utils/` converters and small UI utilities.

## Relationship to other projects
- References `NetworkMonitorProcessorAgent` for agent runtime integration.
- Consumes `NetworkMonitorLib` types for command processors and shared objects.
- Used by `QuantumSecure` (and similar MAUI shells) as the UI base.

## Build
```bash
dotnet restore
dotnet build NetworkMonitorMaui.csproj
```
