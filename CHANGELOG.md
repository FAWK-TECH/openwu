# Change Log

All notable changes to OpenWU will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [0.2.0] - 2026-07-31

### Added
- Custom application icon embedded in `OpenWU.exe` executable.
- About dialog (`AboutForm`) with version, license details, links, and disclaimers.
- Structured action logging in `%ProgramData%\OpenWU\logs\actions-YYYYMMDD.log`.
- `OpenWu.CliLib` class library decoupling CLI host execution from WinForms GUI (`OpenWu.App`).
- GitHub Actions CI/CD workflow (`.github/workflows/build.yml`) for automated build, test, and checksum generation (`SHA256SUMS.txt`) on tag releases.
- Code signing documentation (`docs/SIGNING.md`).
- Visual layout screenshots in `docs/images/`.

### Changed
- Centralized solution versioning set to `0.2.0` in `Directory.Build.props`.
- Updated documentation and CLI help strings to version `0.2.0`.
- Updated `publish.ps1` to stop running OpenWU processes prior to cleaning artifacts.

---

## [0.1.0] - 2026-07-31

### Added
- Initial OpenWU release.
- Native WUA COM integration for scanning, downloading, installing, and hiding Windows Updates.
- WinForms GUI application (`OpenWU.exe`) with multi-tab layout (Updates, History, Settings).
- Headless CLI host with JSON output envelope (`--json`).
- Central policy configuration (`%ProgramData%\OpenWU\policy.json`).
- Safety guards for Domain Controllers and title denial keywords.
