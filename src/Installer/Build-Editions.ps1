# Public: MSI, EXE bootstrapper, MSIX. Dev: MSI only.
[CmdletBinding()]
param([ValidateSet('All','Public','Dev','Private')][string]$Edition = 'All', [string]$Version = '')
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if (!$Version) { [xml]$props = Get-Content (Join-Path $repo 'Directory.Build.props'); $Version = $props.Project.PropertyGroup.Version }
if ($Version -notmatch '^20\d{2}\.\d+\.\d+\.\d+$') { throw 'Version must use YEAR.QUARTER.BUILD.REVISION.' }
$configuredEdition = (Get-Content "$repo/edition.json" -Raw | ConvertFrom-Json).Edition
if ($configuredEdition -notin @('Public','Dev')) { throw 'Invalid edition.json.' }
if ($Edition -eq 'Private') { $Edition = 'Dev' }
if ($Edition -eq 'All') { $Edition = $configuredEdition }
if ($Edition -ne $configuredEdition) { throw "This source tree builds only $configuredEdition." }
$isSourceSnapshot = Test-Path (Join-Path $repo 'source-snapshot.json')
if ($isSourceSnapshot) {
    $snapshot = Get-Content "$repo/source-snapshot.json" -Raw | ConvertFrom-Json
    if ($snapshot.Version -ne $Version -or $snapshot.Edition -ne $Edition) { throw 'Snapshot version/edition mismatch.' }
    foreach ($file in $snapshot.Files) {
        if ((Get-FileHash -LiteralPath (Join-Path $repo $file.Path)).Hash -ne $file.SHA256) { throw "Archived source changed: $($file.Path)" }
    }
}
$v = [version]$Version
# MSI compares three fields. App EXE, DLL and MSIX retain the complete year.
$msiVersion = '{0}.{1}.{2}' -f ($v.Major - 2000), $v.Minor, ($v.Build * 100 + $v.Revision)
if ($v.Revision -gt 99) { throw "Revision must remain below 100 for MSI ordering." }
if ($v.Minor -gt 255 -or ($v.Build * 100 + $v.Revision) -gt 65535) { throw 'Version exceeds MSI limits.' }
$wix = 'C:\Program Files (x86)\WiX Toolset v3.14\bin'
foreach ($tool in 'heat','candle','light') { if (!(Test-Path "$wix/$tool.exe")) { throw "Missing WiX tool: $tool" } }
function Clear-BuildDirectory([string]$Path) {
    $resolved = [IO.Path]::GetFullPath($Path)
    if (!$resolved.StartsWith([IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'obj')) + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe build cleanup path.' }
    if (Test-Path $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
}
# Keep the old command-line name and MSI upgrade identity for existing private installations.
if ($Edition -eq 'Private') { $Edition = 'Dev' }
$editions = if ($Edition -eq 'All') { @('Public','Dev') } else { @($Edition) }
foreach ($flavor in $editions) {
    $project = if ($flavor -eq 'Dev') { 'FluentTB.Desktop' } else { 'FluentTB.Public' }
    $name = if ($flavor -eq 'Dev') { 'FluentTB Dev' } else { 'FluentTB' }
    $upgrade = if ($flavor -eq 'Dev') { '3473725A-0928-478A-9610-32F74029B2DB' } else { 'A1B2C3D4-E5F6-4890-ABCD-1234567890AB' }
    $out = if ($isSourceSnapshot) { Split-Path $repo -Parent } else { Join-Path (Split-Path $repo -Parent) "Outputs/$Version" }
    $work = Join-Path $PSScriptRoot "obj/v$Version/$flavor"
    $publish = Join-Path $work 'publish'
    New-Item -ItemType Directory -Force -Path $out,$work | Out-Null
    Clear-BuildDirectory $publish
    $sourceRepo = $repo
    if (!$isSourceSnapshot) {
        $sourceRepo = & "$PSScriptRoot/Export-ReleaseSource.ps1" -Version $Version -OutputDirectory $out
    }
    & dotnet publish "$sourceRepo/src/$project/$project.csproj" -c Release -r win-x64 --self-contained true -p:Platform=x64 "-p:Version=$Version" "-p:AssemblyVersion=$Version" "-p:FileVersion=$Version" -p:DebugType=None -p:DebugSymbols=false -o $publish -v minimal
    if ($LASTEXITCODE) { throw "$flavor publish failed" }
    # Clean only generated compiler output, never archived source.
    if ($sourceRepo -ne $repo) {
        $sourceBoundary = [IO.Path]::GetFullPath($sourceRepo) + '\'
        foreach ($generated in @('src/FluentTB/bin','src/FluentTB/obj','src/FluentTB.Public/bin','src/FluentTB.Public/obj','src/FluentTB.Desktop/bin','src/FluentTB.Desktop/obj','src/FluentFlyout.SourceGenerators/bin','src/FluentFlyout.SourceGenerators/obj')) {
            $generatedPath = [IO.Path]::GetFullPath((Join-Path $sourceRepo $generated))
            if (!$generatedPath.StartsWith($sourceBoundary, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe snapshot cleanup path.' }
            if (Test-Path -LiteralPath $generatedPath) { Remove-Item -LiteralPath $generatedPath -Recurse -Force }
        }
    }
    if ((Get-Item "$publish/FluentTB.exe").VersionInfo.FileVersion -ne $Version) { throw 'Incorrect published version.' }
    if ($flavor -eq 'Public' -and (Get-ChildItem $publish -Filter 'NAudio*.dll')) { throw 'Audio dependency leaked into public edition.' }
    & "$wix/heat.exe" dir $publish -cg PublishedFiles -dr INSTALLFOLDER -srd -sreg -scom -ag -var var.PublishDir -out "$work/Files.wxs"
    if ($LASTEXITCODE) { throw 'WiX harvest failed' }
    $icon = if ($flavor -eq 'Dev') { "$repo/src/FluentTB.Desktop/Resources/FluentTB.Dev.ico" } else { "$repo/src/FluentTB/res/FluentTB.ico" }
    $licenseRtf = & "$PSScriptRoot/Build-License.ps1" -SourceRoot $sourceRepo -OutputPath "$work/License.rtf"
    $licenseRtfXml = [Security.SecurityElement]::Escape($licenseRtf)
    $wxs = @"
<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
<Product Id="*" Name="$name" Language="1033" Version="$msiVersion" Manufacturer="Shinob1Kai" UpgradeCode="$upgrade">
<Package InstallerVersion="500" Compressed="yes" InstallScope="perMachine" Platform="x64"/>
<MajorUpgrade Schedule="afterInstallInitialize" DowngradeErrorMessage="A newer version of [ProductName] is already installed."/>
<MediaTemplate EmbedCab="yes"/><Property Id="ARPCOMMENTS" Value="FluentTB $Version - $flavor"/><Property Id="ARPHELPLINK" Value="https://github.com/shinob1kai/FluentTB"/>
<Icon Id="AppIcon" SourceFile="$icon"/><Property Id="ARPPRODUCTICON" Value="AppIcon"/>
<Directory Id="TARGETDIR" Name="SourceDir"><Directory Id="ProgramFiles64Folder"><Directory Id="INSTALLFOLDER" Name="$name"/></Directory><Directory Id="ProgramMenuFolder"><Directory Id="MenuFolder" Name="$name"/></Directory></Directory>
<DirectoryRef Id="MenuFolder"><Component Id="ShortcutComponent" Guid="*"><Shortcut Id="StartMenu" Name="$name" Target="[INSTALLFOLDER]FluentTB.exe" WorkingDirectory="INSTALLFOLDER"/><RemoveFolder Id="RemoveMenu" On="uninstall"/><RegistryValue Root="HKCU" Key="Software\$name" Name="Installed" Type="integer" Value="1" KeyPath="yes"/></Component></DirectoryRef>
<Feature Id="Complete" Title="$name" Level="1"><ComponentGroupRef Id="PublishedFiles"/><ComponentRef Id="ShortcutComponent"/></Feature>
<SetProperty Id="ARPINSTALLLOCATION" Value="[INSTALLFOLDER]" After="CostFinalize"/><Property Id="WIXUI_INSTALLDIR" Value="INSTALLFOLDER"/><UIRef Id="WixUI_InstallDir"/><WixVariable Id="WixUILicenseRtf" Value="$licenseRtfXml"/>
</Product></Wix>
"@
    $wxs | Set-Content "$work/Product.wxs" -Encoding utf8
    & "$wix/candle.exe" -arch x64 "-dPublishDir=$publish" "$work/Files.wxs" "$work/Product.wxs" -out "$work/"
    if ($LASTEXITCODE) { throw 'WiX compile failed' }
    $msi = "$out/FluentTB-$flavor-$Version-x64.msi"
    & "$wix/light.exe" "$work/Files.wixobj" "$work/Product.wixobj" -ext WixUIExtension -out $msi -pdbout "$work/Product.wixpdb"
    if ($LASTEXITCODE) { throw 'WiX link/validation failed' }
    if ($flavor -eq 'Dev') { continue }
    $iscc = (Get-Command ISCC.exe -ErrorAction Stop).Source
    $iss = Get-Content "$PSScriptRoot/Bootstrapper.iss" -Raw
    $iss.Replace('@VERSION@', $Version).Replace('@OUTPUT@', $out).Replace('@ICON@', $icon).Replace('@MSI@', $msi) | Set-Content "$work/Setup.iss" -Encoding utf8
    & $iscc "$work/Setup.iss"
    if ($LASTEXITCODE) { throw 'EXE bootstrapper failed' }
    $stage = Join-Path $work 'msix'
    Clear-BuildDirectory $stage
    New-Item -ItemType Directory -Force "$stage/Assets" | Out-Null
    Copy-Item "$publish/*" $stage -Recurse
    foreach ($asset in 'Square150x150Logo','Square44x44Logo','Wide310x150Logo','StoreLogo') { Copy-Item "$repo/src/FluentTB/res/$asset.png" "$stage/Assets/$asset.png" }
    [xml]$manifest = Get-Content "$PSScriptRoot/AppxManifest.xml"
    $manifest.Package.Identity.Version = $Version
    foreach ($node in @($manifest.Package.Capabilities.ChildNodes)) { if ($node.Name -eq 'rescap:Capability' -and $node.GetAttribute('Name') -ne 'runFullTrust') { [void]$node.ParentNode.RemoveChild($node) } }
    $splash = $manifest.SelectSingleNode("//*[local-name()='SplashScreen']"); if ($splash) { [void]$splash.ParentNode.RemoveChild($splash) }
    $manifest.Save("$stage/AppxManifest.xml")
    $sdk = Get-ChildItem 'C:/Program Files (x86)/Windows Kits/10/bin' -Directory | Where-Object { $_.Name -match '^10\.0\.\d+\.0$' } | Sort-Object { [version]$_.Name } -Descending | Where-Object { Test-Path "$($_.FullName)/x64/makeappx.exe" } | Select-Object -First 1
    if (!$sdk) { throw 'Windows SDK makeappx not found' }
    & "$($sdk.FullName)/x64/makeappx.exe" pack /d $stage /p "$out/FluentTB-Public-$Version-x64.msix" /o
    if ($LASTEXITCODE) { throw 'MSIX packing/validation failed' }
}
Get-ChildItem -LiteralPath $out -File | Where-Object Extension -in '.msi','.exe','.msix' | Get-FileHash -Algorithm SHA256 | Select-Object Path,Hash | ConvertTo-Json | Set-Content "$out/SHA256.json"
Write-Host "Release artifacts: $out"
