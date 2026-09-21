[CmdletBinding()]
param([Parameter(Mandatory)][string]$Version, [Parameter(Mandatory)][string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
[xml]$props = Get-Content -LiteralPath "$repo/Directory.Build.props"
if ($Version -ne $props.Project.PropertyGroup.Version) { throw 'Update the source version before building.' }
$edition = (Get-Content "$repo/edition.json" -Raw | ConvertFrom-Json).Edition
$out = [IO.Path]::GetFullPath($OutputDirectory)
if ($out -eq $repo -or $out.StartsWith($repo + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Release outputs must be outside Source.' }
$target = Join-Path $out 'src'
$stage = Join-Path $out ('.source-' + [guid]::NewGuid())
New-Item -ItemType Directory -Path $stage -Force | Out-Null
try {
    # Traverse only source directories; never descend into repositories or generated outputs.
    function Copy-SourceTree([string]$directory) {
        foreach ($item in Get-ChildItem -LiteralPath $directory -Force) {
            if ($item.Name -in @('.git','.vs','.kiro','bin','obj','packages','Output','Outputs','versions')) { continue }
            if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Source link is unsupported: $($item.FullName)" }
            if ($item.PSIsContainer) { Copy-SourceTree $item.FullName; continue }
            if ($item.Name -eq 'source-snapshot.json' -or $item.Extension -in @('.log','.user','.suo','.tmp','.bak')) { continue }
            $relative = [IO.Path]::GetRelativePath($repo, $item.FullName)
            $destination = Join-Path $stage $relative
            New-Item -ItemType Directory -Path (Split-Path $destination) -Force | Out-Null
            Copy-Item -LiteralPath $item.FullName -Destination $destination
        }
    }
    Copy-SourceTree $repo
    $files = @(Get-ChildItem -LiteralPath $stage -File -Recurse | ForEach-Object {
        [ordered]@{ Path = [IO.Path]::GetRelativePath($stage, $_.FullName).Replace('\','/'); SHA256 = (Get-FileHash -LiteralPath $_.FullName).Hash }
    } | Sort-Object { $_.Path })
    $manifest = [ordered]@{ Version = $Version; Edition = $edition; Files = $files }
    if (Test-Path -LiteralPath $target) {
        $old = Get-Content "$target/source-snapshot.json" -Raw | ConvertFrom-Json
        if ($old.Version -ne $Version -or $old.Edition -ne $edition -or
            ($old.Files | ConvertTo-Json -Depth 5 -Compress) -ne ($files | ConvertTo-Json -Depth 5 -Compress)) {
            throw 'This version already has different source. Increment the version; archived source cannot be replaced.'
        }
        foreach ($file in $files) {
            if ((Get-FileHash -LiteralPath (Join-Path $target $file.Path)).Hash -ne $file.SHA256) { throw "Archived source changed: $($file.Path)" }
        }
    } else {
        $manifest | ConvertTo-Json -Depth 5 | Set-Content "$stage/source-snapshot.json" -Encoding utf8
        $resolvedTarget = [IO.Path]::GetFullPath($target)
        if (!$resolvedTarget.StartsWith($out + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe snapshot target.' }
        Move-Item -LiteralPath $stage -Destination $resolvedTarget
    }
    Write-Output $target
} finally {
    $resolvedStage = [IO.Path]::GetFullPath($stage)
    if (!$resolvedStage.StartsWith($out + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe staging cleanup.' }
    if (Test-Path -LiteralPath $resolvedStage) { Remove-Item -LiteralPath $resolvedStage -Recurse -Force }
}
