<#
.SYNOPSIS
    Aktualisiert Store Assets mit benutzerdefinierten Icon und SplashScreen
.DESCRIPTION
    Verwendet die neuen FluentTB.png und SplashScreen.png für das Store Package
#>

$ErrorActionPreference = "Stop"

Write-Host "`n=== FluentTB Store Assets Update ===" -ForegroundColor Cyan
Write-Host ""

# Pfade
$newIcon = "src\FluentTB\res\FluentTB.png"
$newSplash = "src\FluentTB\res\SplashScreen.png"
$assetsDir = "src\Installer\Output\MSIX\PackageFiles\Assets"

# Prüfen ob neue Dateien existieren
if (-not (Test-Path $newIcon)) {
    Write-Host "FEHLER: FluentTB.png nicht gefunden!" -ForegroundColor Red
    Write-Host "Erwartet: $newIcon" -ForegroundColor Yellow
    exit 1
}

if (-not (Test-Path $newSplash)) {
    Write-Host "FEHLER: SplashScreen.png nicht gefunden!" -ForegroundColor Red
    Write-Host "Erwartet: $newSplash" -ForegroundColor Yellow
    exit 1
}

Write-Host "[1/4] Neue Assets gefunden" -ForegroundColor Green
Write-Host "      Icon: $newIcon" -ForegroundColor Gray
Write-Host "      Splash: $newSplash" -ForegroundColor Gray
Write-Host ""

# .NET Assemblies laden
Add-Type -AssemblyName System.Drawing

try {
    # Originales PNG laden
    $sourceImage = [System.Drawing.Image]::FromFile((Resolve-Path $newIcon).Path)
    
    Write-Host "[2/4] Erstelle Store Assets aus neuem Icon..." -ForegroundColor Yellow
    Write-Host ""
    
    # Quadratische Assets
    $squareAssets = @{
        "Square44x44Logo.png" = 44
        "Square150x150Logo.png" = 150
        "StoreLogo.png" = 50
        "LargeTile.png" = 310
        "SmallTile.png" = 71
    }
    
    foreach ($asset in $squareAssets.GetEnumerator()) {
        $fileName = $asset.Key
        $size = $asset.Value
        $destPath = Join-Path $assetsDir $fileName
        
        # Neues Bitmap erstellen
        $resized = New-Object System.Drawing.Bitmap($size, $size)
        $graphics = [System.Drawing.Graphics]::FromImage($resized)
        
        # High-Quality Rendering
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        
        # Transparenter Hintergrund
        $graphics.Clear([System.Drawing.Color]::Transparent)
        
        # Icon skalieren und zeichnen
        $graphics.DrawImage($sourceImage, 0, 0, $size, $size)
        
        # Als PNG speichern
        $resized.Save($destPath, [System.Drawing.Imaging.ImageFormat]::Png)
        
        Write-Host "  ✅ $fileName ($size x $size)" -ForegroundColor Green
        
        $graphics.Dispose()
        $resized.Dispose()
    }
    
    $sourceImage.Dispose()
    
    # Wide Logo (310x150) - breites Format
    Write-Host ""
    Write-Host "  Erstelle Wide310x150Logo.png..." -ForegroundColor Gray
    
    $sourceImage = [System.Drawing.Image]::FromFile((Resolve-Path $newIcon).Path)
    $wideWidth = 310
    $wideHeight = 150
    $wideBitmap = New-Object System.Drawing.Bitmap($wideWidth, $wideHeight)
    $wideGraphics = [System.Drawing.Graphics]::FromImage($wideBitmap)
    
    $wideGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $wideGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $wideGraphics.Clear([System.Drawing.Color]::Transparent)
    
    # Icon zentriert, kleinere Größe für Wide-Format
    $iconSize = [Math]::Min($wideWidth, $wideHeight) * 0.8
    $x = ($wideWidth - $iconSize) / 2
    $y = ($wideHeight - $iconSize) / 2
    $wideGraphics.DrawImage($sourceImage, $x, $y, $iconSize, $iconSize)
    
    $widePath = Join-Path $assetsDir "Wide310x150Logo.png"
    $wideBitmap.Save($widePath, [System.Drawing.Imaging.ImageFormat]::Png)
    
    Write-Host "  ✅ Wide310x150Logo.png (310 x 150)" -ForegroundColor Green
    
    $wideGraphics.Dispose()
    $wideBitmap.Dispose()
    $sourceImage.Dispose()
    
    Write-Host ""
    Write-Host "[3/4] Kopiere benutzerdefinierten SplashScreen..." -ForegroundColor Yellow
    Write-Host ""
    
    # SplashScreen direkt kopieren oder resizen auf 620x300
    $sourceSplash = [System.Drawing.Image]::FromFile((Resolve-Path $newSplash).Path)
    
    # Prüfe ob bereits richtige Größe
    if ($sourceSplash.Width -eq 620 -and $sourceSplash.Height -eq 300) {
        # Direkt kopieren
        Copy-Item $newSplash -Destination (Join-Path $assetsDir "SplashScreen.png") -Force
        Write-Host "  ✅ SplashScreen.png (620 x 300) - Original verwendet" -ForegroundColor Green
    } else {
        # Auf 620x300 skalieren
        $splashBitmap = New-Object System.Drawing.Bitmap(620, 300)
        $splashGraphics = [System.Drawing.Graphics]::FromImage($splashBitmap)
        
        $splashGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $splashGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $splashGraphics.Clear([System.Drawing.Color]::Transparent)
        
        $splashGraphics.DrawImage($sourceSplash, 0, 0, 620, 300)
        
        $splashPath = Join-Path $assetsDir "SplashScreen.png"
        $splashBitmap.Save($splashPath, [System.Drawing.Imaging.ImageFormat]::Png)
        
        Write-Host "  ✅ SplashScreen.png (620 x 300) - Skaliert" -ForegroundColor Green
        
        $splashGraphics.Dispose()
        $splashBitmap.Dispose()
    }
    
    $sourceSplash.Dispose()
    
    Write-Host ""
    Write-Host "[4/4] Neues Store-Package ZIP erstellen..." -ForegroundColor Yellow
    Write-Host ""
    
    # Altes ZIP löschen
    $zipPath = "src\Installer\Output\MSIX\FluentTB-Store-Package.zip"
    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }
    
    # Neues ZIP mit aktualisierten Assets
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory(
        "src\Installer\Output\MSIX\PackageFiles",
        $zipPath
    )
    
    $zipSize = (Get-Item $zipPath).Length / 1MB
    
    Write-Host "=== ERFOLGREICH! ===" -ForegroundColor Green
    Write-Host ""
    Write-Host "Store Package aktualisiert:" -ForegroundColor White
    Write-Host "  Datei: $zipPath" -ForegroundColor Cyan
    Write-Host "  Groesse: $([math]::Round($zipSize, 2)) MB" -ForegroundColor White
    Write-Host ""
    Write-Host "Neue Assets verwendet:" -ForegroundColor White
    Write-Host "  Icon: $newIcon" -ForegroundColor Cyan
    Write-Host "  SplashScreen: $newSplash" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Erstellt: 7 PNG-Dateien" -ForegroundColor White
    Write-Host "  - Square44x44Logo.png" -ForegroundColor Gray
    Write-Host "  - Square150x150Logo.png" -ForegroundColor Gray
    Write-Host "  - Wide310x150Logo.png" -ForegroundColor Gray
    Write-Host "  - StoreLogo.png" -ForegroundColor Gray
    Write-Host "  - LargeTile.png" -ForegroundColor Gray
    Write-Host "  - SmallTile.png" -ForegroundColor Gray
    Write-Host "  - SplashScreen.png" -ForegroundColor Gray
    Write-Host ""
    Write-Host "BEREIT fuer Partner Center Upload!" -ForegroundColor Green
    Write-Host ""
    
} catch {
    Write-Host ""
    Write-Host "FEHLER bei Asset-Erstellung:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Yellow
    Write-Host ""
    exit 1
}
