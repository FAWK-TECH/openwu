# OpenWU GUI Specification & Layout

OpenWU features a polished WinForms desktop interface designed for fast, efficient Windows Update management without complex telemetry, web view overhead, or background service bloat.

---

## Visual Design & Screenshots (v0.3.0)

See `docs/images/`:

- `gui-main.png` — Updates tab (empty state overlay / grid + detail pane)
- `gui-history.png` — History tab (formatted status badges)
- `gui-settings.png` — Settings / Policy (GroupBox layout)
- `gui-about.png` — About dialog (W monogram, repo link, log path)

---

## Layout Overview

```
+-----------------------------------------------------------------------------------+
| OpenWU — Windows Update Manager                                           [-][square][X] |
+-----------------------------------------------------------------------------------+
| Refresh (F5) | Download | INSTALL | Hide | Unhide | Select Security | Select All | About|
+-----------------------------------------------------------------------------------+
| [ ] Include drivers  [ ] Include optional  [x] Microsoft Update  [ ] Persist hides...|
+-----------------------------------------------------------------------------------+
| [Updates] [History] [Settings / Policy]                                           |
| +-------------------------------------------------------------------------------+ |
| | [Empty State Panel / Grid]                                                    | |
| | Title: No pending updates                                                     | |
| | Body: This PC is up to date for current filters, or nothing matched.          | |
| | Last checked: 2026-07-31 20:00:00  [Refresh Updates]                          | |
| +-------------------------------------------------------------------------------+ |
| | split -----------------------------------------------------------------------   |
| | Title · meta (KB · size · category · severity)                                  |
| | Description / release notes (scroll)                                            |
| | Support URL (link)                                                              |
+-----------------------------------------------------------------------------------+
| Ready. Found 0 update(s).                     [Updates: 0] [Admin: OK] [Reboot: Clean]|
+-----------------------------------------------------------------------------------+
```

---

## Key Features & Design Tokens (`UiTheme.cs`)

- **Design Palette**:
  - Headers / Dark Chrome: Slate 900 (`#0F172A`)
  - Surface Background: Pure White (`#FFFFFF`) / Page Background (`#F8FAFC`)
  - Grid Alternate Rows: Slate 50 (`#F8FAFC`)
  - Primary Accent: Sky 500 (`#0EA5E9`) / Hover (`#0284C7`)
  - Severity Badges: Critical Red (`#DC2626`), Important Orange (`#D97706`), Moderate Blue (`#2563EB`)

- **Toolbar Hierarchy**:
  - `Install` button styled as primary action button with bold accent coloring.
  - Clear group separators: `[Refresh] | [Download] [Install] | [Hide] [Unhide] | [Select...] | [History] | [About]`.

- **Empty State Overlay**:
  - Centered panel displayed when 0 updates match current search filters.
  - Shows clear feedback, last checked timestamp, and a direct `Refresh Updates` button.

- **High-DPI Support**:
  - Configured with `Application.SetHighDpiMode(HighDpiMode.PerMonitorV2)` for crisp rendering on high-resolution displays.

- **Structured Settings**:
  - Policy items organized into clear `GroupBox` panels (Update Source & Defaults, Safety Guards, Hidden KBs).

- **Rich Tooltips & Context Menu**:
  - Grid cell tooltips display complete update titles and description summaries.
  - Context menu provides one-click actions: View details, Copy KB, Copy title, Check/Uncheck row, Hide, Open support URL.
