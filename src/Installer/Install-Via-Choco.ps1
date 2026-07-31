#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Installs InnoSetup and WiX via Chocolatey
#>

$ErrorActionPreference = "Stop"

Write-Host "=== Installing Build Tools via Chocolatey ===" -ForegroundColor Cyan
Write-Host ""

# Check if choco is available
$chocoAvailable = Get-Command choco -ErrorAction SilentlyContinue

if (-not $chocoAvailable) {
    Write-Host "Chocolatey not found. Installing Chocolatey first..." -ForegroundColor Yellow
    Set-ExecutionPolicy Bypass -Scope Process -Force
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
    Invoke-Expression ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
    
    # Refresh PATH
    $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
}

Write-Host "[1/2] Installing InnoSetup..." -ForegroundColor Green
choco install innosetup -y

Write-Host ""
Write-Host "[2/2] Installing WiX Toolset..." -ForegroundColor Green
choco install wixtoolset -y

Write-Host ""
Write-Host "=== Installation Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Restart PowerShell and run: .\Build-Installer.ps1" -ForegroundColor Cyan
