@echo off
REM Quick build script for FluentTB installers
REM Runs the PowerShell build script with elevated privileges

echo === FluentTB Installer Builder ===
echo.

REM Check for Administrator privileges
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: This script requires Administrator privileges.
    echo Please right-click and select "Run as Administrator"
    pause
    exit /b 1
)

REM Run the PowerShell build script
powershell.exe -ExecutionPolicy Bypass -File "%~dp0Build-Installer.ps1"

if %errorLevel% neq 0 (
    echo.
    echo ERROR: Build failed. Check the output above for details.
    pause
    exit /b %errorLevel%
)

echo.
echo === Build Complete ===
echo Check the Output folder for installers.
echo.
pause
