$ErrorActionPreference = 'Stop'
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'FluentTB-original.png') -Destination (Join-Path $PSScriptRoot 'FluentTB-master.png')
Add-Type -AssemblyName System.Drawing
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$source = [Drawing.Image]::FromFile((Join-Path $PSScriptRoot 'FluentTB-master.png'))
function New-IconBitmap([int]$width, [int]$height) {
    $bitmap = [Drawing.Bitmap]::new($width, $height)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $side = [Math]::Min($width, $height)
        $graphics.DrawImage($source, [Drawing.Rectangle]::new(($width-$side)/2, ($height-$side)/2, $side, $side))
    } finally { $graphics.Dispose() }
    return $bitmap
}
try {
    $pngs = @{
        'src/FluentTB/res/FluentTB.png' = @(150,150)
        'src/FluentTB/res/Square150x150Logo.png' = @(150,150)
        'src/FluentTB/res/Square44x44Logo.png' = @(44,44)
        'src/FluentTB/res/StoreLogo.png' = @(50,50)
        'src/FluentTB/res/Wide310x150Logo.png' = @(310,150)
    }
    foreach ($entry in $pngs.GetEnumerator()) {
        $bitmap = New-IconBitmap $entry.Value[0] $entry.Value[1]
        try { $bitmap.Save((Join-Path $repoRoot $entry.Key), [Drawing.Imaging.ImageFormat]::Png) }
        finally { $bitmap.Dispose() }
    }
    # PNG-compressed, multi-resolution Windows icon; preserve alpha in every frame.
    $sizes = @(16,24,32,48,64,128,256)
    $frames = foreach ($size in $sizes) {
        $bitmap = New-IconBitmap $size $size
        $stream = [IO.MemoryStream]::new()
        try { $bitmap.Save($stream, [Drawing.Imaging.ImageFormat]::Png); ,$stream.ToArray() }
        finally { $bitmap.Dispose(); $stream.Dispose() }
    }
    $output = [IO.MemoryStream]::new()
    $writer = [IO.BinaryWriter]::new($output)
    try {
        $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$sizes.Count)
        $offset = 6 + 16 * $sizes.Count
        for ($index = 0; $index -lt $sizes.Count; $index++) {
            $dimension = if ($sizes[$index] -eq 256) { 0 } else { $sizes[$index] }
            $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
            $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]32)
            $writer.Write([uint32]$frames[$index].Length); $writer.Write([uint32]$offset)
            $offset += $frames[$index].Length
        }
        foreach ($frame in $frames) { $writer.Write([byte[]]$frame) }
        foreach ($relative in @('src/FluentTB/res/FluentTB.ico','src/FluentTB/res/FluentTBSetup.ico','src/FluentTB.Desktop/Resources/FluentFlyout2.ico')) {
            [IO.File]::WriteAllBytes((Join-Path $repoRoot $relative), $output.ToArray())
        }
    } finally { $writer.Dispose(); $output.Dispose() }
} finally { $source.Dispose() }

# Original monochrome tray artwork, independent from the coloured app logo.
foreach ($theme in @('Dark', 'Light')) {
    $trayPath = Join-Path $PSScriptRoot "tray/Tray$theme.png"
    $trayImage = [Drawing.Image]::FromFile($trayPath)
    $bytes = [IO.File]::ReadAllBytes($trayPath)
    $output = [IO.MemoryStream]::new()
    $writer = [IO.BinaryWriter]::new($output)
    try {
        if ($trayImage.Width -gt 256 -or $trayImage.Height -gt 256) { throw 'Tray artwork exceeds ICO frame size.' }
        $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]1)
        $writer.Write([byte]($trayImage.Width % 256)); $writer.Write([byte]($trayImage.Height % 256))
        $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]32)
        $writer.Write([uint32]$bytes.Length); $writer.Write([uint32]22); $writer.Write($bytes)
        [IO.File]::WriteAllBytes((Join-Path $repoRoot "src/FluentTB/res/Tray$theme.ico"), $output.ToArray())
        Copy-Item -LiteralPath $trayPath -Destination (Join-Path $repoRoot "src/FluentTB/res/Tray$theme.png")
        Copy-Item -LiteralPath $trayPath -Destination (Join-Path $repoRoot "src/FluentTB.Desktop/Resources/TrayIcons/Tray$theme.png")
    } finally { $writer.Dispose(); $output.Dispose(); $trayImage.Dispose() }
}
