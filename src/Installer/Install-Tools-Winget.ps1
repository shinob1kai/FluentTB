#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Installs InnoSetup and WiX Toolset via winget
#>

$ErrorActionPreference = "Stop"

Write-Host "=== Installing Build Tools ===" -ForegroundColor Cyan
Write-Host ""

# Check if winget is available
$winget = Get-Command winget -ErrorAction SilentlyContinue
if (-not $winget) {
    Write-Host "ERROR: winget not found!" -ForegroundColor Red
    Write-Host "Please install App Installer from Microsoft Store" -ForegroundColor Yellow
    exit 1
}

Write-Host "[1/2] Installing InnoSetup 6..." -ForegroundColor Green
try {
    winget install --id JRSoftware.InnoSetup --accept-source-agreements --accept-package-agreements --silent
    Write-Host "  InnoSetup installed!" -ForegroundColor Green
} catch {
    Write-Host "  WARNING: InnoSetup installation failed" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "[2/2] Installing WiX Toolset 3.11..." -ForegroundColor Green
try {
    winget install --id WiXToolset.WiX --version 3.11.2 --accept-source-agreements --accept-package-agreements --silent
    Write-Host "  WiX Toolset installed!" -ForegroundColor Green
} catch {
    Write-Host "  WARNING: WiX installation failed" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Installation Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Installed locations:" -ForegroundColor Cyan
Write-Host "  InnoSetup: C:\Program Files (x86)\Inno Setup 6\" -ForegroundColor Gray
Write-Host "  WiX:       C:\Program Files (x86)\WiX Toolset v3.11\" -ForegroundColor Gray
Write-Host ""
Write-Host "Next step:" -ForegroundColor Yellow
Write-Host "  .\Build-Installer.ps1" -ForegroundColor White
Write-Host ""
Write-Host "This will create:" -ForegroundColor Cyan
Write-Host "  - FluentTB-Setup-2026.3.1.exe (InnoSetup)" -ForegroundColor White
Write-Host "  - FluentTB.msi (WiX MSI)" -ForegroundColor White
Write-Host "  - FluentTB-Setup-2026.3.1.ps1 (PowerShell)" -ForegroundColor White
Write-Host ""
