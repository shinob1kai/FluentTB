<#
.SYNOPSIS
    Erstellt Store Assets aus FluentTB.ico
.DESCRIPTION
    Konvertiert das FluentTB Icon zu den benötigten PNG-Formaten für Microsoft Store
#>

$ErrorActionPreference = "Stop"

Write-Host "`n=== FluentTB Store Assets Creator ===" -ForegroundColor Cyan
Write-Host ""

# Pfade
$icoPath = "src\FluentTB\res\FluentTB.ico"
$assetsDir = "src\Installer\Output\MSIX\PackageFiles\Assets"

# Prüfen ob ICO existiert
if (-not (Test-Path $icoPath)) {
    Write-Host "FEHLER: FluentTB.ico nicht gefunden!" -ForegroundColor Red
    Write-Host "Erwartet: $icoPath" -ForegroundColor Yellow
    exit 1
}

# Assets-Verzeichnis erstellen
New-Item -ItemType Directory -Path $assetsDir -Force | Out-Null

Write-Host "[1/3] ICO-Datei gefunden" -ForegroundColor Green
Write-Host "      Quelle: $icoPath" -ForegroundColor Gray

# Benötigte Größen
$sizes = @{
    "Square44x44Logo.png" = 44
    "Square150x150Logo.png" = 150
    "Wide310x150Logo.png" = @(310, 150)
    "StoreLogo.png" = 50
    "LargeTile.png" = 310
    "SmallTile.png" = 71
    "SplashScreen.png" = @(620, 300)
}

Write-Host ""
Write-Host "[2/3] Erstelle PNG Assets..." -ForegroundColor Yellow
Write-Host ""

# .NET Assemblies laden
Add-Type -AssemblyName System.Drawing

try {
    # Icon laden
    $icon = [System.Drawing.Icon]::new($icoPath)
    $bitmap = $icon.ToBitmap()
    
    foreach ($asset in $sizes.GetEnumerator()) {
        $fileName = $asset.Key
        $size = $asset.Value
        
        $destPath = Join-Path $assetsDir $fileName
        
        if ($size -is [array]) {
            # Wide/Splash - rechteckig
            $width = $size[0]
            $height = $size[1]
        } else {
            # Quadratisch
            $width = $size
            $height = $size
        }
        
        # Neues Bitmap mit gewünschter Größe
        $resized = New-Object System.Drawing.Bitmap($width, $height)
        $graphics = [System.Drawing.Graphics]::FromImage($resized)
        
        # High-Quality Rendering
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        
        # Transparenter Hintergrund
        $graphics.Clear([System.Drawing.Color]::Transparent)
        
        # Zentriert zeichnen
        if ($size -is [array]) {
            # Für rechteckige: Icon zentriert, aber kleinere Größe
            $iconSize = [Math]::Min($width, $height) * 0.8
            $x = ($width - $iconSize) / 2
            $y = ($height - $iconSize) / 2
            $graphics.DrawImage($bitmap, $x, $y, $iconSize, $iconSize)
        } else {
            # Für quadratische: Voll ausfüllen
            $graphics.DrawImage($bitmap, 0, 0, $width, $height)
        }
        
        # Als PNG speichern
        $resized.Save($destPath, [System.Drawing.Imaging.ImageFormat]::Png)
        
        Write-Host "  ✅ $fileName ($width x $height)" -ForegroundColor Green
        
        $graphics.Dispose()
        $resized.Dispose()
    }
    
    $bitmap.Dispose()
    $icon.Dispose()
    
    Write-Host ""
    Write-Host "[3/3] Neues Store-Package ZIP erstellen..." -ForegroundColor Yellow
    
    # Altes ZIP löschen
    $zipPath = "src\Installer\Output\MSIX\FluentTB-Store-Package.zip"
    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }
    
    # Neues ZIP mit Assets
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory(
        "src\Installer\Output\MSIX\PackageFiles",
        $zipPath
    )
    
    $zipSize = (Get-Item $zipPath).Length / 1MB
    
    Write-Host ""
    Write-Host "=== ERFOLGREICH! ===" -ForegroundColor Green
    Write-Host ""
    Write-Host "Store Package erstellt:" -ForegroundColor White
    Write-Host "  Datei: $zipPath" -ForegroundColor Cyan
    Write-Host "  Groesse: $([math]::Round($zipSize, 2)) MB" -ForegroundColor White
    Write-Host ""
    Write-Host "Assets erstellt:" -ForegroundColor White
    Write-Host "  Speicherort: $assetsDir" -ForegroundColor Cyan
    Write-Host "  Anzahl: $($sizes.Count) PNG-Dateien" -ForegroundColor White
    Write-Host ""
    Write-Host "BEREIT fuer Partner Center Upload!" -ForegroundColor Green
    Write-Host "Upload unter: https://partner.microsoft.com/dashboard" -ForegroundColor Cyan
    Write-Host ""
    
} catch {
    Write-Host ""
    Write-Host "FEHLER bei Asset-Erstellung:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Alternative Methode:" -ForegroundColor Cyan
    Write-Host "  1. https://www.pwabuilder.com/imageGenerator" -ForegroundColor White
    Write-Host "  2. Upload: $icoPath" -ForegroundColor White
    Write-Host "  3. Platform: Windows auswaehlen" -ForegroundColor White
    Write-Host "  4. Download + kopieren nach: $assetsDir" -ForegroundColor White
    Write-Host ""
    exit 1
}
