# Browser Router — Product Requirements Document

## Problem

Knowledge workers who use multiple browser profiles (personal vs. work, multiple client accounts) are constantly fighting links opening in the wrong profile. Every link clicked from Outlook, Teams, Slack, or any other app opens in whatever browser Windows happens to use — usually the wrong one, requiring a copy-paste detour.

Browser extensions cannot solve this: they only intercept navigation within their own browser instance and have no ability to launch a different browser or intercept OS-level URL opens.

## Solution

A lightweight Windows utility that registers itself as the system default browser. When any application opens a URL, Browser Router intercepts it and either:

1. **Routes it silently** to the correct browser profile based on configurable rules, or
2. **Shows a picker** letting the user choose a profile when no rule matches (or when multiple profiles are relevant)

The picker doubles as a learning tool — a single checkbox converts any ad-hoc choice into a permanent rule.

---

## Users

Single-user, personal productivity tool. Designed for developers, IT professionals, and anyone who actively maintains multiple browser profiles for different contexts (personal, work, clients, sandboxes).

---

## Core Requirements

### R1 — URL Interception
- Must register as a Windows default browser via HKCU registry (no elevation required)
- Must receive URLs from any application that uses the OS `ShellExecute`/`CreateProcess` mechanism
- Windows will always pass a single URL as the first argument: `BrowserRouter.exe "<url>"`

### R2 — Rule-Based Routing
- Rules match URLs by: **domain suffix** (subdomain-aware), **regex** (full URL), or **prefix**
- Rules are evaluated in order; all matching rules across the list contribute profiles
- Duplicate profile IDs from multiple matching rules are deduplicated, order preserved
- A single matched profile launches immediately with no UI shown
- Two or more matched profiles show a filtered picker

### R3 — Profile Picker
- Shows when: no rules match (full profile list), or multiple profiles matched (filtered list)
- Displays profile name and is keyboard-navigable
- "Always use this for this domain" checkbox: on confirm, appends a `Domain` rule to config and saves immediately
- Cancelling a picker closes the app without opening anything

### R4 — Browser Profile Support
- Supports Chromium-family browsers (Chrome, Edge, Vivaldi, Brave, Opera) via `--profile-directory=<folder>`
- Supports Firefox via `--profile <path> -no-remote`
- Any browser with a CLI-invocable profile argument can be added manually
- Auto-detect reads `Local State` JSON (Chromium) and `profiles.ini` (Firefox) to enumerate installed profiles

### R5 — Settings UI
- **Profiles tab**: add, edit, delete profiles; auto-detect installed profiles
- **Rules tab**: add, edit, delete rules; reorder by priority (move up/down); multi-profile assignment
- **Registration tab**: one-click register/unregister; link to Windows Default Apps settings; config file path
- **Log tab**: recent routing decisions for debugging
- Changes are not persisted until explicitly saved ("Save" or "Save & Close")

### R6 — Configuration
- Config stored at `%APPDATA%\BrowserRouter\config.json`
- Human-readable JSON, manually editable
- Survives app updates (no migration needed for additive changes)

### R7 — System Tray
- Icon present when app is running in settings/tray mode
- Double-click opens Settings
- Context menu: Settings, Recent URLs (last 10), Exit
- Not present during silent URL routing (process exits immediately after launch)

### R8 — Startup Performance
- URL routing path must be fast — it is in the critical path of every clicked link
- No heavy initialization in router mode; config is loaded once per URL open
- Self-contained single `.exe` (no runtime install, no installer)

---

## Non-Requirements (Explicitly Out of Scope)

- **No browser extension** — cannot intercept OS-level URL opens
- **No HKLM / machine-wide registration** — HKCU is sufficient and requires no elevation
- **No auto-start on login** — user can add to Startup manually if desired (tray mode only makes sense if they do)
- **No sync or cloud config** — local only
- **No incognito/private window routing** — can be added as a profile arg if the user wants it
- **No URL rewriting** — routes as-is, does not modify the URL
- **No telemetry**

---

## Constraints

- Windows only (WPF + WinForms, `net8.0-windows`)
- The user must manually complete default browser assignment in Windows Settings after `--register` — Windows does not allow programmatic self-assignment
- Profile directory names (e.g. `Default`, `Profile 1`) are internal folder names, not the display names shown in the browser UI

---

## Success Criteria

1. A URL opened from Outlook/Teams/Slack reaches the correct browser profile without manual copy-paste
2. When no rule matches, the picker appears within ~500ms of clicking the link
3. A new profile and routing rule can be configured in under 2 minutes by a first-time user
4. The `config.json` is legible and editable in a text editor as a fallback
