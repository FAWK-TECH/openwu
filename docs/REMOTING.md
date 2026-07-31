# OpenWU CLI & Automation Guide

OpenWU provides full CLI parity with `--json` formatted outputs for integration with RMM tools, PowerShell remoting, Ansible, or custom orchestration scripts.

## Common CLI Commands

### 1. Health & Environment Test
```cmd
openwu-cli.exe test --json
```

**Sample Output:**
```json
{
  "ok": true,
  "host": "WORKSTATION01",
  "version": "0.1.0",
  "verb": "test",
  "message": "WUA COM Session initialized successfully.",
  "health": {
    "isElevated": true,
    "isDomainController": false,
    "wuaServiceRunning": true,
    "wuaVersion": "1.0 (COM)",
    "canSearch": true,
    "statusMessage": "WUA COM Session initialized successfully."
  }
}
```

### 2. List Pending Updates
```cmd
openwu-cli.exe list --json --include-drivers
```

**Sample Output (always an object envelope — never a bare array):**
```json
{
  "ok": true,
  "host": "WORKSTATION01",
  "version": "0.1.0",
  "verb": "list",
  "count": 1,
  "updates": [
    {
      "kb": "KB5031234",
      "title": "2026-07 Cumulative Update for Windows 11 Version 23H2 for x64-based Systems",
      "sizeMB": 650.4,
      "categories": "Security Updates",
      "severity": "Critical",
      "isDownloaded": false,
      "isHidden": false,
      "isDriver": false,
      "rebootRequired": true,
      "identity": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "revision": 200
    }
  ]
}
```

Common envelope fields on every `--json` response: `ok`, `host`, `version`, `verb`, optional `message`.

### 3. WhatIf Simulation
```cmd
openwu-cli.exe install --security-only --whatif --json
```

### 4. Quiet Security Update Installation
```cmd
openwu-cli.exe install --security-only --reboot --json
```

### 5. Managing Hidden KBs
```cmd
openwu-cli.exe hide --kb KB5031234 --persist
openwu-cli.exe show --kb KB5031234
```
