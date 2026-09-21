# FluentTB Dev-Edition

The complete FluentTB and FluentFlyout integration, maintained by Shinob1Kai. The Dev-Edition branch is distributed as source only. Its MSI is produced locally for private installation and must not be attached to public GitHub releases. The in-app Dev update link opens the source branch, not Public binary releases.

```text
FluentTB Dev-Edition/
  Source/                 # this Git checkout (Dev-Edition branch)
  Outputs/
    2026.3.8.0/
      src/                # exact Dev source compiled for this version
      FluentTB-Dev-2026.3.8.0-x64.msi
      SHA256.json
```

## Architecture and dependencies

- src/FluentTB.Desktop: .NET 10 WPF host, settings, media/volume/lock-key flyouts, taskbar media widget, audio visualizer and shell integration. Existing FluentFlyout namespaces are retained.
- src/FluentTB: taskbar engine, clipping, margins, rounding, multi-monitor geometry and shell recovery.
- src/FluentFlyout.SourceGenerators: build-time settings/search generator.
- src/Shared: edition-local release and lock-key helpers.
- src/Installer: local MSI build and source export scripts. No public executable project is included.

Pinned host dependencies: Dubya.WindowsMediaController 2.5.6, MicaWPF 6.3.2, Microsoft.Toolkit.Uwp.Notifications 7.1.3, NAudio 2.3.0, NLog 6.1.3, unchihugo.WPF-UI 4.4.2, WPF-UI.Tray 4.3.0 and CommunityToolkit.Mvvm 8.4.2. Core retains Hardcodet.NotifyIcon.Wpf 2.0.1, DesktopBridge 1.2.2 and Newtonsoft.Json 13.0.3. See project files for authoritative versions. The SDK preview language setting supports the existing generated partial properties; this is .NET 10, not .NET Framework.

Dev MSI UpgradeCode: 3473725A-0928-478A-9610-32F74029B2DB. The former Private edition uses the same identity and settings. -Edition Private remains a command-line alias for Dev.

## Automatic widget placement

Windows taskbar alignment is read at startup and each existing 1.5-second position update. Centered alignment places the media widget at the start side, beside the native Windows widget. Left alignment places it at the end side, before the native widget or notification area. Actual native widget/tray bounds determine spacing on the selected monitor. DPI, rounding and the custom offset still apply.

Automatic placement is enabled for new settings and older XML files without the new field. Disable “Automatically follow Windows taskbar alignment” to use the saved Left/Center/Right selection. Vertical taskbars retain manual placement. Changing Windows alignment does not require reopening FluentTB. Missing registry values use the Windows 11 centered default; temporary read failures retain the last known value.

Audio visualization retries unavailable capture endpoints while playback and the widget/visualization are enabled. Stopping or disabling the feature cancels recovery; normal silence does not trigger repeated capture restarts.

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
dotnet run --project tests/FluentTB.AudioRecoveryTests/FluentTB.AudioRecoveryTests.csproj
dotnet run --project tests/FluentTB.BindingTests/FluentTB.BindingTests.csproj -p:Platform=x64 -- src/FluentTB.Desktop/Pages/FluentTaskbarPage.xaml
./tests/Localization.Tests.ps1
./tests/SourceArchive.Tests.ps1
./tests/Release.Tests.ps1
```

Manual checks: enable automatic placement and switch Windows alignment left/center; repeat on both monitors and at different DPI settings, with native Widgets enabled/disabled, shaping enabled/disabled and Explorer restarted. Verify widget manual positioning after disabling automatic placement. Check Windows-logon audio recovery, media target monitor and physical NumPad Insert separately. Automated tests do not establish live shell behavior; ask before desktop control or provide manual steps.

## Installer license (2026.3.9.0)

The interactive MSI wizard embeds the complete canonical GNU GPL v3 text, identifies this combined application as GPL-3.0-or-later, and retains the original FluentTB MIT notice and component credits. Next remains disabled until the license checkbox is selected. Build-License.ps1 generates the RTF from the release snapshot; WiX uses it through WixUILicenseRtf. Package tests read and decode the actual MSI text and verify the acceptance controls. The Public EXE launches the same MSI wizard. MSIX uses the Windows-managed installer and has no custom MSI license page.

Sources: https://www.gnu.org/licenses/gpl-3.0.txt and https://docs.firegiant.com/wix3/wixui/wixui_customizations/

## Visualization output selection (2026.3.10.0)

The widget visualization settings list Windows output devices directly in the app. Windows default follows the multimedia output endpoint. A specific device is persisted by endpoint ID, so device ordering or identical display names do not change the selection. An unavailable saved device stays selected, is labelled unavailable, and is retried by the existing capture recovery loop. The list refreshes when the page or dropdown opens. Device enumeration runs off the UI thread and stale responses are ignored after navigation.

This selects the audio source for the visualization; it does not reroute music or change the Windows default output. The old Windows Settings hyperlink has been removed. All 29 locales have the new selection labels and description.

Validation: 18 audio-device checks passed, including legacy XML, saved ID/default round-trips, resolving 11 available endpoints by ID and an unavailable endpoint without default fallback. The endpoint test is read-only and does not capture audio. Run it with:

    dotnet run --project tests/FluentTB.AudioDeviceTests/FluentTB.AudioDeviceTests.csproj -p:Platform=x64

Manual test: play music on the chosen device and check the bars; switch to another endpoint and back to Windows default; restart the app and disconnect/reconnect the selected device. Allow up to three seconds for capture recovery. Live playback and the visible dropdown have not been tested through desktop control.

## First-run storage fix (2026.3.11.0)

The taskbar engine now creates the configuration and log parent directories before opening files. Fresh profiles no longer fail when LOCALAPPDATA/FluentTB is absent. The same initialization covers the existing packaged-app Roaming path. This is performed by the application for the launching user, not by a machine-wide MSI under the installing administrator's profile. Existing settings are preserved; missing/empty settings receive the existing defaults. A locked log alone does not block startup, and saving settings recreates a missing configuration directory.

Regression checks reproduce the old DirectoryNotFoundException in isolated temporary Local/Roaming profile directories, then exercise the actual new storage initializer: files/defaults, exact preservation of existing settings, empty-file recovery and a locked log. Both editions pass 165 core assertions. These tests do not install software or launch the taskbar hooks on a clean Windows account.
