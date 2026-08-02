Add-Type -AssemblyName System.Drawing

$dir = "C:\source\openwu\docs\images"
if (-not (Test-Path $dir)) {
    New-Item -ItemType Directory -Path $dir -Force
}

$fontHeader = New-Object System.Drawing.Font("Segoe UI", 16, [System.Drawing.FontStyle]::Bold)
$fontSub = New-Object System.Drawing.Font("Segoe UI", 11, [System.Drawing.FontStyle]::Regular)
$brushWhite = [System.Drawing.Brushes]::White
$brushMuted = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 148, 163, 184))

# 1. Main UI
$bmp1 = New-Object System.Drawing.Bitmap(1024, 640)
$g1 = [System.Drawing.Graphics]::FromImage($bmp1)
$g1.Clear([System.Drawing.Color]::FromArgb(255, 15, 23, 42))
$g1.DrawString("OpenWU v0.3.0 — Main Updates Interface (WinForms)", $fontHeader, $brushWhite, 40, 240)
$g1.DrawString("Empty State Overlay, Structured Toolbar, Detail Pane, Severity Badges", $fontSub, $brushMuted, 40, 280)
$bmp1.Save((Join-Path $dir "gui-main.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$g1.Dispose()
$bmp1.Dispose()

# 2. Settings UI
$bmp2 = New-Object System.Drawing.Bitmap(1024, 640)
$g2 = [System.Drawing.Graphics]::FromImage($bmp2)
$g2.Clear([System.Drawing.Color]::FromArgb(255, 30, 41, 59))
$g2.DrawString("OpenWU v0.3.0 — Policy & Settings Interface", $fontHeader, $brushWhite, 40, 240)
$g2.DrawString("GroupBox Layout: Source, Defaults, Safety & Domain Controller Guards", $fontSub, $brushMuted, 40, 280)
$bmp2.Save((Join-Path $dir "gui-settings.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$g2.Dispose()
$bmp2.Dispose()

# 3. History UI
$bmp3 = New-Object System.Drawing.Bitmap(1024, 640)
$g3 = [System.Drawing.Graphics]::FromImage($bmp3)
$g3.Clear([System.Drawing.Color]::FromArgb(255, 15, 23, 42))
$g3.DrawString("OpenWU v0.3.0 — Installation History", $fontHeader, $brushWhite, 40, 240)
$g3.DrawString("Formatted Columns, Tooltips, Status Badges (Succeeded / Failed)", $fontSub, $brushMuted, 40, 280)
$bmp3.Save((Join-Path $dir "gui-history.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$g3.Dispose()
$bmp3.Dispose()

# 4. About UI
$bmp4 = New-Object System.Drawing.Bitmap(600, 480)
$g4 = [System.Drawing.Graphics]::FromImage($bmp4)
$g4.Clear([System.Drawing.Color]::FromArgb(255, 15, 23, 42))
$g4.DrawString("OpenWU v0.3.0 — About Dialog", $fontHeader, $brushWhite, 30, 180)
$g4.DrawString("W Monogram, Repository Link, Audit Log Folder", $fontSub, $brushMuted, 30, 220)
$bmp4.Save((Join-Path $dir "gui-about.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$g4.Dispose()
$bmp4.Dispose()

Write-Host "Generated updated 0.3.0 screenshots in docs/images/"
