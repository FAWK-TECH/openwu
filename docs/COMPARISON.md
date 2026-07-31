# OpenWU Comparison & Prior Art

OpenWU is designed as an open-source, trustworthy replacement for legacy Windows Update tools like WUMT (Windows Update MiniTool) and WuMgr.

## Comparison Table

| Feature / Metric | Legacy WUMT | WuMgr | Windows Settings | **OpenWU** |
|------------------|-------------|-------|------------------|------------|
| **Source Code** | Closed source (proprietary) | Open source (.NET C#) | Closed (built-in OS) | **Open source (MIT)** |
| **Dual-Mode EXE** | No (GUI only) | No (GUI focused) | No | **Yes (GUI + CLI in one EXE)** |
| **Native COM Engine** | Yes | Yes | Internal OS service | **Yes (WUA COM API)** |
| **JSON Output** | No | No | No | **Yes (`--json` flag)** |
| **Domain Controller Safeguards** | No | Basic | No | **Yes (strict safety guard & prompts)** |
| **Policy Store** | Custom registry | Limited | GPO / Registry | **Central `%ProgramData%\OpenWU\policy.json`** |
| **Framework** | Native C++ / Win32 | .NET Framework / WinForms | UWP / WinUI | **.NET 8 WinForms** |

## Design Philosophy

1. **Clean-room WUA Integration**: Direct interaction with Microsoft Windows Update Agent COM APIs without proprietary DLL dependencies.
2. **Safe Defaults**: Drivers and optional updates are excluded by default to avoid accidental driver regressions or unvetted feature updates.
3. **First-Class Automation**: CLI commands leverage the exact same C# core engine as the WinForms GUI, guaranteeing consistent behavior whether running interactively or via remote scripts.
