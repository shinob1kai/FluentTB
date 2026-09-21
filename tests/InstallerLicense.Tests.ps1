param([string]$Version = '')
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
if (!$Version) { [xml]$props = Get-Content "$repo/Directory.Build.props"; $Version = $props.Project.PropertyGroup.Version }
$edition = (Get-Content "$repo/edition.json" -Raw | ConvertFrom-Json).Edition
$output = Join-Path (Split-Path $repo -Parent) "Outputs/$Version"
$msi = Join-Path $output "FluentTB-$edition-$Version-x64.msi"
$installer = New-Object -ComObject WindowsInstaller.Installer
$db = $installer.OpenDatabase([IO.Path]::GetFullPath($msi), 0)
$view = $db.OpenView('SELECT `Text` FROM `Control` WHERE `Dialog_` = ''LicenseAgreementDlg'' AND `Control` = ''LicenseText''')
$view.Execute()
$record = $view.Fetch()
if (!$record) { throw 'MSI has no license text control.' }
$rtf = $record.StringData(1)
$view.Close()
Add-Type -AssemblyName System.Windows.Forms
$control = New-Object System.Windows.Forms.RichTextBox
try { $control.Rtf = $rtf; $text = $control.Text.Replace("`r", '') } finally { $control.Dispose() }
foreach ($relative in 'LICENSE','licenses/FluentTB-MIT.txt','THIRD_PARTY_NOTICES.md') {
    $expected = [IO.File]::ReadAllText((Join-Path $repo $relative)).Replace("`r", '').Trim()
    if (!$text.Contains($expected)) { throw "MSI dialog has incomplete or outdated $relative" }
}
$expectedName = if ($edition -eq 'Dev') { 'FluentTB Dev-Edition' } else { 'FluentTB' }
if (!$text.StartsWith("$expectedName - License information")) { throw 'Wrong edition license heading.' }
$view = $db.OpenView('SELECT `Property` FROM `Control` WHERE `Dialog_` = ''LicenseAgreementDlg'' AND `Control` = ''LicenseAcceptedCheckBox''')
$view.Execute()
if ($view.Fetch().StringData(1) -ne 'LicenseAccepted') { throw 'Missing acceptance checkbox binding.' }
$view.Close()
$view = $db.OpenView('SELECT `Action`, `Condition` FROM `ControlCondition` WHERE `Dialog_` = ''LicenseAgreementDlg'' AND `Control_` = ''Next''')
$view.Execute()
$rules = @{}
while ($row = $view.Fetch()) { $rules[$row.StringData(1)] = $row.StringData(2) }
$view.Close()
if ($rules.Disable -ne 'LicenseAccepted <> "1"' -or $rules.Enable -ne 'LicenseAccepted = "1"') { throw 'Next is not gated on license acceptance.' }
$view = $db.OpenView('SELECT `Condition` FROM `ControlEvent` WHERE `Dialog_` = ''LicenseAgreementDlg'' AND `Control_` = ''Next'' AND `Event` = ''NewDialog''')
$view.Execute()
$count = 0
while ($row = $view.Fetch()) {
    $count++
    if ($row.StringData(1) -ne 'LicenseAccepted = "1"') { throw 'License dialog can be bypassed using Next.' }
}
$view.Close()
if (!$count) { throw 'Missing accepted-license continuation.' }
$view = $db.OpenView('SELECT `Value` FROM `Property` WHERE `Property` = ''LicenseAccepted''')
$view.Execute()
$row = $view.Fetch()
if ($row -and $row.StringData(1) -eq '1') { throw 'License acceptance must not be preselected.' }
$view.Close()
Write-Output "PASS: $edition MSI embeds complete GPL/MIT/attributions, has the correct title, and requires acceptance in the interactive wizard."
