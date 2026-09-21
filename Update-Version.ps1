param([Parameter(Mandatory)][ValidatePattern('^20\d{2}\.\d+\.\d+\.\d+$')][string]$NewVersion)
$ErrorActionPreference = 'Stop'
$propsPath = Join-Path $PSScriptRoot 'Directory.Build.props'
[xml]$props = Get-Content $propsPath
$props.Project.PropertyGroup.Version = $NewVersion
$props.Project.PropertyGroup.AssemblyVersion = $NewVersion
$props.Project.PropertyGroup.FileVersion = $NewVersion
$props.Save($propsPath)
$assembly = Join-Path $PSScriptRoot 'src/FluentTB/Properties/AssemblyInfo.cs'
$content = [IO.File]::ReadAllText($assembly)
$content = [regex]::Replace($content, '(Assembly(?:File)?Version\(")[0-9.]+("\))', { param($m) $m.Groups[1].Value + $NewVersion + $m.Groups[2].Value })
[IO.File]::WriteAllText($assembly, $content)
$versionPath = Join-Path $PSScriptRoot 'VERSION.txt'
$content = [IO.File]::ReadAllText($versionPath)
$content = $content -replace '(?m)^FluentTB Version .*$', "FluentTB Version $NewVersion" -replace '(?m)^Current Version: .*$', "Current Version: $NewVersion"
[IO.File]::WriteAllText($versionPath, $content)
Write-Host "Version set to $NewVersion. Build installers using src/Installer/Build-Editions.ps1."
