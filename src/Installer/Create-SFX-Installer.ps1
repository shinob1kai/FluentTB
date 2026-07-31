#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Creates self-extracting installer for FluentTB
#>

param([string]$Version = "2026.3.1")

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression.FileSystem

Write-Host "=== Creating FluentTB Installers ===" -ForegroundColor Cyan
Write-Host ""

$rootDir = Split-Path $PSScriptRoot -Parent
$buildDir = Join-Path $rootDir "FluentTB\bin\Release"
$outputDir = Join-Path $PSScriptRoot "Output"

if (-not (Test-Path $buildDir)) {
    Write-Host "ERROR: Build not found!" -ForegroundColor Red
    exit 1
}

New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

# Create ZIP package
Write-Host "[1/2] Creating ZIP package..." -ForegroundColor Green
$zipPath = Join-Path $outputDir "FluentTB-$Version-Portable.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
[System.IO.Compression.ZipFile]::CreateFromDirectory($buildDir, $zipPath)
$zipSize = (Get-Item $zipPath).Length / 1MB
Write-Host "  Created: FluentTB-$Version-Portable.zip ($([math]::Round($zipSize, 2)) MB)" -ForegroundColor Green

# Create self-installing PS1
Write-Host "[2/2] Creating self-extracting installer..." -ForegroundColor Green

$zipBytes = [System.IO.File]::ReadAllBytes($zipPath)
$base64 = [System.Convert]::ToBase64String($zipBytes)

$sfxScript = @"
#Requires -RunAsAdministrator
`$ErrorActionPreference = 'Stop'
`$ProgressPreference = 'SilentlyContinue'

Add-Type -AssemblyName System.IO.Compression.FileSystem

Write-Host ''
Write-Host '================================' -ForegroundColor Cyan
Write-Host '  FluentTB Installer v$Version' -ForegroundColor Cyan
Write-Host '================================' -ForegroundColor Cyan
Write-Host ''

`$InstallPath = "`$env:ProgramFiles\FluentTB"

if (Test-Path `$InstallPath) {
    `$response = Read-Host "FluentTB already installed. Overwrite? (Y/N)"
    if (`$response -ne 'Y' -and `$response -ne 'y') {
        Write-Host 'Installation cancelled.' -ForegroundColor Yellow
        exit 0
    }
    Stop-Process -Name 'FluentTB' -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
    Remove-Item -Path `$InstallPath -Recurse -Force
}

Write-Host '[1/5] Extracting files...' -ForegroundColor Green
`$zipData = '$base64'
`$zipBytes = [System.Convert]::FromBase64String(`$zipData)
`$tempZip = "`$env:TEMP\fluenttb_setup.zip"
[System.IO.File]::WriteAllBytes(`$tempZip, `$zipBytes)

Write-Host '[2/5] Installing to' `$InstallPath'...' -ForegroundColor Green
New-Item -ItemType Directory -Path `$InstallPath -Force | Out-Null
[System.IO.Compression.ZipFile]::ExtractToDirectory(`$tempZip, `$InstallPath)
Remove-Item `$tempZip -Force

Write-Host '[3/5] Creating data directory...' -ForegroundColor Green
`$dataPath = "`$env:LOCALAPPDATA\FluentTB"
New-Item -ItemType Directory -Path `$dataPath -Force | Out-Null

Write-Host '[4/5] Creating shortcuts...' -ForegroundColor Green
`$startMenu = "`$env:ProgramData\Microsoft\Windows\Start Menu\Programs"
`$shell = New-Object -ComObject WScript.Shell
`$shortcut = `$shell.CreateShortcut("`$startMenu\FluentTB.lnk")
`$shortcut.TargetPath = "`$InstallPath\FluentTB.exe"
`$shortcut.WorkingDirectory = `$InstallPath
`$shortcut.Description = 'FluentTB - Customize Windows Taskbar'
`$shortcut.IconLocation = "`$InstallPath\FluentTB.exe,0"
`$shortcut.Save()

Write-Host '[5/5] Registering with Windows...' -ForegroundColor Green
`$regKey = 'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\FluentTB'
New-Item -Path `$regKey -Force | Out-Null
Set-ItemProperty -Path `$regKey -Name 'DisplayName' -Value 'FluentTB'
Set-ItemProperty -Path `$regKey -Name 'DisplayVersion' -Value '$Version'
Set-ItemProperty -Path `$regKey -Name 'Publisher' -Value 'FluentTB Team'
Set-ItemProperty -Path `$regKey -Name 'InstallLocation' -Value `$InstallPath
Set-ItemProperty -Path `$regKey -Name 'UninstallString' -Value "powershell.exe -ExecutionPolicy Bypass -File ```"`$InstallPath\Uninstall.ps1```""
Set-ItemProperty -Path `$regKey -Name 'DisplayIcon' -Value "`$InstallPath\FluentTB.exe"
Set-ItemProperty -Path `$regKey -Name 'NoModify' -Value 1
Set-ItemProperty -Path `$regKey -Name 'NoRepair' -Value 1

`$uninstaller = @'
#Requires -RunAsAdministrator
Write-Host 'Uninstalling FluentTB...' -ForegroundColor Yellow
Stop-Process -Name 'FluentTB' -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1
Remove-Item -Path "`$env:ProgramFiles\FluentTB" -Recurse -Force
Remove-Item -Path "`$env:ProgramData\Microsoft\Windows\Start Menu\Programs\FluentTB.lnk" -Force -ErrorAction SilentlyContinue
Remove-Item -Path 'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\FluentTB' -Force -ErrorAction SilentlyContinue
Write-Host 'FluentTB uninstalled.' -ForegroundColor Green
'@
Set-Content -Path "`$InstallPath\Uninstall.ps1" -Value `$uninstaller

Write-Host ''
Write-Host '================================' -ForegroundColor Green
Write-Host '  Installation Complete!' -ForegroundColor Green
Write-Host '================================' -ForegroundColor Green
Write-Host ''
Write-Host 'Installed:' `$InstallPath -ForegroundColor White
Write-Host 'Data:     ' "`$env:LOCALAPPDATA\FluentTB" -ForegroundColor White
Write-Host ''
`$launch = Read-Host 'Launch FluentTB now? (Y/N)'
if (`$launch -eq 'Y' -or `$launch -eq 'y') {
    Start-Process "`$InstallPath\FluentTB.exe"
}
"@

$sfxPath = Join-Path $outputDir "FluentTB-Setup-$Version.ps1"
Set-Content -Path $sfxPath -Value $sfxScript -Encoding UTF8
$sfxSize = (Get-Item $sfxPath).Length / 1MB
Write-Host "  Created: FluentTB-Setup-$Version.ps1 ($([math]::Round($sfxSize, 2)) MB)" -ForegroundColor Green

Write-Host ""
Write-Host "=== Build Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Created installers:" -ForegroundColor Cyan
Write-Host "  1. FluentTB-Setup-$Version.ps1 (Self-extracting)" -ForegroundColor White
Write-Host "  2. FluentTB-$Version-Portable.zip (Portable)" -ForegroundColor White
Write-Host "  3. Install-FluentTB.ps1 (Standard)" -ForegroundColor White
Write-Host ""
Write-Host "Install with:" -ForegroundColor Yellow
Write-Host "  .\FluentTB-Setup-$Version.ps1" -ForegroundColor Gray
Write-Host ""
