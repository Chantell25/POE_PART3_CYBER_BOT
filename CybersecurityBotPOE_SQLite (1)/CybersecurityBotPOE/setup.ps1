<#
.SYNOPSIS
  One-time setup: downloads all NuGet packages into the local .\packages folder
  so the project can be built offline on any Windows machine afterwards.

.HOW TO RUN
  Right-click setup.ps1 → "Run with PowerShell"
  OR in a terminal: .\setup.ps1
#>

Write-Host ""
Write-Host "=== CybersecurityBot — One-Time Package Setup ===" -ForegroundColor Cyan
Write-Host ""

# ── 1. Find nuget.exe (download it if missing) ────────────────────────────────
$nugetPath = "$PSScriptRoot\packages\nuget.exe"
if (-not (Test-Path $nugetPath)) {
    Write-Host "Downloading nuget.exe..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Force -Path "$PSScriptRoot\packages" | Out-Null
    try {
        Invoke-WebRequest -Uri "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" `
                          -OutFile $nugetPath -UseBasicParsing
        Write-Host "  nuget.exe downloaded OK." -ForegroundColor Green
    } catch {
        Write-Host "  ERROR: Could not download nuget.exe. Check your internet connection." -ForegroundColor Red
        Write-Host "  $_"
        Read-Host "Press Enter to exit"
        exit 1
    }
}

# ── 2. Download Microsoft.Data.Sqlite and all its dependencies ────────────────
$packages = @(
    "Microsoft.Data.Sqlite,8.0.0",
    "SQLitePCLRaw.bundle_e_sqlite3,2.1.8",
    "SQLitePCLRaw.core,2.1.8",
    "SQLitePCLRaw.lib.e_sqlite3,2.1.8",
    "SQLitePCLRaw.provider.e_sqlite3,2.1.8"
)

$dest = "$PSScriptRoot\packages"
$allOk = $true

foreach ($pkg in $packages) {
    $parts = $pkg -split ","
    $name  = $parts[0]
    $ver   = $parts[1]
    $nupkg = "$dest\$name.$ver.nupkg"

    if (Test-Path $nupkg) {
        Write-Host "  [SKIP] $name $ver already present." -ForegroundColor DarkGray
        continue
    }

    Write-Host "  Downloading $name $ver..." -ForegroundColor Yellow
    try {
        & $nugetPath install $name -Version $ver -OutputDirectory $dest -Source https://api.nuget.org/v3/index.json -ExcludeVersion:$false -NonInteractive 2>&1 | Out-Null
        # Copy the .nupkg to the root of packages\ so it acts as a flat local feed
        $found = Get-ChildItem -Path $dest -Filter "$name.$ver.nupkg" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($found -and $found.FullName -ne $nupkg) {
            Copy-Item $found.FullName $nupkg -Force
        }
        Write-Host "    OK" -ForegroundColor Green
    } catch {
        Write-Host "    FAILED: $_" -ForegroundColor Red
        $allOk = $false
    }
}

Write-Host ""
if ($allOk) {
    Write-Host "All packages ready. You can now build offline:" -ForegroundColor Green
    Write-Host "  dotnet restore CybersecurityBotGUI.csproj" -ForegroundColor White
    Write-Host "  dotnet build   CybersecurityBotGUI.csproj --configuration Release" -ForegroundColor White
} else {
    Write-Host "Some packages failed. Fix the errors above and re-run setup.ps1." -ForegroundColor Red
}

Write-Host ""
Read-Host "Press Enter to exit"
