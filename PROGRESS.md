# Browser Router — Progress

## Status: Initial Implementation Complete (not yet built/tested)

---

## Completed

### Project Scaffolding
- [x] `BrowserRouter.sln` — solution file
- [x] `BrowserRouter/BrowserRouter.csproj` — .NET 8 WPF + WinForms, self-contained publish configured

### Models (`BrowserRouter/Models/`)
- [x] `AppConfig` — root config model (`defaultProfile`, `browsers`, `rules`)
- [x] `BrowserProfile` — `id`, `name`, `executable`, `args`, `icon`
- [x] `UrlRule` — `pattern`, `patternType` (Domain/Regex/Prefix), `profiles`, `comment`

### Services (`BrowserRouter/Services/`)
- [x] `ConfigService` — load/save `%APPDATA%\BrowserRouter\config.json` with camelCase JSON
- [x] `UrlRouter` — rule evaluation engine; all matching rules aggregate profiles; deduplication preserves order
- [x] `BrowserLauncher` — `Process.Start` with profile args; handles args with spaces
- [x] `RegistryService` — HKCU register/unregister; `IsRegistered()` check
- [x] `BrowserDetector` — auto-detect Chromium profiles via `Local State` JSON; Firefox profiles via `profiles.ini`

### Entry Point (`App.xaml` / `App.xaml.cs`)
- [x] Argument dispatch: `<url>` → route, `--settings`, `--register`, `--unregister`, no-args → settings+tray
- [x] Full routing flow: 1 match → silent launch, 2+ → filtered picker, 0 → full picker, 0 profiles → prompt
- [x] "Remember" rule auto-save from picker flows back through `ConfigService`

### UI
- [x] `PickerWindow` — profile buttons, URL display, "Always use this for this domain" checkbox
- [x] `SettingsWindow` — 4 tabs: Profiles, Rules, Registration, Log
  - [x] Profiles tab: list, add/edit/delete form, browse exe, auto-detect button
  - [x] Rules tab: list, add/edit/delete form, multi-select profile list, move up/down
  - [x] Registration tab: register/unregister buttons, status indicator, open Default Apps link, config path
  - [x] Log tab: timestamped entries, clear button
  - [x] Dirty-state tracking with save prompt on close

### Tray
- [x] `TrayIconManager` — `NotifyIcon` with double-click → Settings, context menu, recent URLs (last 10)
- [x] Fallback icon (blue "B" bitmap) when `Resources/icon.ico` is absent

### Documentation
- [x] `CLAUDE.md` — build commands, architecture overview, key coupling points
- [x] `PRD.md` — problem statement, requirements, non-requirements, success criteria
- [x] `PROGRESS.md` — this file

---

## Not Yet Done

### Must-do before first real use
- [ ] **Build verification** — project has not been compiled; resolve any build errors
- [ ] **Icon** — `BrowserRouter/Resources/icon.ico` is missing; fallback bitmap is functional but ugly
- [ ] **End-to-end test** — register, set as default in Windows Settings, open a URL from an external app
- [ ] **Picker double-fire bug risk** — `PickerWindow.OnClosed` fires `Cancelled` even after `ProfileSelected`; needs guard flag

### Quality / Polish
- [ ] **Settings window: default profile selector** — `AppConfig.defaultProfile` is modelled but not surfaced in the UI; currently unused in routing
- [ ] **Settings window: profile ID editing** — IDs are auto-generated from name on create; no way to change them without editing JSON directly
- [ ] **Log tab wiring** — `SettingsWindow.AddLogEntry()` exists but `App.xaml.cs` never calls it; routing decisions are not logged
- [ ] **Tray recent URLs: re-open action** — menu items show recent URLs but clicking them does nothing; intended to re-open in a different profile
- [ ] **Startup shortcut** — no mechanism to add the app to Windows startup (needed for persistent tray)
- [ ] **Rule test / preview** — no way to test a pattern against a sample URL before saving
- [ ] **Config backup** — no backup before save; a bad edit can corrupt the config with no recovery path
- [ ] **Error reporting** — silent `catch {}` blocks in BrowserDetector and PickerWindow hide failures; consider logging to Log tab

### Stretch / Future
- [ ] Opera and Opera GX detection in `BrowserDetector`
- [ ] Arc browser support (macOS-first but has Windows preview)
- [ ] Import/export config
- [ ] Per-rule "open in private/incognito" flag (appended as a profile arg)
- [ ] `defaultProfile` routing — use it as fallback when no rules match instead of always showing picker
- [ ] Keyboard shortcut in picker (number keys 1–9 to select profile)
- [ ] Windows startup auto-registration option in Settings UI

---

## Known Issues

| # | Description | Severity |
|---|---|---|
| 1 | `PickerWindow.OnClosed` always fires `Cancelled` because `ProfileSelected` field is never set to null after invoke — double-fire risk | Medium |
| 2 | `Resources/icon.ico` not present; tray icon is a GDI handle from `GetHicon()` which is never released (GDI leak) | Low |
| 3 | `dotnet` not on bash PATH in current dev environment — must use PowerShell to build | Low (dev only) |
| 4 | `SettingsWindow` loads a fresh config from disk on open; if `PickerWindow` saved a new rule concurrently, reopening Settings will pick it up but the in-memory config in `App.xaml.cs` will be stale | Low |
