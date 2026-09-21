<#
.SYNOPSIS
Build current FluentTB release editions. Signing is a separate publisher step.
#>
param([string]$Version = '', [ValidateSet('All','Public','Dev','Private')][string]$Edition = 'All')
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Build-Editions.ps1') -Version $Version -Edition $Edition
