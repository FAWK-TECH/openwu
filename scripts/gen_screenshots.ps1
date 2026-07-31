Add-Type -AssemblyName System.Drawing

$dir = "C:\source\openwu\docs\images"
if (-not (Test-Path $dir)) {
    New-Item -ItemType Directory -Path $dir -Force
}

$bmp1 = New-Object System.Drawing.Bitmap(800, 480)
$g1 = [System.Drawing.Graphics]::FromImage($bmp1)
$g1.Clear([System.Drawing.Color]::FromArgb(255, 15, 23, 42))
$font = New-Object System.Drawing.Font("Segoe UI", 16, [System.Drawing.FontStyle]::Bold)
$brush = [System.Drawing.Brushes]::White
$g1.DrawString("OpenWU v0.2.0 — Main Updates Interface", $font, $brush, 50, 220)
$bmp1.Save((Join-Path $dir "gui-main.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$g1.Dispose()
$bmp1.Dispose()

$bmp2 = New-Object System.Drawing.Bitmap(800, 480)
$g2 = [System.Drawing.Graphics]::FromImage($bmp2)
$g2.Clear([System.Drawing.Color]::FromArgb(255, 30, 41, 59))
$g2.DrawString("OpenWU v0.2.0 — Settings & Policy Interface", $font, $brush, 50, 220)
$bmp2.Save((Join-Path $dir "gui-settings.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$g2.Dispose()
$bmp2.Dispose()

Write-Host "Generated screenshot placeholders in docs/images/"
