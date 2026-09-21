# FluentTB — Public edition

Windows 11 taskbar shaping and lock-key flyouts. This source tree does not contain the Dev media/widget implementation or its audio dependencies.

Local layout:

```text
FluentTB/
  Source/                 # this Git checkout
  Outputs/
    2026.3.8.0/
      src/                # exact source compiled for this version
      FluentTB-Public-2026.3.8.0-x64.msi
      FluentTB-Public-2026.3.8.0-x64-Setup.exe
      FluentTB-Public-2026.3.8.0-x64.msix
      SHA256.json
```

The small src/FluentTB.Desktop directory contains only linked taskbar UI, translations, app manifest and demo resources. It does not contain a Desktop project or the Dev host. src/FluentTB.Public is the executable, src/FluentTB the taskbar engine, src/Shared the keyboard/release helpers.

MSI UpgradeCode: A1B2C3D4-E5F6-4890-ABCD-1234567890AB. MSIX signing and the assigned Store identity still need publisher configuration.

## Build and release layout

Use Windows 11 and the .NET 10 SDK. Each Source checkout is self-contained and has its own copies of shared code; there are no references to the sibling edition or the archived integration tree.

```powershell
dotnet build FluentTB.slnx -c Release -p:Platform=x64
./Update-Version.ps1 -NewVersion 2026.3.8.0
./src/Installer/Build-Editions.ps1
```

The build reads edition.json and refuses the other edition. All means only the edition of this checkout. It exports Source to ../Outputs/<version>/src before compiling that snapshot. Installers and SHA256.json sit beside src, never inside it. A source-snapshot.json file records the edition, version and source hashes. An existing source snapshot cannot be replaced with changed source; increment the version first. Build scripts can also run inside an archived src folder, after verifying its hashes. Generated build files are excluded from source archives.

MSI packaging requires WiX 3.14 at the path checked by the script. Public additionally requires Inno Setup (ISCC.exe on PATH) and the Windows SDK (makeappx.exe). Release output is self-contained and contains no PDB files. Signing, installation and remote publication are separate steps.

Version format: YEAR.QUARTER.BUILD.REVISION. MSI maps this to (year-2000).quarter.(build*100+revision), with revision below 100. Preserve the edition UpgradeCode when incrementing a version so its MSI updates the previous installation. Never install both editions to own the taskbar simultaneously.

## Shared changes and repository boundaries

Source is the Git checkout. Keep Outputs, bin, obj, packages, logs, local user settings and reference repositories out of commits. Develop Dev-only features in Dev-Edition. Port changes to the taskbar core, shared keyboard code or taskbar UI deliberately to Public and test both copies. Do not merge the complete Dev host into Public for a shared change. No sibling checkout is required to compile.

## Settings and notices

Taskbar settings retain their existing JSON location under LOCALAPPDATA/FluentTB (or the packaged core's APPDATA/FluentTB path). Dev flyouts use APPDATA/FluentTB/flyouts.xml, Public lock keys use LOCALAPPDATA/FluentTB/public.json. MSI upgrades must retain these settings. Missing taskbar-enable settings default to enabled; explicit false remains preserved.

FluentTB: Shinob1Kai. RoundedTB: torchgm and contributors. FluentFlyout: Hugo Li (unchihugo) and contributors. Applicable notices remain in LICENSE, THIRD_PARTY_NOTICES.md and licenses/. FluentFlyout is separate from the unrelated FluentFlyouts application.

## Verification

```powershell
dotnet run --project tests/FluentTB.Tests/FluentTB.Tests.csproj
./tests/SourceArchive.Tests.ps1
./tests/Release.Tests.ps1
```

Verify both taskbar alignments, mixed-DPI monitors, taskbar shaping off/on, tray access and lock-key notifications before publishing. Local packaging does not publish a release.

## Installer license (2026.3.9.0)

The interactive MSI wizard embeds the complete canonical GNU GPL v3 text, identifies this combined application as GPL-3.0-or-later, and retains the original FluentTB MIT notice and component credits. Next remains disabled until the license checkbox is selected. Build-License.ps1 generates the RTF from the release snapshot; WiX uses it through WixUILicenseRtf. Package tests read and decode the actual MSI text and verify the acceptance controls. The Public EXE launches the same MSI wizard. MSIX uses the Windows-managed installer and has no custom MSI license page.

Sources: https://www.gnu.org/licenses/gpl-3.0.txt and https://docs.firegiant.com/wix3/wixui/wixui_customizations/

## First-run storage fix (2026.3.11.0)

The taskbar engine now creates the configuration and log parent directories before opening files. Fresh profiles no longer fail when LOCALAPPDATA/FluentTB is absent. The same initialization covers the existing packaged-app Roaming path. This is performed by the application for the launching user, not by a machine-wide MSI under the installing administrator's profile. Existing settings are preserved; missing/empty settings receive the existing defaults. A locked log alone does not block startup, and saving settings recreates a missing configuration directory.

Regression checks reproduce the old DirectoryNotFoundException in isolated temporary Local/Roaming profile directories, then exercise the actual new storage initializer: files/defaults, exact preservation of existing settings, empty-file recovery and a locked log. Both editions pass 165 core assertions. These tests do not install software or launch the taskbar hooks on a clean Windows account.
