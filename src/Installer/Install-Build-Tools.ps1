#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Installs InnoSetup and WiX Toolset for building installers
#>

$ErrorActionPreference = "Stop"

Write-Host "=== FluentTB Installer Build Tools Setup ===" -ForegroundColor Cyan
Write-Host ""

# Check if winget is available
$wingetAvailable = Get-Command winget -ErrorAction SilentlyContinue

if ($wingetAvailable) {
    Write-Host "[1/2] Installing InnoSetup via winget..." -ForegroundColor Green
    try {
        winget install --id JRSoftware.InnoSetup --silent --accept-source-agreements --accept-package-agreements
        Write-Host "  InnoSetup installed successfully!" -ForegroundColor Green
    } catch {
        Write-Host "  Failed to install InnoSetup via winget" -ForegroundColor Yellow
        Write-Host "  Please download manually from: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "[2/2] Installing WiX Toolset via winget..." -ForegroundColor Green
    try {
        winget install --id WiXToolset.WiX --version 3.11.2 --silent --accept-source-agreements --accept-package-agreements
        Write-Host "  WiX Toolset installed successfully!" -ForegroundColor Green
    } catch {
        Write-Host "  Failed to install WiX via winget" -ForegroundColor Yellow
        Write-Host "  Please download manually from: https://github.com/wixtoolset/wix3/releases" -ForegroundColor Yellow
    }
} else {
    Write-Host "winget not found. Manual installation required:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "1. InnoSetup (for EXE installer):" -ForegroundColor Cyan
    Write-Host "   Download: https://jrsoftware.org/isdl.php" -ForegroundColor White
    Write-Host "   Install to default location: C:\Program Files (x86)\Inno Setup 6\" -ForegroundColor Gray
    Write-Host ""
    Write-Host "2. WiX Toolset 3.11 (for MSI installer):" -ForegroundColor Cyan
    Write-Host "   Download: https://github.com/wixtoolset/wix3/releases/download/wix3112rtm/wix311.exe" -ForegroundColor White
    Write-Host "   Install to default location: C:\Program Files (x86)\WiX Toolset v3.11\" -ForegroundColor Gray
    Write-Host ""
    Write-Host "After installation, run: .\Build-Installer.ps1" -ForegroundColor Green
    exit 0
}

Write-Host ""
Write-Host "=== Installation Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Close and reopen PowerShell (to refresh PATH)" -ForegroundColor White
Write-Host "2. Run: cd ..; .\Build-Installer.ps1" -ForegroundColor White
Write-Host ""
Write-Host "This will create:" -ForegroundColor Gray
Write-Host "  - Output\FluentTB-Setup-1.0.0.exe (InnoSetup)" -ForegroundColor Gray
Write-Host "  - Output\FluentTB.msi (WiX MSI)" -ForegroundColor Gray
