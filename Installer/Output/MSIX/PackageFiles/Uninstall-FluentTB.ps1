#Requires -RunAsAdministrator
<#
.SYNOPSIS
    FluentTB Uninstaller Script
.DESCRIPTION
    Removes FluentTB from the system
.NOTES
    Version: 1.0
#>

$ErrorActionPreference = "Stop"

Write-Host "=== FluentTB Uninstaller ===" -ForegroundColor Cyan
Write-Host ""

# Get installation path from registry
$uninstallKey = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\FluentTB"
if (Test-Path $uninstallKey) {
    $InstallPath = (Get-ItemProperty -Path $uninstallKey).InstallLocation
} else {
    $InstallPath = "$env:ProgramFiles\FluentTB"
}

# Stop running processes
Write-Host "Stopping FluentTB processes..." -ForegroundColor Yellow
Get-Process -Name "FluentTB" -ErrorAction SilentlyContinue | Stop-Process -Force

# Remove shortcuts
Write-Host "Removing shortcuts..." -ForegroundColor Yellow
$startMenuPath = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\FluentTB.lnk"
$desktopPath = [Environment]::GetFolderPath("CommonDesktopDirectory") + "\FluentTB.lnk"
$startupPath = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\Startup\FluentTB.lnk"

Remove-Item -Path $startMenuPath -Force -ErrorAction SilentlyContinue
Remove-Item -Path $desktopPath -Force -ErrorAction SilentlyContinue
Remove-Item -Path $startupPath -Force -ErrorAction SilentlyContinue

# Remove user startup shortcuts
$userStartup = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup\FluentTB.lnk"
Remove-Item -Path $userStartup -Force -ErrorAction SilentlyContinue

# Remove installation directory
if (Test-Path $InstallPath) {
    Write-Host "Removing installation directory: $InstallPath" -ForegroundColor Yellow
    Remove-Item -Path $InstallPath -Recurse -Force
}

# Remove registry entries
Write-Host "Removing registry entries..." -ForegroundColor Yellow
if (Test-Path $uninstallKey) {
    Remove-Item -Path $uninstallKey -Recurse -Force
}

# Note: User data in %LOCALAPPDATA%\FluentTB is NOT removed
Write-Host ""
Write-Host "=== Uninstallation Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Note: User settings in %LOCALAPPDATA%\FluentTB have been preserved." -ForegroundColor Cyan
Write-Host "To remove settings, delete: $env:LOCALAPPDATA\FluentTB" -ForegroundColor Cyan
