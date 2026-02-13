# Browser Router

A lightweight Windows utility that routes URLs to the right browser profile automatically.

When you click a link in Outlook, Teams, Slack, or any other app, Windows normally opens it in your default browser. Browser Router intercepts that URL and sends it to the correct browser and profile based on rules you define — silently if there's a clear match, or via a quick picker if there's ambiguity.

---

## Features

- **Rule-based routing** — Route URLs by domain, URL prefix, or regex pattern
- **Multiple profile support** — Works with Chrome, Edge, Firefox, Vivaldi, and Brave, including multiple profiles per browser
- **Auto-detection** — Automatically discovers your installed browsers and profiles
- **Quick picker** — A lightweight popup for choosing manually when no rule matches, with an "Always use this for this domain" option that creates a rule on the spot
- **System tray** — Runs quietly in the background; access settings from the tray icon
- **No elevation required** — Registers as a URL handler under your user account only (HKCU)

---

## How It Works

1. Browser Router registers itself as your default browser (http/https handler)
2. When any app opens a URL, Windows passes it to Browser Router
3. Browser Router checks your rules in order:
   - **1 match** → launches that browser profile silently
   - **2+ matches** → shows the picker filtered to matched profiles
   - **No match** → shows the picker with all your configured profiles
4. In the picker, checking "Always use this for this domain" saves a new rule immediately so the same domain routes automatically next time

---

## Installation

### Requirements

- Windows 10 or 11
- No admin rights required

### Download

Download the latest `BrowserRouter.exe` from the [Releases](../../releases) page.

### Setup

1. **Place the exe** somewhere permanent (e.g. `C:\Users\<you>\AppData\Local\BrowserRouter\`)

2. **Register as a URL handler** by running:
   ```
   BrowserRouter.exe --register
   ```
   A confirmation dialog will appear with a link to the next step.

3. **Set as default browser** — Windows does not allow apps to self-assign as default, so you need to do this manually:
   - Open **Windows Settings → Apps → Default apps**
   - Search for **Browser Router** and select it for both HTTP and HTTPS

4. **Open Settings** to configure your browsers and rules:
   ```
   BrowserRouter.exe --settings
   ```
   Or just double-click the exe with no arguments.

---

## Configuration

### Adding Browser Profiles

In the **Profiles** tab, click **Auto-Detect** to discover all installed browsers and profiles automatically. You can also add profiles manually by specifying the executable path and any command-line arguments (e.g. `--profile-directory="Profile 1"` for Chrome).

### Creating Rules

In the **Rules** tab you can create rules with three matching modes:

| Type | How it matches | Example pattern |
|------|----------------|-----------------|
| **Domain** | Exact domain or any subdomain | `github.com` |
| **Prefix** | URL starts with the given string | `https://intranet.company.com` |
| **Regex** | Full regex match against the URL | `.*\.corp\.example\.com.*` |

Rules are evaluated in order — use **Move Up / Move Down** to set priority. All matching rules contribute profiles (deduplicated), so you can layer rules to build up a profile set.

### Config File

Settings are stored at `%APPDATA%\BrowserRouter\config.json`. You can edit this file directly in any text editor if you prefer — the format is readable JSON.

---

## Uninstalling

To remove the URL handler registration:
```
BrowserRouter.exe --unregister
```

Then go to **Windows Settings → Apps → Default apps** and reassign your preferred browser for HTTP/HTTPS. You can delete the exe afterwards.

---

## Building from Source

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download).

```powershell
# Debug build
dotnet build BrowserRouter/BrowserRouter.csproj

# Self-contained single .exe
dotnet publish BrowserRouter/BrowserRouter.csproj -c Release
```

The published exe will be at `BrowserRouter/bin/Release/net8.0-windows/win-x64/publish/BrowserRouter.exe`.

---

## License

MIT — see [LICENSE](LICENSE).
