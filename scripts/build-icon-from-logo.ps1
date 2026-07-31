# Build multi-size openwu.ico + docs/images/logo.png from a source logo image.
# Usage: pwsh -File scripts/build-icon-from-logo.ps1 [-Source path]

param(
    [string]$Source = "E:\projects\LOGO\openwu-candidates\openwu-W-variant-A.jpg"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$root = Split-Path (Split-Path $PSScriptRoot -Parent) -ErrorAction SilentlyContinue
if (-not $root) { $root = "C:\source\openwu" }
# PSScriptRoot is scripts/ -> parent is openwu
$root = Resolve-Path (Join-Path $PSScriptRoot "..")

if (-not (Test-Path $Source)) {
    $alt = Join-Path $root "src\OpenWu.App\Assets\openwu-logo-source.jpg"
    if (Test-Path $alt) { $Source = $alt }
    else { throw "Source not found: $Source" }
}

$assets = Join-Path $root "src\OpenWu.App\Assets"
$docs = Join-Path $root "docs\images"
New-Item -ItemType Directory -Force -Path $assets, $docs | Out-Null

Copy-Item $Source (Join-Path $assets "openwu-logo-source.jpg") -Force

function New-SquareBitmap([System.Drawing.Image]$img, [int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $g.Clear([System.Drawing.Color]::FromArgb(255, 28, 28, 30))
    $side = [Math]::Min($img.Width, $img.Height)
    $sx = [int](($img.Width - $side) / 2)
    $sy = [int](($img.Height - $side) / 2)
    $dest = New-Object System.Drawing.Rectangle 0, 0, $size, $size
    $srcR = New-Object System.Drawing.Rectangle $sx, $sy, $side, $side
    $g.DrawImage($img, $dest, $srcR, [System.Drawing.GraphicsUnit]::Pixel)
    $g.Dispose()
    return $bmp
}

function Get-PngBytes([System.Drawing.Bitmap]$bmp) {
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    return $ms.ToArray()
}

# PNG-in-ICO for all sizes is widely supported on modern Windows
$sizes = @(16, 32, 48, 64, 128, 256)
$srcImg = [System.Drawing.Image]::FromFile((Resolve-Path $Source).Path)

$png512 = New-SquareBitmap $srcImg 512
$logoPng = Join-Path $docs "logo.png"
$png512.Save($logoPng, [System.Drawing.Imaging.ImageFormat]::Png)
$png512.Dispose()

$entries = New-Object System.Collections.Generic.List[object]
foreach ($s in $sizes) {
    $b = New-SquareBitmap $srcImg $s
    $data = Get-PngBytes $b
    $b.Dispose()
    $entries.Add([pscustomobject]@{ Size = $s; Data = $data })
}
$srcImg.Dispose()

$icoPath = Join-Path $assets "openwu.ico"
$fs = [System.IO.File]::Create($icoPath)
$bw = New-Object System.IO.BinaryWriter $fs
$count = $entries.Count
$bw.Write([int16]0)
$bw.Write([int16]1)
$bw.Write([int16]$count)

$offset = 6 + (16 * $count)
foreach ($e in $entries) {
    $s = $e.Size
    $len = $e.Data.Length
    # 0 means 256 in classic ICO width/height bytes
    $dim = 0
    if ($s -lt 256) { $dim = [byte]$s }
    $bw.Write([byte]$dim)
    $bw.Write([byte]$dim)
    $bw.Write([byte]0)
    $bw.Write([byte]0)
    $bw.Write([int16]1)
    $bw.Write([int16]32)
    $bw.Write([int32]$len)
    $bw.Write([int32]$offset)
    $offset += $len
}
foreach ($e in $entries) {
    $bw.Write($e.Data)
}
$bw.Flush()
$bw.Close()
$fs.Close()

Write-Host "OK: $icoPath ($((Get-Item $icoPath).Length) bytes)"
Write-Host "OK: $logoPng ($((Get-Item $logoPng).Length) bytes)"
