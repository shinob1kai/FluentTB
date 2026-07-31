#Requires -RunAsAdministrator
<#
.SYNOPSIS
    FluentTB Installer Script
.DESCRIPTION
    Installs FluentTB to Program Files and creates shortcuts
.NOTES
    Version: 1.0
    Author: FluentTB Team
#>

param(
    [string]$InstallPath = "$env:ProgramFiles\FluentTB",
    [switch]$CreateStartMenuShortcut = $true,
    [switch]$CreateDesktopShortcut = $false,
    [switch]$AddToStartup = $false
)

$ErrorActionPreference = "Stop"

Write-Host "=== FluentTB Installer ===" -ForegroundColor Cyan
Write-Host ""

# Check if already installed
if (Test-Path $InstallPath) {
    $response = Read-Host "FluentTB is already installed. Overwrite? (Y/N)"
    if ($response -ne 'Y' -and $response -ne 'y') {
        Write-Host "Installation cancelled." -ForegroundColor Yellow
        exit 0
    }
    Write-Host "Removing existing installation..." -ForegroundColor Yellow
    Remove-Item -Path $InstallPath -Recurse -Force
}

# Create installation directory
Write-Host "Creating installation directory: $InstallPath" -ForegroundColor Green
New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null

# Copy files
Write-Host "Copying application files..." -ForegroundColor Green
$sourcePath = Join-Path $PSScriptRoot "..\FluentTB\bin\Release"
if (-not (Test-Path $sourcePath)) {
    Write-Host "Error: Build output not found at $sourcePath" -ForegroundColor Red
    Write-Host "Please build the project in Release mode first." -ForegroundColor Red
    exit 1
}

Copy-Item -Path "$sourcePath\*" -Destination $InstallPath -Recurse -Force

# Create Start Menu shortcut
if ($CreateStartMenuShortcut) {
    Write-Host "Creating Start Menu shortcut..." -ForegroundColor Green
    $startMenuPath = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs"
    $WshShell = New-Object -ComObject WScript.Shell
    $shortcut = $WshShell.CreateShortcut("$startMenuPath\FluentTB.lnk")
    $shortcut.TargetPath = "$InstallPath\FluentTB.exe"
    $shortcut.WorkingDirectory = $InstallPath
    $shortcut.Description = "FluentTB - Customize Windows Taskbar"
    $shortcut.IconLocation = "$InstallPath\FluentTB.exe,0"
    $shortcut.Save()
}

# Create Desktop shortcut
if ($CreateDesktopShortcut) {
    Write-Host "Creating Desktop shortcut..." -ForegroundColor Green
    $desktopPath = [Environment]::GetFolderPath("CommonDesktopDirectory")
    $WshShell = New-Object -ComObject WScript.Shell
    $shortcut = $WshShell.CreateShortcut("$desktopPath\FluentTB.lnk")
    $shortcut.TargetPath = "$InstallPath\FluentTB.exe"
    $shortcut.WorkingDirectory = $InstallPath

    $shortcut.Description = "FluentTB - Customize Windows Taskbar"
    $shortcut.IconLocation = "$InstallPath\FluentTB.exe,0"
    $shortcut.Save()
}

# Add to Startup
if ($AddToStartup) {
    Write-Host "Adding to Startup..." -ForegroundColor Green
    $startupPath = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\Startup"
    $WshShell = New-Object -ComObject WScript.Shell
    $shortcut = $WshShell.CreateShortcut("$startupPath\FluentTB.lnk")
    $shortcut.TargetPath = "$InstallPath\FluentTB.exe"
    $shortcut.WorkingDirectory = $InstallPath
    $shortcut.Save()
}

# Add to registry for uninstaller
Write-Host "Registering uninstaller..." -ForegroundColor Green
$uninstallKey = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\FluentTB"
New-Item -Path $uninstallKey -Force | Out-Null
Set-ItemProperty -Path $uninstallKey -Name "DisplayName" -Value "FluentTB"
Set-ItemProperty -Path $uninstallKey -Name "DisplayVersion" -Value "1.0.0"
Set-ItemProperty -Path $uninstallKey -Name "Publisher" -Value "FluentTB Team"
Set-ItemProperty -Path $uninstallKey -Name "InstallLocation" -Value $InstallPath
Set-ItemProperty -Path $uninstallKey -Name "UninstallString" -Value "powershell.exe -ExecutionPolicy Bypass -File `"$InstallPath\Uninstall-FluentTB.ps1`""
Set-ItemProperty -Path $uninstallKey -Name "DisplayIcon" -Value "$InstallPath\FluentTB.exe,0"
Set-ItemProperty -Path $uninstallKey -Name "NoModify" -Value 1
Set-ItemProperty -Path $uninstallKey -Name "NoRepair" -Value 1

Write-Host ""
Write-Host "=== Installation Complete ===" -ForegroundColor Green
Write-Host "FluentTB has been installed to: $InstallPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "You can now run FluentTB from the Start Menu." -ForegroundColor White
