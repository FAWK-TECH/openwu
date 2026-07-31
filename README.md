# OpenWU — Universal Windows Update Manager (GUI + CLI)

> Open, auditable Windows Update control — GUI like WUMT, engine you can trust.

**Version:** 0.1.0 (see `Directory.Build.props` / `OpenWU.exe --version`)

OpenWU is a general-purpose, open-source Windows Update management tool built on **.NET 8** and native Windows Update Agent (WUA) COM APIs. It provides a classic desktop GUI (WinForms) for interactive update management alongside a powerful CLI mode with `--json` support for enterprise automation.

CLI `--json` always returns an **object envelope** (`ok`, `host`, `version`, `verb`, plus payload) — never a bare array — so scripts can branch on `ok` and parse `updates` / `health` reliably.

**Note:** This is an administrator utility for managing updates via official Microsoft APIs. It is not a substitute for organizational patch policy (Intune, WSUS, etc.).

---

## 🚀 Getting Started (GUI First)

**Double-click `OpenWU.exe`** (or launch from elevated terminal):

```cmd
OpenWU.exe
```

The graphical user interface opens with full administrator rights (via UAC manifest):

1. Click **Refresh (F5)** to scan for pending Windows Updates.
2. Use **Select Security** to instantly check all Critical/Security patches.
3. Double-click any row to view full KB details, release descriptions, and support URLs.
4. Click **Install** or **Hide** to manage updates safely.

---

## 💻 CLI Automation Mode

**Preferred:** dedicated console binary `openwu-cli.exe` (no WinForms flash, real stdout).

`OpenWU.exe` is **WinExe** (double-click opens GUI, no black console). It still accepts CLI verbs via parent-console attach, but scripts should use **`openwu-cli.exe`**.

> **Windows note:** You cannot ship both `OpenWU.exe` and `openwu.exe` — the filesystem is case-insensitive; they are the same path.

```cmd
# Test WUA session health & elevation status
openwu-cli.exe test --json

# List pending updates as JSON
openwu-cli.exe list --json

# Perform a WhatIf simulation for security updates
openwu-cli.exe install --security-only --whatif --json

# Silently install security updates and reboot if required
openwu-cli.exe install --security-only --reboot --json

# Hide specific KB articles and persist to policy
openwu-cli.exe hide --kb KB5031234 --persist

# View update history
openwu-cli.exe history --last 20 --json
```

---

## 🛡️ Safety & Policy Features

- **Central Policy Store**: Standardized configuration saved at `%ProgramData%\OpenWU\policy.json`.
- **Domain Controller Protection**: Blocks installation on Active Directory DCs unless explicitly allowed via policy or `--allow-domain-controller`.
- **Title Denial Filters**: Soft-denies updates containing keywords like `"Preview"` to prevent unstable builds from installing automatically.
- **Safe Defaults**: Drivers and optional updates are excluded by default.

---

## 🏗️ Building from Source

### Prerequisites
- .NET 8.0 SDK

### Build & Test
```powershell
dotnet build OpenWu.sln
dotnet test OpenWu.sln
```

### Publish Single-File Executable
```powershell
.\scripts\publish.ps1
```
The output binary will be created at `artifacts/win-x64/OpenWU.exe`.

---

## 📄 Documentation

- [Comparison & Prior Art](docs/COMPARISON.md)
- [Policy Schema](docs/POLICY.md)
- [CLI & Remoting Guide](docs/REMOTING.md)
- [GUI Layout Specification](docs/GUI.md)

---

## 📜 License

MIT License — Copyright (c) 2026 OpenWU Contributors
