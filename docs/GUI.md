# OpenWU GUI Specification & Layout

OpenWU features a classic WinForms desktop interface designed for fast, efficient Windows Update management without complex telemetry or web view overhead.

## Layout Overview

```
+-----------------------------------------------------------------------------------+
| OpenWU — Windows Update                                                  [-][square][X] |
+-----------------------------------------------------------------------------------+
| Refresh (F5) | Download | Install | Hide | Unhide | Select Security | Select None | History |
+-----------------------------------------------------------------------------------+
| [ ] Include drivers  [ ] Include optional  [x] Microsoft Update  ...              |
+-----------------------------------------------------------------------------------+
| [Updates] [History] [Settings / Policy]                                           |
| +----+-----------+------------------------------------+----+----------+-----------+
| |    | KB        | Title                              | MB | Severity | Status    |  dense rows,
| | [x]| KB5031234 | 2026-07 Security Update...         |650 | Critical | Pending   |  alt stripes,
| | [ ]| KB5039999 | Windows Driver Update              | 12 | Moderate | Pending   |  severity color
| +----+-----------+------------------------------------+----+----------+-----------+
| | split -----------------------------------------------------------------------   |
| | Title · meta (KB · size · category · severity)                                  |
| | Description / release notes (scroll)                                            |
| | Support URL (link)                                                              |
+-----------------------------------------------------------------------------------+
| Ready. Found 2 update(s).                     [Updates: 2] [Admin: OK] [Reboot: Clean]|
+-----------------------------------------------------------------------------------+
```

## Key Interactions

- **Toolbar Buttons**:
  - `Refresh (F5)`: Asynchronously queries pending updates using WUA COM on a background thread.
  - `Download`: Downloads checked updates with progress tracking.
  - `Install`: Executes installation for selected updates.
  - `Hide / Unhide`: Toggles update visibility in Windows Update database.
  - `Select Security`: Checks all Security and Critical/Important non-driver updates with a single click.

- **Dense grid**: 22px rows, alternating row colors, bold severity text (Critical red / Important orange).
- **Selection detail pane** (bottom split): live title, meta line, description, support link for the selected row (WUMT-style).
- **Context menu** (right-click): View details, Copy KB, Copy title, Check/Uncheck row, Hide, Open support URL.
- **Double-Click Row**: Still opens `UpdateDetailsForm` for a full details window.
- **Ctrl+C** on grid: copies the selected row KB.
