# OpenWU Code Signing Guide

This document outlines the code signing policy, SmartScreen behavior, and procedures for signing OpenWU executables.

---

## 1. SmartScreen & Unsigned Executables

When running freshly compiled binaries or unsigned releases on Windows, Defender SmartScreen may display a warning ("Windows protected your PC"). 

- **Why this happens**: SmartScreen flags executables that lack an Authenticode digital signature backed by a trusted Certificate Authority (CA) or sufficient reputation history.
- **Verification**: Users can verify build integrity by inspecting the source code, building from source using `.NET 8 SDK`, or checking the `SHA256SUMS.txt` checksums provided on official Releases.

---

## 2. Post-Build Signing Procedure

Maintainers possessing a valid Code Signing Certificate (EV or OV) can sign published executables using `signtool.exe`:

```cmd
signtool.exe sign /fd SHA256 ^
    /tr http://timestamp.digicert.com /td SHA256 ^
    /f "path\to\certificate.pfx" /p "YourCertPassword" ^
    "artifacts\win-x64\OpenWU.exe" "artifacts\win-x64\openwu-cli.exe"
```

To verify signature validity:

```cmd
signtool.exe verify /pa /v "artifacts\win-x64\OpenWU.exe"
```

---

## 3. GitHub Actions Integration (Future Cert Setup)

When a PFX certificate is added to the repository secrets, CI can be configured to automatically sign tagged releases by setting:

- **Secret Name**: `SIGNING_CERT_PFX_BASE64` (Base64 encoded PFX cert file)
- **Secret Name**: `SIGNING_CERT_PASSWORD` (PFX password)

```yaml
- name: Decode and Sign Executables
  if: env.SIGNING_CERT_PFX_BASE64 != ''
  shell: pwsh
  run: |
    $certBytes = [Convert]::FromBase64String("${{ secrets.SIGNING_CERT_PFX_BASE64 }}")
    [IO.File]::WriteAllBytes("cert.pfx", $certBytes)
    & "signtool.exe" sign /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 /f cert.pfx /p "${{ secrets.SIGNING_CERT_PASSWORD }}" artifacts/win-x64/*.exe
    Remove-Item cert.pfx
```

---

## 4. Community Build Statement

Unless explicitly stated on a release notes page, default community builds uploaded to GitHub Releases are provided **unsigned** for transparency and auditability.
