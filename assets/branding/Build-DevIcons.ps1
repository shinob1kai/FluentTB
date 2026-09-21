$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$source = [Drawing.Image]::FromFile((Join-Path $PSScriptRoot 'dev/FluentDev.png'))
function New-IconBitmap([int]$width, [int]$height) {
    $bitmap = [Drawing.Bitmap]::new($width, $height)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $scale = [Math]::Min($width / $source.Width, $height / $source.Height)
        $drawWidth = [int][Math]::Round($source.Width * $scale)
        $drawHeight = [int][Math]::Round($source.Height * $scale)
        $graphics.DrawImage($source, [Drawing.Rectangle]::new(($width-$drawWidth)/2, ($height-$drawHeight)/2, $drawWidth, $drawHeight))
    } finally { $graphics.Dispose() }
    return $bitmap
}
try {
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
        foreach ($relative in @('src/FluentTB.Desktop/Resources/FluentTB.Dev.ico')) {
            [IO.File]::WriteAllBytes((Join-Path $repoRoot $relative), $output.ToArray())
        }
    } finally { $writer.Dispose(); $output.Dispose() }
} finally { $source.Dispose() }

Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'dev/HeadBannerDev.png') -Destination (Join-Path $repoRoot 'src/FluentTB.Desktop/Resources/HeadBannerDev.png')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'dev/FluentDev.png') -Destination (Join-Path $repoRoot 'src/FluentTB.Desktop/Resources/FluentDev.png')
