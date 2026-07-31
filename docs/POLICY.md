# OpenWU Policy Specification

Path: `%ProgramData%\OpenWU\policy.json`

The policy file controls default update behavior across both the **GUI** and **CLI** modes.

## Schema Definition

```json
{
  "schemaVersion": 1,
  "service": "MicrosoftUpdate",
  "includeDrivers": false,
  "includeOptional": false,
  "autoSelect": "SecurityOnly",
  "reboot": "Never",
  "hiddenKBs": [],
  "denyTitlesContains": [
    "Preview"
  ],
  "allowOnDomainController": false,
  "maxInstallBatch": 50,
  "searchTimeoutSec": 300
}
```

## Field Descriptions

- **`schemaVersion`**: Integer version for policy migration compatibility (default `1`).
- **`service`**: Update catalog provider. Options: `"MicrosoftUpdate"` or `"WindowsUpdate"`.
- **`includeDrivers`**: Boolean flag indicating whether driver updates should be queried by default.
- **`includeOptional`**: Boolean flag indicating whether optional software updates should be included.
- **`autoSelect`**: Strategy for GUI "Select Security" button.
- **`reboot`**: Default reboot action following installation (`"Never"`, `"IfRequired"`, `"Always"`).
- **`hiddenKBs`**: Array of KB identifiers (e.g., `["KB5031234"]`) automatically hidden from search results.
- **`denyTitlesContains`**: Array of title substrings that trigger safety soft-denial during installation.
- **`allowOnDomainController`**: Guard flag blocking execution on Active Directory Domain Controllers unless explicitly enabled.
