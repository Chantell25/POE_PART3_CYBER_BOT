<#
.SYNOPSIS
  Restores and builds the project.
  If dotnet restore fails (NU1301), it automatically runs setup.ps1 first.
#>

Write-Host ""
Write-Host "=== CybersecurityBot — Build ===" -ForegroundColor Cyan

$proj = "$PSScriptRoot\CybersecurityBotGUI.csproj"

Write-Host "Restoring packages..." -ForegroundColor Yellow
$restore = dotnet restore $proj 2>&1
if ($LASTEXITCODE -ne 0) {
    if ($restore -match "NU1301") {
        Write-Host ""
        Write-Host "NuGet cannot reach the internet (NU1301). Running setup.ps1 to cache packages locally..." -ForegroundColor Yellow
        & "$PSScriptRoot\setup.ps1"
        Write-Host "Retrying restore..." -ForegroundColor Yellow
        dotnet restore $proj
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Restore still failing. Check errors above." -ForegroundColor Red
            Read-Host "Press Enter to exit"
            exit 1
        }
    } else {
        Write-Host $restore -ForegroundColor Red
        Read-Host "Press Enter to exit"
        exit 1
    }
}

Write-Host "Building Release..." -ForegroundColor Yellow
dotnet build $proj --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed. Check errors above." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host ""
Write-Host "Build succeeded! Output is in bin\Release\net8.0-windows\" -ForegroundColor Green
Write-Host "Run: bin\Release\net8.0-windows\CybersecurityBotGUI.exe" -ForegroundColor White
Read-Host "Press Enter to exit"
