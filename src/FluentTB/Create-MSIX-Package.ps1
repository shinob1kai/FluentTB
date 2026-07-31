<#
.SYNOPSIS
    Creates MSIX package for Microsoft Store submission
.DESCRIPTION
    Packages FluentTB as MSIX for Windows Store
#>

$ErrorActionPreference = "Stop"

Write-Host "=== FluentTB MSIX Package Creator ===" -ForegroundColor Cyan
Write-Host ""

# Paths
$appVersion = "2026.3.1.0"
$publisherName = "CN=Shinob1Kai"
$packageName = "FluentTB"
$buildDir = "bin\Release"
$outputDir = "..\..\Installer\Output\MSIX"

# Check if build exists
if (-not (Test-Path "$buildDir\FluentTB.exe")) {
    Write-Host "ERROR: Build not found at $buildDir" -ForegroundColor Red
    Write-Host "Please build first: dotnet build FluentTB.csproj -c Release" -ForegroundColor Yellow
    exit 1
}

# Create output directory
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

Write-Host "[1/5] Creating package manifest..." -ForegroundColor Green

# Create AppxManifest.xml
$manifestContent = @"
<?xml version="1.0" encoding="utf-8"?>
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
         xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
         xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
         IgnorableNamespaces="uap rescap">
  
  <Identity Name="Shinob1Kai.FluentTB"
            Version="$appVersion"
            Publisher="$publisherName"
            ProcessorArchitecture="x64" />
  
  <Properties>
    <DisplayName>FluentTB</DisplayName>
    <PublisherDisplayName>Shinob1Kai</PublisherDisplayName>
    <Logo>Assets\StoreLogo.png</Logo>
    <Description>Customize your Windows 11 taskbar with rounded corners and margins</Description>
  </Properties>
  
  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.19041.0" MaxVersionTested="10.0.22621.0" />
  </Dependencies>
  
  <Resources>
    <Resource Language="en-us" />
    <Resource Language="de-de" />
  </Resources>
  
  <Applications>
    <Application Id="FluentTB"
                 Executable="FluentTB.exe"
                 EntryPoint="Windows.FullTrustApplication">
      <uap:VisualElements DisplayName="FluentTB"
                          Description="Customize your Windows 11 taskbar"
                          BackgroundColor="transparent"
                          Square150x150Logo="Assets\Square150x150Logo.png"
                          Square44x44Logo="Assets\Square44x44Logo.png">
        <uap:DefaultTile Wide310x150Logo="Assets\Wide310x150Logo.png"
                         Square310x310Logo="Assets\LargeTile.png"
                         Square71x71Logo="Assets\SmallTile.png">
        </uap:DefaultTile>
        <uap:SplashScreen Image="Assets\SplashScreen.png" />
      </uap:VisualElements>
      <Extensions>
        <uap:Extension Category="windows.protocol">
          <uap:Protocol Name="fluenttb" />
        </uap:Extension>
      </Extensions>
    </Application>
  </Applications>
  
  <Capabilities>
    <rescap:Capability Name="runFullTrust" />
  </Capabilities>
</Package>
"@

# Create package directory
$packageDir = Join-Path $outputDir "PackageFiles"
New-Item -ItemType Directory -Path $packageDir -Force | Out-Null
New-Item -ItemType Directory -Path "$packageDir\Assets" -Force | Out-Null

# Save manifest
Set-Content -Path "$packageDir\AppxManifest.xml" -Value $manifestContent -Encoding UTF8

Write-Host "[2/5] Copying application files..." -ForegroundColor Green

# Copy all files from Release build
Copy-Item -Path "$buildDir\*" -Destination $packageDir -Recurse -Force -Exclude "*.pdb","*.xml"

Write-Host "[3/5] Creating asset images..." -ForegroundColor Green

# Note: You need to create proper Store assets
# For now, we'll use the app icon
if (Test-Path "res\FluentTB.ico") {
    # Extract ICO to PNG (requires ImageMagick or similar)
    # For now, just copy the ICO
    Write-Host "  Note: Store assets should be created separately" -ForegroundColor Yellow
    Write-Host "  Required sizes:" -ForegroundColor Gray
    Write-Host "    - Square44x44Logo.png (44x44)" -ForegroundColor Gray
    Write-Host "    - Square150x150Logo.png (150x150)" -ForegroundColor Gray
    Write-Host "    - Wide310x150Logo.png (310x150)" -ForegroundColor Gray
    Write-Host "    - LargeTile.png (310x310)" -ForegroundColor Gray
    Write-Host "    - StoreLogo.png (50x50)" -ForegroundColor Gray
    Write-Host "    - SplashScreen.png (620x300)" -ForegroundColor Gray
}

Write-Host "[4/5] Checking for Windows SDK..." -ForegroundColor Green

# Find makeappx.exe
$sdkPaths = @(
    "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\makeappx.exe",
    "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22000.0\x64\makeappx.exe",
    "C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64\makeappx.exe"
)

$makeappx = $null
foreach ($path in $sdkPaths) {
    if (Test-Path $path) {
        $makeappx = $path
        break
    }
}

if (-not $makeappx) {
    Write-Host ""
    Write-Host "WARNING: Windows SDK not found!" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Package files are ready at:" -ForegroundColor Cyan
    Write-Host "  $packageDir" -ForegroundColor White
    Write-Host ""
    Write-Host "To create MSIX package:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Option 1: Install Windows SDK" -ForegroundColor Cyan
    Write-Host "  Download: https://developer.microsoft.com/windows/downloads/windows-sdk" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Option 2: Use Visual Studio" -ForegroundColor Cyan
    Write-Host "  1. Open project in Visual Studio 2022" -ForegroundColor Gray
    Write-Host "  2. Right-click project > Publish > Microsoft Store" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Option 3: Partner Center Upload" -ForegroundColor Cyan
    Write-Host "  1. Create ZIP of PackageFiles folder" -ForegroundColor Gray
    Write-Host "  2. Upload directly to Partner Center" -ForegroundColor Gray
    Write-Host ""
    exit 0
}

Write-Host "  Found: $makeappx" -ForegroundColor Gray

Write-Host "[5/5] Creating MSIX package..." -ForegroundColor Green

$msixPath = Join-Path $outputDir "FluentTB_$appVersion`_x64.msix"

# Create MSIX
& $makeappx pack /d $packageDir /p $msixPath /l

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "=== MSIX Package Created Successfully ===" -ForegroundColor Green
    Write-Host ""
    Write-Host "Package Location:" -ForegroundColor Cyan
    Write-Host "  $msixPath" -ForegroundColor White
    Write-Host ""
    Write-Host "Package Size:" -ForegroundColor Cyan
    $size = (Get-Item $msixPath).Length / 1MB
    Write-Host "  $([math]::Round($size, 2)) MB" -ForegroundColor White
    Write-Host ""
    Write-Host "Next Steps:" -ForegroundColor Yellow
    Write-Host "  1. Test install: Add-AppxPackage $msixPath" -ForegroundColor Gray
    Write-Host "  2. Upload to Microsoft Partner Center" -ForegroundColor Gray
    Write-Host "  3. Fill in Store listing details" -ForegroundColor Gray
    Write-Host ""
    Write-Host "IMPORTANT: Create Store assets before submission!" -ForegroundColor Red
    Write-Host "  See: MICROSOFT-STORE-GUIDE.md" -ForegroundColor Gray
} else {
    Write-Host ""
    Write-Host "ERROR: Failed to create MSIX package" -ForegroundColor Red
    Write-Host "Exit code: $LASTEXITCODE" -ForegroundColor Yellow
}

Write-Host ""
