# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```powershell
# Debug build
dotnet build BrowserRouter/BrowserRouter.csproj

# Release build (self-contained single .exe)
dotnet publish BrowserRouter/BrowserRouter.csproj -c Release

# Run in settings mode
dotnet run --project BrowserRouter/BrowserRouter.csproj -- --settings

# Register as default browser
dotnet run --project BrowserRouter/BrowserRouter.csproj -- --register

# Simulate routing a URL
dotnet run --project BrowserRouter/BrowserRouter.csproj -- https://example.com
```

The published `.exe` ends up at `BrowserRouter/bin/Release/net8.0-windows/win-x64/publish/BrowserRouter.exe`.

> **Note:** `dotnet` may not be on the bash `PATH` in this environment — use PowerShell or a developer command prompt if bash can't find it.

## Architecture

This is a single-exe WPF + WinForms hybrid app (.NET 8, `net8.0-windows`). It needs both `<UseWPF>true</UseWPF>` and `<UseWindowsForms>true</UseWindowsForms>` — WPF for the windows, WinForms for `NotifyIcon` (system tray).

### Execution Modes (dispatched in `App.xaml.cs`)

| Invocation | Behavior |
|---|---|
| `BrowserRouter.exe <url>` | Route the URL (main use case) |
| `BrowserRouter.exe --settings` | Open settings window + tray |
| `BrowserRouter.exe --register` | Write HKCU registry entries, then exit |
| `BrowserRouter.exe --unregister` | Remove HKCU registry entries, then exit |
| `BrowserRouter.exe` (no args) | Open settings window + tray |

### URL Routing Flow

`App.RouteUrl()` → `UrlRouter.Route()` → result:
- **1 profile matched**: launch directly, exit
- **2+ profiles matched**: show `PickerWindow` filtered to matched profiles
- **0 profiles matched**: show `PickerWindow` with all configured profiles
- **0 profiles configured**: prompt to open Settings

### Configuration

Stored at `%APPDATA%\BrowserRouter\config.json`. `ConfigService` handles load/save with camelCase JSON. The root model is `AppConfig` containing `List<BrowserProfile>` and `List<UrlRule>`.

`UrlRule.PatternType` is an enum: `Domain` (suffix match, subdomain-aware), `Regex` (matched against full URL), `Prefix` (URL starts-with). Rules are evaluated in order; **all** matching rules contribute profiles (deduplicated). The picker's "Always use this for this domain" checkbox appends a new `Domain` rule to config.

### Windows Registry (HKCU only, no elevation required)

`RegistryService` writes to:
- `HKCU\Software\Classes\BrowserRouterURL\` — ProgID
- `HKCU\Software\Classes\http\` and `https\` — protocol handlers
- `HKCU\Software\BrowserRouter\Capabilities\` — app capabilities
- `HKCU\Software\RegisteredApplications\` — registration entry

After `--register`, the user must manually go to **Windows Settings → Apps → Default apps** and select Browser Router — Windows prevents apps from self-assigning as default browser.

### Browser Auto-Detection (`BrowserDetector`)

Reads `%LOCALAPPDATA%\<Browser>\User Data\Local State` JSON for Chromium-family browsers (Chrome, Edge, Vivaldi, Brave) to extract profile folder names and display names. Reads `%APPDATA%\Mozilla\Firefox\profiles.ini` for Firefox. Detected profiles are suggested in the Settings UI via the "Auto-Detect" button.

### Key Coupling Points

- `PickerWindow` receives the live `AppConfig` reference and `ConfigService` so it can persist "remember" rules immediately without going through Settings.
- `TrayIconManager` uses `System.Windows.Forms.NotifyIcon` and lives for the app lifetime; it is only created when running in tray mode (settings or no-args invocation), not in URL-routing mode which exits immediately after launch.
- `SettingsWindow` directly mutates the `AppConfig` object; changes are only written to disk on "Save" or "Save & Close".
