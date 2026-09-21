param([Parameter(Mandatory)][string]$Version)
& "$PSScriptRoot/Export-ReleaseSource.ps1" -Version $Version -OutputDirectory (Join-Path ([IO.Path]::GetFullPath("$PSScriptRoot/../../..")) "Outputs/$Version")
