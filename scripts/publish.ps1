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

$appIco = Join-Path $rootDir "src\OpenWu.App\Assets\openwu.ico"
$cliIco = Join-Path $rootDir "src\OpenWu.Cli\Assets\openwu.ico"
if (-not (Test-Path $appIco)) { throw "Missing ApplicationIcon: $appIco" }
if (-not (Test-Path $cliIco)) {
    Write-Host "==> Syncing CLI icon from App Assets..." -ForegroundColor Cyan
    New-Item -ItemType Directory -Force -Path (Split-Path $cliIco) | Out-Null
    Copy-Item $appIco $cliIco -Force
}

Write-Host "==> Publishing OpenWU.App (GUI, win-x64, self-contained)..." -ForegroundColor Cyan
# Do NOT pass -p:ApplicationIcon on the command line — MSBuild applies it to all
# projects in the graph (including OpenWu.Core) and breaks the build.
& $dotnet publish "$rootDir\src\OpenWu.App\OpenWu.App.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o "$rootDir\artifacts\win-x64"
if ($LASTEXITCODE -ne 0) { throw "App publish failed" }

Write-Host "==> Publishing OpenWu.Cli (CLI, win-x64, self-contained)..." -ForegroundColor Cyan
& $dotnet publish "$rootDir\src\OpenWu.Cli\OpenWu.Cli.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o "$rootDir\artifacts\win-x64"
if ($LASTEXITCODE -ne 0) { throw "CLI publish failed" }

# Sanity: shell can extract an icon from each EXE (not the default blank)
Add-Type -AssemblyName System.Drawing
foreach ($name in @('OpenWU.exe', 'openwu-cli.exe')) {
    $exe = Join-Path $outDir $name
    if (-not (Test-Path $exe)) { throw "Missing output: $exe" }
    $ico = [System.Drawing.Icon]::ExtractAssociatedIcon($exe)
    if ($null -eq $ico) { throw "No associated icon on $name" }
    Write-Host "    Icon OK: $name ($($ico.Width)x$($ico.Height))" -ForegroundColor Green
    $ico.Dispose()
}

Write-Host "==> Build complete! Output located at:" -ForegroundColor Green
Write-Host "    $rootDir\artifacts\win-x64\OpenWU.exe      (GUI, WinExe, no console)" -ForegroundColor Yellow
Write-Host "    $rootDir\artifacts\win-x64\openwu-cli.exe  (CLI, console)" -ForegroundColor Yellow
Write-Host "Note: openwu.exe cannot be used as CLI name on Windows (collides with OpenWU.exe)." -ForegroundColor DarkYellow
Write-Host "Tip: Explorer may cache old icons - restart Explorer or rename the file if the shell still shows the default." -ForegroundColor DarkYellow
