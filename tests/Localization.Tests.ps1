$ErrorActionPreference = 'Stop'
$desktop = Join-Path $PSScriptRoot '../src/FluentTB.Desktop'
$page = [IO.File]::ReadAllText((Join-Path $desktop 'Pages/FluentTaskbarPage.xaml'))
$code = [IO.File]::ReadAllText((Join-Path $desktop 'Pages/FluentTaskbarPage.xaml.cs'))
$required = [regex]::Matches($page + $code, 'Ftb\w+') | ForEach-Object Value | Sort-Object -Unique
$files = Get-ChildItem (Join-Path $desktop 'Resources/Localization') -Filter 'Dictionary-*.xaml'
foreach ($file in $files) {
    [xml]$dictionary = [IO.File]::ReadAllText($file.FullName)
    foreach ($key in $required) {
        $entries = @($dictionary.DocumentElement.ChildNodes | Where-Object {
            $_ -is [System.Xml.XmlElement] -and $_.GetAttribute('Key', 'http://schemas.microsoft.com/winfx/2006/xaml') -eq $key
        })
        if ($entries.Count -ne 1 -or [string]::IsNullOrWhiteSpace($entries[0].InnerText)) {
            throw "$($file.Name): missing, empty or duplicated translation $key"
        }
    }
}
[xml]$navigation = [IO.File]::ReadAllText((Join-Path $desktop 'SettingsWindow.xaml'))
$items = @($navigation.SelectNodes('//*[@TargetPageType]'))
$homeIndex = [Array]::FindIndex($items, [Predicate[object]] { param($item) $item.TargetPageType -eq '{x:Type pages:HomePage}' })
$taskbarIndex = [Array]::FindIndex($items, [Predicate[object]] { param($item) $item.TargetPageType -eq '{x:Type pages:FluentTaskbarPage}' })
if ($homeIndex -lt 0 -or $taskbarIndex -ne $homeIndex + 1) { throw 'Home must immediately precede Taskbar shape.' }
Write-Output "PASS: $($required.Count) taskbar strings in $($files.Count) locales; sidebar order correct."
