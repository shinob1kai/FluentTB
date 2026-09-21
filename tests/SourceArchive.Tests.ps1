$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$fixture = Join-Path $repo ('src/Installer/obj/archive-test-' + [guid]::NewGuid())
$source = Join-Path $fixture 'Source'
$outputs = Join-Path $fixture 'Outputs'
New-Item -ItemType Directory -Path "$source/src/Installer","$source/bin","$source/.git" -Force | Out-Null
Copy-Item "$repo/src/Installer/Export-ReleaseSource.ps1" "$source/src/Installer/Export-ReleaseSource.ps1"
'<Project><PropertyGroup><Version>2026.3.8.0</Version></PropertyGroup></Project>' | Set-Content "$source/Directory.Build.props"
'{"Edition":"Dev"}' | Set-Content "$source/edition.json"
'original source' | Set-Content "$source/example.cs"
'generated' | Set-Content "$source/bin/generated.cs"
'private metadata' | Set-Content "$source/.git/config"
$export = "$source/src/Installer/Export-ReleaseSource.ps1"
$snapshot = & $export -Version '2026.3.8.0' -OutputDirectory "$outputs/2026.3.8.0"
if (!(Test-Path "$snapshot/example.cs") -or (Test-Path "$snapshot/bin") -or (Test-Path "$snapshot/.git")) { throw 'Source export filtering failed.' }
$manifest = Get-Content "$snapshot/source-snapshot.json" -Raw | ConvertFrom-Json
if ($manifest.Edition -ne 'Dev') { throw 'Dev snapshot lost edition identity.' }
& $export -Version '2026.3.8.0' -OutputDirectory "$outputs/2026.3.8.0" | Out-Null
'changed source' | Set-Content "$source/example.cs"
$rejected = $false
try { & $export -Version '2026.3.8.0' -OutputDirectory "$outputs/2026.3.8.0" | Out-Null } catch { $rejected = $_ -match 'different source' }
if (!$rejected) { throw 'Changed source overwrote an existing version.' }
if ((Get-Content "$snapshot/example.cs") -ne 'original source') { throw 'Old source was modified.' }
'<Project><PropertyGroup><Version>2026.3.9.0</Version></PropertyGroup></Project>' | Set-Content "$source/Directory.Build.props"
$next = & $export -Version '2026.3.9.0' -OutputDirectory "$outputs/2026.3.9.0"
if ((Get-Content "$next/example.cs") -ne 'changed source') { throw 'New version lacks its own source.' }
'tampered' | Set-Content "$next/example.cs"
$rejected = $false
try { & $export -Version '2026.3.9.0' -OutputDirectory "$outputs/2026.3.9.0" | Out-Null } catch { $rejected = $_ -match 'Archived source changed' }
if (!$rejected) { throw 'Archive tampering was not detected.' }
Write-Output 'PASS: edition metadata, source filtering, idempotence, overwrite prevention, independent versions and archive tamper detection.'
