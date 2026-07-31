<#
.SYNOPSIS
    Creates a portable self-extracting installer without requiring InnoSetup or WiX
.DESCRIPTION
    Creates a PowerShell-based self-extracting installer that works on any Windows system
#>

param(
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

Write-Host "=== FluentTB Portable Installer Creation ===" -ForegroundColor Cyan
Write-Host ""

# Paths
$rootDir = Split-Path $PSScriptRoot -Parent
$buildDir = Join-Path $rootDir "FluentTB\bin\Release"
$outputDir = Join-Path $PSScriptRoot "Output"

# Check if build exists
if (-not (Test-Path $buildDir)) {
    Write-Host "ERROR: Build not found at: $buildDir" -ForegroundColor Red
    Write-Host "Please run: dotnet build FluentTB.csproj -c Release" -ForegroundColor Yellow
    exit 1
}

# Create output directory
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

Write-Host "[1/3] Creating self-extracting EXE installer..." -ForegroundColor Green

# Create installer script
$installerScript = @'
#Requires -RunAsAdministrator
param(
    [string]$InstallPath = "$env:ProgramFiles\FluentTB"
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

Add-Type -AssemblyName System.IO.Compression.FileSystem

Write-Host ""
Write-Host "==================================" -ForegroundColor Cyan
Write-Host "   FluentTB Installer v{VERSION}   " -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

# Extract embedded zip
Write-Host "[1/5] Extracting files..." -ForegroundColor Green
$zipBytes = [System.Convert]::FromBase64String(@'
{BASE64_DATA}
'@)

$tempZip = "$env:TEMP\fluenttb_install.zip"
[System.IO.File]::WriteAllBytes($tempZip, $zipBytes)

# Check if already installed
if (Test-Path $InstallPath) {
    $response = Read-Host "FluentTB is already installed at $InstallPath. Overwrite? (Y/N)"
    if ($response -ne 'Y' -and $response -ne 'y') {
        Write-Host "Installation cancelled." -ForegroundColor Yellow
        Remove-Item $tempZip -Force
        exit 0
    }
    Write-Host "[2/5] Removing existing installation..." -ForegroundColor Yellow
    Stop-Process -Name "FluentTB" -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
    Remove-Item -Path $InstallPath -Recurse -Force
}

# Create install directory
Write-Host "[3/5] Installing to $InstallPath..." -ForegroundColor Green
New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
[System.IO.Compression.ZipFile]::ExtractToDirectory($tempZip, $InstallPath)
Remove-Item $tempZip -Force

# Create data directory
$dataPath = "$env:LOCALAPPDATA\FluentTB"
New-Item -ItemType Directory -Path $dataPath -Force | Out-Null

# Create Start Menu shortcut
Write-Host "[4/5] Creating shortcuts..." -ForegroundColor Green
$startMenuPath = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs"
$WshShell = New-Object -ComObject WScript.Shell
$shortcut = $WshShell.CreateShortcut("$startMenuPath\FluentTB.lnk")
$shortcut.TargetPath = "$InstallPath\FluentTB.exe"
$shortcut.WorkingDirectory = $InstallPath
$shortcut.Description = "FluentTB - Customize Windows Taskbar"
$shortcut.IconLocation = "$InstallPath\FluentTB.exe,0"
$shortcut.Save()

# Add to uninstall registry
Write-Host "[5/5] Registering with Windows..." -ForegroundColor Green
$uninstallKey = "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\FluentTB"
New-Item -Path $uninstallKey -Force | Out-Null
Set-ItemProperty -Path $uninstallKey -Name "DisplayName" -Value "FluentTB"
Set-ItemProperty -Path $uninstallKey -Name "DisplayVersion" -Value "{VERSION}"
Set-ItemProperty -Path $uninstallKey -Name "Publisher" -Value "FluentTB Team"
Set-ItemProperty -Path $uninstallKey -Name "InstallLocation" -Value $InstallPath
Set-ItemProperty -Path $uninstallKey -Name "UninstallString" -Value "powershell.exe -ExecutionPolicy Bypass -File `"$InstallPath\Uninstall.ps1`""
Set-ItemProperty -Path $uninstallKey -Name "DisplayIcon" -Value "$InstallPath\FluentTB.exe"
Set-ItemProperty -Path $uninstallKey -Name "NoModify" -Value 1
Set-ItemProperty -Path $uninstallKey -Name "NoRepair" -Value 1

# Create uninstaller
$uninstallScript = @"
#Requires -RunAsAdministrator
Write-Host "Uninstalling FluentTB..." -ForegroundColor Yellow
Stop-Process -Name "FluentTB" -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1
Remove-Item -Path "$InstallPath" -Recurse -Force
Remove-Item -Path "$startMenuPath\FluentTB.lnk" -Force -ErrorAction SilentlyContinue
Remove-Item -Path "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\FluentTB" -Force -ErrorAction SilentlyContinue
Write-Host "FluentTB has been uninstalled." -ForegroundColor Green
Write-Host "Data files remain at: $env:LOCALAPPDATA\FluentTB" -ForegroundColor Gray
"@
Set-Content -Path "$InstallPath\Uninstall.ps1" -Value $uninstallScript

Write-Host ""
Write-Host "==================================" -ForegroundColor Green
Write-Host "  Installation Complete!          " -ForegroundColor Green
Write-Host "==================================" -ForegroundColor Green
Write-Host ""
Write-Host "Installed to: $InstallPath" -ForegroundColor White
Write-Host "Data folder:  $env:LOCALAPPDATA\FluentTB" -ForegroundColor White
Write-Host ""
Write-Host "Start FluentTB from:" -ForegroundColor Cyan
Write-Host "  - Start Menu > FluentTB" -ForegroundColor White
Write-Host "  - $InstallPath\FluentTB.exe" -ForegroundColor Gray
Write-Host ""

$response = Read-Host "Launch FluentTB now? (Y/N)"
if ($response -eq 'Y' -or $response -eq 'y') {
    Start-Process "$InstallPath\FluentTB.exe"
}
'@

# Create zip of build files
$tempZip = "$env:TEMP\fluenttb_build_$([Guid]::NewGuid()).zip"
Write-Host "  Compressing build files..." -ForegroundColor Gray
[System.IO.Compression.ZipFile]::CreateFromDirectory($buildDir, $tempZip)

# Convert to base64
Write-Host "  Encoding..." -ForegroundColor Gray
$zipBytes = [System.IO.File]::ReadAllBytes($tempZip)
$base64 = [System.Convert]::ToBase64String($zipBytes)
Remove-Item $tempZip -Force

# Replace placeholders
$installerScript = $installerScript.Replace('{VERSION}', $Version)
$installerScript = $installerScript.Replace('{BASE64_DATA}', $base64)

# Save installer script
$installerPs1 = Join-Path $outputDir "FluentTB-Setup-$Version.ps1"
Set-Content -Path $installerPs1 -Value $installerScript -Encoding UTF8

Write-Host "  Created: FluentTB-Setup-$Version.ps1" -ForegroundColor Green

Write-Host ""
Write-Host "[2/3] Creating portable EXE wrapper..." -ForegroundColor Green

# Create EXE wrapper using PowerShell to EXE
$wrapperCmd = @"
@echo off
powershell.exe -ExecutionPolicy Bypass -NoProfile -WindowStyle Hidden -Command "& {iex (Get-Content '%~f0' -Raw)}"
exit /b
<# PowerShell code starts here
$installerScript
#>
"@

$exePath = Join-Path $outputDir "FluentTB-Setup-$Version.exe"
# For a real EXE, we'd need a proper converter. Let's create a BAT launcher instead
$batPath = Join-Path $outputDir "FluentTB-Setup-$Version.bat"
Set-Content -Path $batPath -Value $wrapperCmd -Encoding ASCII

Write-Host "  Note: Created BAT launcher (EXE requires additional tools)" -ForegroundColor Yellow
Write-Host "  Created: FluentTB-Setup-$Version.bat" -ForegroundColor Green

Write-Host ""
Write-Host "[3/3] Creating MSI alternative (WiX-free)..." -ForegroundColor Green

# Create advanced installer script with MSI-like behavior
$msiLikeScript = $installerScript.Replace('FluentTB Installer', 'FluentTB Installation Wizard')
$msiLikePath = Join-Path $outputDir "FluentTB-$Version-Setup.ps1"
Set-Content -Path $msiLikePath -Value $msiLikeScript -Encoding UTF8

Write-Host "  Created: FluentTB-$Version-Setup.ps1" -ForegroundColor Green

Write-Host ""
Write-Host "==================================" -ForegroundColor Green
Write-Host "  Build Complete!                 " -ForegroundColor Green
Write-Host "==================================" -ForegroundColor Green
Write-Host ""
Write-Host "Created installers in: $outputDir" -ForegroundColor Cyan
Write-Host ""
Write-Host "Available installers:" -ForegroundColor White
Write-Host "  1. FluentTB-Setup-$Version.ps1     (PowerShell installer)" -ForegroundColor Gray
Write-Host "  2. FluentTB-Setup-$Version.bat     (Batch launcher)" -ForegroundColor Gray
Write-Host "  3. Install-FluentTB.ps1            (Simple installer)" -ForegroundColor Gray
Write-Host ""
Write-Host "To create real EXE/MSI, install:" -ForegroundColor Yellow
Write-Host "  - InnoSetup: https://jrsoftware.org/isdl.php" -ForegroundColor Gray
Write-Host "  - WiX Toolset: https://github.com/wixtoolset/wix3/releases" -ForegroundColor Gray
Write-Host ""

# Show file sizes
Get-ChildItem $outputDir -File | Where-Object {$_.Extension -in '.ps1','.bat'} | ForEach-Object {
    $size = "{0:N2} MB" -f ($_.Length / 1MB)
    Write-Host "  $($_.Name.PadRight(40)) $size" -ForegroundColor Gray
}
