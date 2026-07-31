# OpenWU 0.2 Plan

> **For implementers (human or AG):** Ship trust + polish that make strangers willing to run the binary. No multi-host, no tray agent, no “disable Windows Update.” Keep the product local-first.

**Version target:** `0.2.0`  
**Repo:** https://github.com/FAWK-TECH/openwu  
**Baseline:** `0.1.0` (GUI + CLI, WUA COM, policy, JSON envelopes)

---

## Goals (what 0.2 is for)

| Goal | Success looks like |
|------|-------------------|
| **Looks real** | README has screenshots; app has icon + About |
| **Trustworthy build** | Tag → CI builds EXEs; Release has SHA256 |
| **Cleaner architecture** | CLI does not reference WinForms App project |
| **Light audit trail** | Hide/install actions land in ProgramData log |
| **Signing (optional)** | Documented path; only required if cert available |

**Non-goals for 0.2:** cancel download, offline CAB, fleet remoting, dark mode, i18n, drivers-only browser, Authenticode purchase flow automation.

---

## Workstreams (do in this order)

```
A  Visual identity (icon, About, screenshots)
B  Action log
C  Split CLI library (architecture)
D  CI + Release checksums
E  Signing notes / optional sign step
F  Version bump, CHANGELOG, tag v0.2.0
```

A and B can overlap. **C before D** so CI builds the final layout. E only if a cert exists or you add a manual “signed release” checklist.

---

## A — Visual identity

### A1. Application icon

**Files:**
- Add: `src/OpenWu.App/Assets/openwu.ico` (and optional `openwu-256.png` for docs)
- Modify: `OpenWu.App.csproj` — `<ApplicationIcon>Assets\openwu.ico</ApplicationIcon>`
- Modify: `OpenWu.Cli.csproj` — same icon optional (nice for Task Manager)

**Requirements:**
- Simple, readable at 16×16 and 32×32 (Windows Update / shield / checklist motif — not clipart chaos)
- Embed in both published EXEs
- License-clean (original or CC0)

**Done when:** Taskbar / Explorer shows custom icon for `OpenWU.exe`.

### A2. About dialog

**Files:**
- Add: `src/OpenWu.App/Gui/AboutForm.cs`
- Modify: `MainForm.cs` — Help/About menu or toolbar button

**About contents (exact-ish):**
- Product name + version (`Directory.Build.props` / assembly informational version)
- “Open-source Windows Update manager”
- Link: `https://github.com/FAWK-TECH/openwu`
- MIT license one-liner
- **Disclaimer:** Administrator utility using official Windows Update Agent APIs. Not a substitute for organizational patch policy (Intune, WSUS, etc.). Hiding updates can increase risk — use as an exception, not a default.

**Done when:** User can open About from GUI without hunting README.

### A3. README screenshots

**Files:**
- Add: `docs/images/gui-main.png` (Updates tab with grid + detail pane)
- Add: `docs/images/gui-settings.png` (optional second shot)
- Modify: `README.md` — “Screenshots” section near top (after one-line pitch)

**How to capture:**
1. Build/run elevated GUI on a machine with at least one pending update if possible (empty grid OK if labeled).
2. 1280×720-ish window; no personal hostnames in title bar if avoidable (or crop).
3. PNG, reasonable size (&lt; 500 KB each if possible).

**Done when:** GitHub repo page shows UI without downloading EXE.

---

## B — Action log

### B1. Logger in Core

**Files:**
- Add: `src/OpenWu.Core/Logging/ActionLog.cs`
- Path: `%ProgramData%\OpenWU\logs\actions-YYYYMMDD.log`
- Also retain rolling: keep last 14 days optional (simple: one file per day, no delete in 0.2 unless easy)

**Line format (one line per event, no secrets):**

```text
2026-07-31T18:04:00Z host=DESKTOP01 user=DOMAIN\bob elevated=true action=install ok=true kbs=KB5034441,KB5034123 rebootRequired=false message=Succeeded
2026-07-31T18:10:00Z host=DESKTOP01 user=DOMAIN\bob elevated=true action=hide ok=true kbs=KB1234567 persist=true
```

**Log on:**
- `install` (including what-if? **No** — only real install attempts)
- `hide` / `unhide` (show)
- optional: `policy set` / `policy reset` (nice-to-have)

**API:**

```csharp
public static class ActionLog
{
    public static void Write(string action, bool ok, IEnumerable<string>? kbs = null, string? message = null, IReadOnlyDictionary<string, string>? extra = null);
}
```

Call from `UpdateService` after hide/install/unhide complete (success or fail).

**Done when:** Performing hide/install appends lines; log path documented in `docs/POLICY.md` or new `docs/LOGGING.md` (short section in POLICY is enough).

### B2. GUI status affordance

- Status strip or About: “Action log: %ProgramData%\OpenWU\logs\”
- No log viewer UI in 0.2 (open folder is enough)

---

## C — Split CLI off WinForms App

### Problem (0.1)

`OpenWu.Cli` → references `OpenWu.App` → pulls WinForms into CLI single-file (~76 MB, wrong dependency direction).

### Target layout

```
OpenWu.Core          WUA, policy, guards, ActionLog
OpenWu.CliLib        CliHost + JsonEnvelope (class library, no WinForms)
OpenWu.App           WinExe GUI; optional AttachConsole → CliLib
OpenWu.Cli           Exe → CliLib only
OpenWu.Core.Tests    (+ optional CliLib tests for envelope shape)
```

### Steps

1. Add `src/OpenWu.CliLib/OpenWu.CliLib.csproj` (`net8.0-windows` only if needed for WMI; prefer `net8.0` if Core allows — Core is `net8.0-windows` today, keep consistent).
2. Move `OpenWu.App/Cli/*` → `OpenWu.CliLib/`.
3. `OpenWu.Cli` references **CliLib + Core only** (not App).
4. `OpenWu.App` references **CliLib** for dual-mode args path.
5. Update `OpenWu.sln`, `scripts/publish.ps1`, README architecture blurb.
6. Verify:
   - `openwu-cli.exe list --json` envelope unchanged
   - `OpenWU.exe` no-args → GUI
   - CLI publish size should drop or stay flat without WinForms if trim works; document actual size

**Done when:** `OpenWu.Cli.csproj` has no `ProjectReference` to `OpenWu.App`.

---

## D — CI + Release checksums

### D1. GitHub Actions workflow

**File:** `.github/workflows/build.yml`

**Triggers:**
- `push` / `pull_request` to `main` → restore, build, test
- `push` tags `v*` → build, test, publish both EXEs, upload artifacts, create/update Release assets + checksums

**Job sketch (windows-latest):**

```yaml
# pseudo — implement real YAML
- uses: actions/setup-dotnet@v4
  with: { dotnet-version: '8.0.x' }
- run: dotnet test OpenWu.sln -c Release
- run: ./scripts/publish.ps1   # or inline publish commands
- run: |
    cd artifacts/win-x64
    Get-FileHash OpenWU.exe,openwu-cli.exe -Algorithm SHA256 |
      ForEach-Object { "$($_.Hash)  $($_.Hash.Path | Split-Path -Leaf)" } |
      Set-Content SHA256SUMS.txt
# on tag: softprops/action-gh-release or gh release upload
```

**Constraints:**
- No secrets required for unsigned build
- Do not commit `artifacts/` to git
- Use `FAWK-TECH/openwu` permissions: `contents: write` on tag job for release upload

### D2. Checksums on Release

Every Release **must** include:

| Asset | Purpose |
|-------|---------|
| `OpenWU.exe` | GUI |
| `openwu-cli.exe` | CLI |
| `SHA256SUMS.txt` | Hashes for both |

README “Download” section: point to Releases + “verify with `Get-FileHash`”.

**Done when:** Pushing `v0.2.0` tag produces a Release with three assets without a human running publish locally (local publish still OK as fallback).

---

## E — Code signing (optional / documented)

### If no cert in 0.2

**File:** `docs/SIGNING.md`

Contents:
- Why SmartScreen warns
- How maintainers can sign post-build (`signtool sign /fd SHA256 ...`)
- Where to put cert secrets in GitHub Actions **later** (`SIGNING_CERT_PFX_BASE64`, password) — do not implement until cert exists
- “Community builds are unsigned unless stated on the Release”

### If cert available

- Add optional job step after publish
- Only sign Release assets, not every PR build
- Note thumbprint / timestamp server (e.g. DigiCert/Sectigo free timestamp URL)

**Done when:** Either SIGNING.md exists **or** Release assets are actually signed (signtool verify).

---

## F — Ship checklist (v0.2.0)

- [ ] `Directory.Build.props` → `0.2.0`
- [ ] `CHANGELOG.md` with 0.2.0 section (user-facing bullets)
- [ ] README: screenshots, download/verify, log path one-liner
- [ ] `dotnet test` green
- [ ] Local `publish.ps1` produces both EXEs + optional local SHA256
- [ ] Tag `v0.2.0` → CI green → Release complete
- [ ] Spot-check GUI: About, icon, hide once, confirm log line
- [ ] Spot-check CLI: `openwu-cli.exe test --json` envelope + version `0.2.0`

### CHANGELOG skeleton

```markdown
## 0.2.0

### Added
- Application icon and About dialog
- README screenshots
- Action log under %ProgramData%\OpenWU\logs\
- OpenWu.CliLib (CLI no longer depends on WinForms App)
- CI build/test; tagged releases publish EXEs + SHA256SUMS.txt
- docs/SIGNING.md

### Changed
- Version 0.2.0
```

---

## Effort guide

| Workstream | Rough effort |
|------------|----------------|
| A Visual | 2–4 h (icon + About + shots) |
| B Action log | 1–2 h |
| C CliLib split | 2–4 h |
| D CI + checksums | 2–3 h |
| E Signing docs | 30 min (or more if real cert) |
| F Ship | 1 h |

**Total:** about **1–2 focused days**, not a multi-week epic.

---

## AG / implementer handoff blurb

```text
Implement docs/ROADMAP-0.2.md for OpenWU 0.2.0 in https://github.com/FAWK-TECH/openwu
Order: A (icon/About/screenshots) → B (action log) → C (CliLib split) → D (CI+SHA256) → E (SIGNING.md) → F (version/changelog/tag).
Do not add multi-host, tray apps, or disable-Windows-Update features.
Keep JSON CLI envelope stable (ok, host, version, verb, ...).
CLI must not reference OpenWu.App when done.
Stop for human review before tagging v0.2.0 if CI is new.
```

---

## Out of scope reminders (push to 0.3+ if ever)

- Cancel in-flight WUA ops  
- Offline MSU/CAB  
- CSV export from GUI  
- Event Log provider  
- Dark mode / i18n  
- Multi-PC remoting  

### Remember later (do not do in 0.2)

**winget distribution (post-0.2):** After 0.2 ships (CI releases + SHA256), consider publishing **OpenWU itself** to winget (`winget install …`) so people can install/update the tool easily. Point the manifest at GitHub Release assets.

**Not in scope even then (unless product pivot):** folding **winget package management** into OpenWU’s GUI/CLI (app upgrades via winget). OpenWU stays **Windows Update Agent only**. winget = how users get OpenWU, not a second update engine inside the app.

Track as: **0.3 packaging / distribution**, not a 0.2 workstream.

---

## Definition of Done (0.2)

A stranger can: open the GitHub page, **see the UI**, download **checksummed** EXEs from a **CI-built** Release, run GUI with a **proper icon**, read **About/disclaimer**, hide or install something, and find an **action log line** on disk — and maintainers can build CLI **without** linking WinForms App.
