Add-Type -AssemblyName System.Drawing

$dir = "C:\source\openwu\src\OpenWu.App\Assets"
if (-not (Test-Path $dir)) {
    New-Item -ItemType Directory -Path $dir -Force
}

$targetPath = Join-Path $dir "openwu.ico"

$bmp = New-Object System.Drawing.Bitmap(32, 32)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

$bgBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 15, 23, 42))
$g.FillEllipse($bgBrush, 2, 2, 28, 28)

$pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 56, 189, 248), 3)
$pt1 = New-Object System.Drawing.Point(9, 16)
$pt2 = New-Object System.Drawing.Point(14, 21)
$pt3 = New-Object System.Drawing.Point(23, 11)
$pts = [System.Drawing.Point[]]@($pt1, $pt2, $pt3)
$g.DrawLines($pen, $pts)

$hIcon = $bmp.GetHicon()
$icon = [System.Drawing.Icon]::FromHandle($hIcon)
$fs = [System.IO.File]::Create($targetPath)
$icon.Save($fs)
$fs.Close()

$g.Dispose()
$bmp.Dispose()
Write-Host "Successfully generated openwu.ico at $targetPath"
