[CmdletBinding()]
param([Parameter(Mandatory)][string]$SourceRoot, [Parameter(Mandatory)][string]$OutputPath)
$ErrorActionPreference = 'Stop'
$edition = (Get-Content -LiteralPath "$SourceRoot/edition.json" -Raw | ConvertFrom-Json).Edition
$name = switch ($edition) { 'Public' { 'FluentTB' } 'Dev' { 'FluentTB Dev-Edition' } default { throw 'Unknown license edition.' } }
$gpl = [IO.File]::ReadAllText((Join-Path $SourceRoot 'LICENSE'))
$componentGpl = [IO.File]::ReadAllText((Join-Path $SourceRoot 'licenses/FluentFlyout-GPL-3.0.txt'))
if ($gpl -ne $componentGpl -or !$gpl.Contains('Copyright (C) 2007 Free Software Foundation, Inc.') -or !$gpl.Contains('END OF TERMS AND CONDITIONS')) {
    throw 'The complete canonical GPL license must be present in both license files.'
}
$notices = [IO.File]::ReadAllText((Join-Path $SourceRoot 'THIRD_PARTY_NOTICES.md'))
$mit = [IO.File]::ReadAllText((Join-Path $SourceRoot 'licenses/FluentTB-MIT.txt'))
$text = @"
$name - License information

This application includes FluentFlyout-derived code and is provided under the GNU General Public License, version 3 or (at your option) any later version (GPL-3.0-or-later).
FluentTB: Shinob1Kai. FluentFlyout: Hugo Li (unchihugo) and contributors. RoundedTB: torchgm and contributors.
The original FluentTB MIT notice and component attributions are retained below. They do not replace the license of the combined application. No additional license restrictions are imposed by this installer.

$gpl

COMPONENT ATTRIBUTIONS

$notices

ORIGINAL FLUENTTB MIT NOTICE

$mit
"@
# ASCII RTF with signed Unicode escapes, including non-ASCII names and punctuation.
$rtf = [Text.StringBuilder]::new()
[void]$rtf.Append('{\rtf1\ansi\deff0{\fonttbl{\f0 Segoe UI;}}\viewkind4\uc1\pard\f0\fs18 ')
foreach ($character in $text.Replace("`r", '').ToCharArray()) {
    switch ([int]$character) {
        10 { [void]$rtf.Append('\par '); break }
        92 { [void]$rtf.Append('\\'); break }
        123 { [void]$rtf.Append('\{'); break }
        125 { [void]$rtf.Append('\}'); break }
        default {
            $code = [int]$character
            if ($code -gt 127) {
                if ($code -gt 32767) { $code -= 65536 }
                [void]$rtf.Append("\u${code}?")
            } else { [void]$rtf.Append($character) }
        }
    }
}
[void]$rtf.Append('}')
[IO.File]::WriteAllText([IO.Path]::GetFullPath($OutputPath), $rtf.ToString(), [Text.Encoding]::ASCII)
Write-Output ([IO.Path]::GetFullPath($OutputPath))
