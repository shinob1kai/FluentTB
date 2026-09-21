param([string]$Version = '')
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
if (!$Version) { [xml]$props = Get-Content "$repo/Directory.Build.props"; $Version = $props.Project.PropertyGroup.Version }
$base = Join-Path (Split-Path $repo -Parent) "Outputs/$Version"
$selectedEdition = (Get-Content "$repo/edition.json" -Raw | ConvertFrom-Json).Edition
$installer = New-Object -ComObject WindowsInstaller.Installer
$upgradeCodes = @()
foreach ($edition in @($selectedEdition)) {
    $publish = Join-Path $repo "src/Installer/obj/v$Version/$edition/publish"
    $msiPath = Join-Path $base "FluentTB-$edition-$Version-x64.msi"
    if (!(Test-Path $msiPath)) { throw "Missing $edition MSI" }
    foreach ($file in 'FluentTB.exe','FluentTB.dll','FluentTB.Taskbar.dll') {
        if ((Get-Item "$publish/$file").VersionInfo.FileVersion -ne $Version) { throw "$edition/$file has incorrect version" }
    }
    if (Get-ChildItem $publish -Filter '*.pdb') { throw 'Debug symbols in published app' }
    $deps = [IO.File]::ReadAllText("$publish/FluentTB.deps.json")
    $hasAudio = $deps.Contains('NAudio/')
    if ($hasAudio -ne ($edition -eq 'Dev')) { throw "Incorrect audio dependencies in $edition" }
    if (($deps.Contains('Dubya.WindowsMediaController/')) -ne ($edition -eq 'Dev')) { throw "Incorrect media dependencies in $edition" }
    foreach ($notice in 'LICENSE','THIRD_PARTY_NOTICES.md','licenses/FluentFlyout-GPL-3.0.txt','licenses/FluentTB-MIT.txt') {
        if (!(Test-Path "$publish/$notice")) { throw "Missing license: $notice" }
    }
    $db = $installer.OpenDatabase($msiPath.Replace('/', '\'), 0)
    $view = $db.OpenView('SELECT `Property`, `Value` FROM `Property`')
    $view.Execute()
    $properties = @{}
    while ($record = $view.Fetch()) { $properties[$record.StringData(1)] = $record.StringData(2) }
    $view.Close()
    $expected = if ($edition -eq 'Dev') { 'FluentTB Dev' } else { 'FluentTB' }
    if ($properties.ProductName -ne $expected) { throw 'Incorrect MSI identity' }
    $expectedUpgrade = if ($edition -eq 'Dev') { '{3473725A-0928-478A-9610-32F74029B2DB}' } else { '{A1B2C3D4-E5F6-4890-ABCD-1234567890AB}' }
    if ($properties.UpgradeCode -ne $expectedUpgrade) { throw 'MSI upgrade identity changed; existing installations would not be upgraded' }
    $upgradeCodes += $properties.UpgradeCode
    $asset = 'Resources/FluentTBDemoFloatingTaskbar.png'
    $original = Join-Path $repo "src/FluentTB.Desktop/$asset"
    if (!(Test-Path "$publish/$asset") -or (Get-FileHash "$publish/$asset").Hash -ne (Get-FileHash $original).Hash) { throw "Missing or altered onboarding screenshot in $edition" }
    $upgradeView = $db.OpenView('SELECT `UpgradeCode`, `VersionMax`, `Attributes`, `ActionProperty` FROM `Upgrade`')
    $upgradeView.Execute()
    $upgradeRule = $false
    while ($row = $upgradeView.Fetch()) {
        if ($row.StringData(1) -eq $expectedUpgrade -and $row.StringData(4) -eq 'WIX_UPGRADE_DETECTED') {
            if (($row.IntegerData(3) -band 2) -ne 0) { throw 'Upgrade rule only detects previous versions without removing them' }
            if ([version]$row.StringData(2) -le [version]'26.3.300') { throw 'Upgrade range does not cover the previous Dev release' }
            $upgradeRule = $true
        }
    }
    $upgradeView.Close()
    if (!$upgradeRule) { throw 'Missing major upgrade rule' }
    $view = $db.OpenView('SELECT `FileName`, `Version` FROM `File`')
    $view.Execute()
    $hasApp = $false
    while ($record = $view.Fetch()) {
        if ($record.StringData(1) -match '(^|\|)FluentTB\.exe$') {
            $hasApp = $true
            if ($record.StringData(2) -ne $Version) { throw 'Incorrect version inside MSI' }
        }
        if ($edition -eq 'Public' -and $record.StringData(1) -match 'NAudio|WindowsMediaController') { throw 'Media assembly inside public MSI' }
    }
    $view.Close()
    if (!$hasApp) { throw 'MSI lacks FluentTB.exe' }
    Write-Output "PASS: $edition MSI identity, app versions, dependencies and licenses."
}
if ($selectedEdition -eq 'Public') {
if (!(Test-Path "$base/FluentTB-Public-$Version-x64-Setup.exe")) { throw 'Missing EXE bootstrapper' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead("$base/FluentTB-Public-$Version-x64.msix")
try {
    $reader = [IO.StreamReader]::new($zip.GetEntry('AppxManifest.xml').Open())
    try { [xml]$manifest = $reader.ReadToEnd() } finally { $reader.Dispose() }
    if ($manifest.Package.Identity.Version -ne $Version) { throw 'Incorrect MSIX version' }
    if (!$zip.GetEntry('Resources/FluentTBDemoFloatingTaskbar.png')) { throw 'Missing onboarding screenshot in MSIX' }
    if (!$zip.GetEntry('FluentTB.exe')) { throw 'Missing MSIX executable' }
    if ($zip.Entries.FullName -match 'NAudio|WindowsMediaController') { throw 'Media dependency in public MSIX' }
    Write-Output "PASS: public MSIX payload and version; signature present: $([bool]$zip.GetEntry('AppxSignature.p7x'))."
} finally { $zip.Dispose() }
}
if ($selectedEdition -eq 'Dev' -and (Get-ChildItem $base -File | Where-Object Extension -in '.exe','.msix')) { throw 'Dev edition must be MSI only' }
Write-Output 'PASS: edition separation and requested installer formats.'

$source = Get-Content "$base/src/source-snapshot.json" -Raw | ConvertFrom-Json
if ($source.Edition -ne $selectedEdition -or $source.Version -ne $Version) { throw 'Wrong source snapshot.' }
foreach ($file in $source.Files) {
 if ((Get-FileHash -LiteralPath (Join-Path "$base/src" $file.Path)).Hash -ne $file.SHA256) { throw "Snapshot hash mismatch: $($file.Path)" }
}
foreach ($file in (Get-Content "$base/SHA256.json" -Raw | ConvertFrom-Json)) {
 if ((Get-FileHash -LiteralPath $file.Path).Hash -ne $file.Hash) { throw 'Package hash mismatch.' }
}
Write-Output 'PASS: matching versioned source snapshot and package hashes.'

& "$PSScriptRoot/InstallerLicense.Tests.ps1" -Version $Version
