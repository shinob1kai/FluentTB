<#
.SYNOPSIS
    Automated build script for FluentTB installers
.DESCRIPTION
    Builds the application and creates all installer formats (PS1, EXE, MSI)
.PARAMETER BuildOnly
    Only build the application without creating installers
.PARAMETER SkipBuild
    Skip building and use existing binaries
.PARAMETER InnoSetupPath
    Path to InnoSetup compiler (ISCC.exe)
.PARAMETER WixPath
    Path to WiX Toolset binaries directory
#>

param(
    [switch]$BuildOnly,
    [switch]$SkipBuild,
    [string]$InnoSetupPath = "C:\ProgramData\chocolatey\bin\ISCC.exe",
    [string]$WixPath = "C:\Program Files (x86)\WiX Toolset v3.14\bin"
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$projectPath = Join-Path $projectRoot "FluentTB\FluentTB.csproj"
$releasePath = Join-Path $projectRoot "FluentTB\bin\Release"
$outputDir = Join-Path $scriptDir "Output"

Write-Host "=== FluentTB Build & Installer Creation ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Build the application
if (-not $SkipBuild) {
    Write-Host "[1/4] Building FluentTB in Release mode..." -ForegroundColor Green
    
    # Clean previous build
    if (Test-Path $releasePath) {
        Write-Host "  Cleaning previous build..." -ForegroundColor Yellow
        Remove-Item -Path $releasePath -Recurse -Force
    }
    
    # Restore NuGet packages
    Write-Host "  Restoring NuGet packages..." -ForegroundColor Yellow
    & dotnet restore $projectPath --packages "$projectRoot\FluentTB\packages"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ERROR: NuGet restore failed" -ForegroundColor Red
        exit 1
    }
    
    # Build
    Write-Host "  Compiling..." -ForegroundColor Yellow
    & dotnet build $projectPath -c Release -v minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ERROR: Build failed" -ForegroundColor Red
        Write-Host "  Please fix compilation errors and try again." -ForegroundColor Red
        exit 1
    }
    
    Write-Host "  Build successful!" -ForegroundColor Green
} else {
    Write-Host "[1/4] Skipping build (using existing binaries)..." -ForegroundColor Yellow
}

# Verify build output exists
if (-not (Test-Path "$releasePath\FluentTB.exe")) {
    Write-Host "ERROR: FluentTB.exe not found at $releasePath" -ForegroundColor Red
    Write-Host "Please build the project first or remove -SkipBuild parameter." -ForegroundColor Red
    exit 1
}

if ($BuildOnly) {
    Write-Host ""
    Write-Host "Build complete. Installers not created (BuildOnly mode)." -ForegroundColor Cyan
    exit 0
}

# Create output directory
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

Write-Host ""
Write-Host "[2/4] Copying PowerShell installers..." -ForegroundColor Green

# Copy PowerShell scripts to output
Copy-Item -Path "$scriptDir\Install-FluentTB.ps1" -Destination $outputDir -Force
Copy-Item -Path "$scriptDir\Uninstall-FluentTB.ps1" -Destination "$releasePath\Uninstall-FluentTB.ps1" -Force
Write-Host "  PowerShell installers ready" -ForegroundColor Green

# Step 3: Build InnoSetup installer
Write-Host ""
Write-Host "[3/4] Building InnoSetup installer..." -ForegroundColor Green
if (Test-Path $InnoSetupPath) {
    try {
        & $InnoSetupPath "$scriptDir\FluentTB-Setup.iss"
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  InnoSetup installer created successfully" -ForegroundColor Green
        } else {
            Write-Host "  WARNING: InnoSetup compilation failed" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "  WARNING: InnoSetup compilation error: $($_.Exception.Message)" -ForegroundColor Yellow
    }
} else {
    Write-Host "  SKIPPED: InnoSetup not found at $InnoSetupPath" -ForegroundColor Yellow
    Write-Host "  Download from: https://jrsoftware.org/isdl.php" -ForegroundColor Cyan
}

# Step 4: Build WiX MSI installer
Write-Host ""
Write-Host "[4/4] Building WiX MSI installer..." -ForegroundColor Green
$candlePath = Join-Path $WixPath "candle.exe"
$lightPath = Join-Path $WixPath "light.exe"

if ((Test-Path $candlePath) -and (Test-Path $lightPath)) {
    try {
        # Compile WiX source
        Write-Host "  Compiling WiX object..." -ForegroundColor Yellow
        & $candlePath "$scriptDir\FluentTB.wxs" -out "$scriptDir\FluentTB.wixobj"
        
        if ($LASTEXITCODE -eq 0) {
            # Link to create MSI
            Write-Host "  Linking MSI..." -ForegroundColor Yellow
            & $lightPath "$scriptDir\FluentTB.wixobj" `
                -out "$outputDir\FluentTB.msi" `
                -ext WixUIExtension `
                -sval
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "  WiX MSI installer created successfully" -ForegroundColor Green
                # Clean up temp files
                Remove-Item "$scriptDir\FluentTB.wixobj" -ErrorAction SilentlyContinue
                Remove-Item "$scriptDir\FluentTB.wixpdb" -ErrorAction SilentlyContinue
            } else {
                Write-Host "  WARNING: WiX linking failed" -ForegroundColor Yellow
            }
        } else {
            Write-Host "  WARNING: WiX compilation failed" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "  WARNING: WiX build error: $($_.Exception.Message)" -ForegroundColor Yellow
    }
} else {
    Write-Host "  SKIPPED: WiX Toolset not found at $WixPath" -ForegroundColor Yellow
    Write-Host "  Download from: https://wixtoolset.org/" -ForegroundColor Cyan
}

# Summary
Write-Host ""
Write-Host "=== Build Summary ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Application binaries:" -ForegroundColor White
Write-Host "  $releasePath" -ForegroundColor Gray
Write-Host ""
Write-Host "Installers created in:" -ForegroundColor White
Write-Host "  $outputDir" -ForegroundColor Gray
Write-Host ""

# List created files
$installerFiles = @()
if (Test-Path "$outputDir\Install-FluentTB.ps1") { $installerFiles += "[OK] PowerShell installer (Install-FluentTB.ps1)" }
if (Test-Path "$outputDir\FluentTB-Setup-1.0.0.exe") { $installerFiles += "[OK] InnoSetup EXE installer" }
if (Test-Path "$outputDir\FluentTB.msi") { $installerFiles += "[OK] WiX MSI installer" }

if ($installerFiles.Count -gt 0) {
    Write-Host "Created installers:" -ForegroundColor Green
    foreach ($file in $installerFiles) {
        Write-Host "  $file" -ForegroundColor Green
    }
} else {
    Write-Host "No installers were created." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Build process complete!" -ForegroundColor Green
