# OpenWU Publish Script
# Compiles OpenWU GUI (OpenWU.exe) and CLI (openwu.exe) as self-contained executables in artifacts/win-x64/

$ErrorActionPreference = "Stop"

$rootDir = Resolve-Path "$PSScriptRoot\.."
Set-Location $rootDir

Write-Host "==> Stopping running OpenWU processes..." -ForegroundColor Cyan
Get-Process OpenWU,openwu-cli,openwu -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host "==> Cleaning old artifacts..." -ForegroundColor Cyan
$outDir = Join-Path $rootDir "artifacts\win-x64"
if (Test-Path $outDir) {
    Get-ChildItem $outDir -Force | ForEach-Object {
        try {
            Remove-Item $_.FullName -Recurse -Force -ErrorAction Stop
        } catch {
            Write-Host "    Skip locked: $($_.Name)" -ForegroundColor DarkYellow
        }
    }
}
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$dotnet = "C:\Program Files\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) {
    $dotnet = "dotnet"
}

Write-Host "==> Publishing OpenWU.App (GUI, win-x64, self-contained)..." -ForegroundColor Cyan
& $dotnet publish "$rootDir\src\OpenWu.App\OpenWu.App.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o "$rootDir\artifacts\win-x64"

Write-Host "==> Publishing OpenWu.Cli (CLI, win-x64, self-contained)..." -ForegroundColor Cyan
& $dotnet publish "$rootDir\src\OpenWu.Cli\OpenWu.Cli.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o "$rootDir\artifacts\win-x64"

Write-Host "==> Build complete! Output located at:" -ForegroundColor Green
Write-Host "    $rootDir\artifacts\win-x64\OpenWU.exe      (GUI, WinExe, no console)" -ForegroundColor Yellow
Write-Host "    $rootDir\artifacts\win-x64\openwu-cli.exe  (CLI, console)" -ForegroundColor Yellow
Write-Host "Note: openwu.exe cannot be used as CLI name on Windows (collides with OpenWU.exe)." -ForegroundColor DarkYellow
