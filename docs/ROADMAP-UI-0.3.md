# OpenWU UI Upgrade Plan (0.3)

> **For AG / implementers:** Polish the **WinForms GUI** so OpenWU looks owned and intentional. Stay on **WinForms** (Path A). Do **not** rewrite in WPF/WinUI. Do **not** add Fawk branding. Product mark is the existing **W monogram** (`Assets/openwu.ico`, `openwu-mark.png`).

**Version target:** `0.3.0`  
**Repo:** https://github.com/FAWK-TECH/openwu  
**Baseline:** `0.2.1` (W brand, CliLib, action log, CI releases)  
**Local:** `C:\source\openwu` when present  

---

## Mission

Make the GUI feel like a **credible admin utility** (Sysinternals-adjacent), not a default-control prototype:

1. Clear visual hierarchy (primary Install action, grouped chrome)
2. Professional empty/error states
3. Readable Settings and About (no wall-of-text)
4. History/Updates grids that don’t truncate into nonsense
5. Consistent **design tokens** (slate/charcoal + neutral surfaces) aligned with the W mark
6. Optional light toolbar icons (not emoji; simple glyphs or Segoe MDL2)

**Non-goals:** dark mode theme engine, Fluent/WinUI migration, multi-window shell, animation system, Fawk logo/wordmark, winget UI.

---

## Brand / design constraints

| Rule | Detail |
|------|--------|
| Product name | **OpenWU** only in UI chrome |
| Mark | Existing W monogram — use for Form.Icon (already), About, optional toolbar header |
| No Fawk | No F monogram, no “Fawk” / “FAWK” in window titles or About body (repo URL `github.com/FAWK-TECH/openwu` is OK as repository link) |
| Palette (lock) | Charcoal header/surfaces `#1C1C1E`–`#0F172A`; page bg `#F8FAFC`; borders `#E2E8F0`; text primary `#0F172A`; muted `#64748B`; accent (links/focus) soft cyan/slate `#0EA5E9` or keep severity reds/oranges as today |
| Typography | Segoe UI throughout; 9pt body, 8.5–9pt grid, 11–12pt section headers |
| Spacing | 8px grid: 8/12/16 padding; toolbar height ~36–40px |

Extract colors into a single static class so MainForm/History/Settings/About stay consistent:

```
src/OpenWu.App/Gui/UiTheme.cs
  Colors, Fonts helpers, ApplyGridStyle(DataGridView), ApplyToolStrip(ToolStrip)
```

---

## Current pain (from 0.2 screenshots / code)

| Area | Problem |
|------|---------|
| Toolbar | All-text, flat, Install not visually primary |
| Updates empty | Huge white void, weak empty-state copy only in detail pane |
| Detail pane | OK idea; needs typography + separator |
| History | Columns clip (`K…`, `N…`); weak density |
| Settings | Loose labels (“Update”, “Default”); not grouped |
| About | Improved in 0.2.1 but keep structured (already better) |
| Status strip | Fine; tighten colors to theme |
| No visual idle feedback | Busy state exists; idle empty needs illustration-free friendly panel |

---

## Workstreams (order)

```
0  UiTheme + Form shell polish
1  Toolbar hierarchy (+ optional glyphs)
2  Updates tab: empty state, grid, detail pane
3  History tab: columns, tooltips, density
4  Settings tab: group boxes, labels, buttons
5  About polish pass (if needed)
6  High-DPI / resize sanity
7  Screenshots + CHANGELOG + version 0.3.0
```

Do **0 → 2** before screenshots. Ship when 0–6 are done.

---

### Workstream 0 — Design system shell

**Files:**
- Create: `src/OpenWu.App/Gui/UiTheme.cs`
- Modify: `MainForm.cs` constructor / `BuildUi` to apply theme
- Modify: `HistoryControl.cs`, `SettingsControl.cs`, `AboutForm.cs` to use theme colors

**Requirements:**
- Centralize colors listed above
- `MainForm`: set `BackColor`, font, minimum size ≥ 960×600, `StartPosition` center screen
- TabControl: slightly taller items if easy; consistent padding on tab pages
- StatusStrip: themed borders/text (Admin OK green, reboot red stay semantic)

**Done when:** One place defines colors; no magic ARGB scatter for chrome (severity colors may stay near grid).

---

### Workstream 1 — Toolbar

**Files:** `MainForm.cs` toolbar construction

**Requirements:**
- Keep actions: Refresh, Download, **Install**, Hide, Unhide, Select Security, Select All, Select None, History, About
- Visual hierarchy:
  - **Install** = primary (bold + accent BackColor if ToolStrip supports; or use a Panel with a real `Button` for Install only — prefer keeping ToolStrip but style Install differently)
  - Destructive-ish **Hide** not red unless you add confirm (keep confirm if multi-hide)
  - Separators between logical groups: [Refresh] | [Download Install] | [Hide Unhide] | [Select…] | [History About]
- Optional: Segoe MDL2 Assets glyphs via `ToolStripButton.DisplayStyle = ImageAndText` if you can load system font icons simply; if glyph loading is fragile, **text-only with better spacing is OK for 0.3**
- Disable toolbar buttons while busy (already partial via `SetBusy`) — ensure checkboxes on options row also disable when busy
- Keyboard: keep F5, Ctrl+A (check all)

**Done when:** Install is obviously the main action; groups read left-to-right.

---

### Workstream 2 — Updates tab

**Files:** `MainForm.cs` grid + detail + empty overlay

**Requirements:**

#### 2a. Empty state
When search returns 0 rows (success):
- Show a centered empty panel **over the grid** (not only detail text):
  - Title: “No pending updates”
  - Body: “This PC is up to date for the current filters, or nothing matched.”
  - Secondary: “Last checked: {local time}” (store timestamp on successful refresh)
  - Button: **Refresh**
- Hide empty panel when rows > 0

#### 2b. Grid
- Keep density (~22–24px rows), alt rows, severity coloring
- Tooltips on Title cell = full title (and description first line if short)
- Ensure checkbox column stable width
- Auto-size Title as fill; KB min width ≥ 90 so “KB########” fits
- Status column values: Pending / Downloaded / Hidden — consistent casing

#### 2c. Detail pane
- Top border or splitter visual clarity
- Title: semibold 10pt, meta: muted 8.5pt
- Description: readable; placeholder when none
- Support URL: link label (already) — ensure visible when present
- When empty selection + rows exist: “Select an update to see details”
- When empty list: hide detail content or show short “Nothing to show”

#### 2d. Context menu
Keep existing items; ensure enabled states match selection; optional **Copy KB** already there

**Done when:** Empty PC and full list both look intentional; no pure white void.

---

### Workstream 3 — History tab

**Files:** `HistoryControl.cs`

**Requirements:**
- Columns: Date (sortable), KB (min width 100), Title (fill), Result
- Full title in tooltip
- Result text color: Succeeded = green-ish muted, Failed = red, else default
- “Refresh History” button aligned with theme
- Empty history: “No history entries returned”
- Load on tab select (already) without freezes (async already preferred)

**Done when:** No `K…` / garbage truncation as the primary UX; tooltips always have full text.

---

### Workstream 4 — Settings / Policy

**Files:** `SettingsControl.cs`

**Requirements:**
- Use **GroupBox** or labeled panels:
  1. **Update source** — Service dropdown (Microsoft Update / Windows Update)
  2. **Install defaults** — reboot policy, include drivers/optional defaults
  3. **Safety** — Allow DC install (red hint stays), deny titles list
  4. **Persisted hidden KBs** — multiline list
- Explicit labels that match fields (not “Update” / “Default” alone)
- Buttons: **Save Policy** (primary), **Reset to Defaults** (secondary) — bottom-right, consistent padding
- Header note stays: policy path `%ProgramData%\OpenWU\policy.json` applies to GUI + CLI
- Validate booleans on save; show status label “Saved.” / error MessageBox

**Done when:** A new user can edit policy without guessing label meanings.

---

### Workstream 5 — About

**Files:** `AboutForm.cs` (0.2.1 already improved)

**Light pass only:**
- Ensure mark loads (`Assets/openwu-mark.png`)
- Structured sections if still one blob: About / License / Disclaimer / Log path
- LinkLabel for GitHub URL (clickable)
- No Fawk wordmark

**Done when:** About is skimmable in 5 seconds.

---

### Workstream 6 — High-DPI & resize

**Requirements:**
- `MainForm` / app: consider `Application.SetHighDpiMode(HighDpiMode.PerMonitorV2)` in `Program.cs` if not already
- Splitter min sizes so detail pane never collapses to 0
- Settings controls anchor/dock so resize doesn’t clip Save buttons
- Test at 100% and 150% scaling if possible (document if only 100% verified)

---

### Workstream 7 — Ship

**Requirements:**
- Bump `Directory.Build.props` → **0.3.0**
- `CHANGELOG.md` section for 0.3.0 (user-facing UI bullets)
- Capture **new screenshots** replacing `docs/images/gui-*.png` (main with empty state **and** optional with rows if available; history; settings; about)
- Update `docs/GUI.md` if layout changed
- `dotnet test` green
- Local GUI smoke: Refresh, empty state, About mark, Settings save, History load
- Do **not** break CLI or JSON envelopes
- Tag `v0.3.0` only after human OK **or** if instructed to ship (prefer human for screenshot quality)

---

## File map (expected touch list)

```
src/OpenWu.App/
  Program.cs                    # DPI if needed
  Gui/
    UiTheme.cs                  # NEW
    MainForm.cs                 # toolbar, empty state, detail, grid
    HistoryControl.cs
    SettingsControl.cs
    AboutForm.cs                # light pass
docs/
  ROADMAP-UI-0.3.md             # this file
  GUI.md                        # update
  images/gui-*.png              # refresh screenshots
CHANGELOG.md
Directory.Build.props           # 0.3.0
```

Prefer **no new NuGet** UI libraries. Stay BCL + WinForms.

---

## Testing checklist (AG final report)

- [ ] `dotnet test` passes  
- [ ] GUI launches elevated; **W** icon on window  
- [ ] Empty updates: empty-state panel visible, Refresh works  
- [ ] With updates (if any): select row → detail fills; severity colors still work  
- [ ] Context menu: copy KB, details, hide  
- [ ] History: columns readable; tooltips full title  
- [ ] Settings: save/load/reset; labels clear  
- [ ] About: mark + links + disclaimer  
- [ ] Busy: toolbar disabled during search/install  
- [ ] CLI unchanged: `openwu-cli.exe test --json` still envelope  
- [ ] No Fawk branding in UI strings  
- [ ] Screenshots updated in docs/images  

---

## Out of scope (reject)

- WPF / WinUI / MAUI rewrite  
- Dark mode as full theme  
- Custom owner-draw everything  
- Animated splash  
- Integrating winget UI  
- Changing Core WUA behavior except if a UI bug requires a tiny surface  

---

## Effort guide

| Stream | Rough |
|--------|--------|
| 0 Theme | 1–2 h |
| 1 Toolbar | 1–2 h |
| 2 Updates/empty/detail | 2–4 h |
| 3 History | 1–2 h |
| 4 Settings | 2–3 h |
| 5 About | 30–60 m |
| 6 DPI | 30–60 m |
| 7 Ship/screenshots | 1–2 h |

**Total:** ~1–2 focused days for a strong agent.

---

## AG handoff blurb (paste this)

```text
Implement OpenWU UI upgrade per docs/ROADMAP-UI-0.3.md
Repo: https://github.com/FAWK-TECH/openwu (local C:\source\openwu if present)
Baseline: 0.2.1 — target version 0.3.0

Path A only: polish WinForms. NO WPF/WinUI rewrite.
Brand: existing W monogram only. NO Fawk F logo or Fawk wordmark in UI.
Extract UiTheme.cs; upgrade toolbar hierarchy, Updates empty state + detail pane,
History columns/tooltips, Settings group layout, light About pass, DPI sanity.
Keep Core/CLI/JSON envelopes behavior unchanged.
Do not expand into winget, multi-host, or cancel-download features.
When UI done: new screenshots in docs/images, CHANGELOG, bump 0.3.0.
Stop before git tag v0.3.0 unless human says ship — report verification checklist.
```

---

## Human review gates

1. After workstream 2 (Updates empty state + toolbar) — optional visual check  
2. Before tag **v0.3.0** — screenshots + wrist check on real machine  

---

## Definition of Done (0.3)

A stranger opens `OpenWU.exe` and immediately understands: scan → select → install; empty state is calm; settings are readable; the app feels branded as **OpenWU** (W mark) without looking like a default Form1.
