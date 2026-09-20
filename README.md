# FluentTB Dev

Full development edition maintained by **Shinob1Kai**, including the FluentFlyout integration. Target branch: [Dev-Edition](https://github.com/shinob1kai/FluentTB/tree/Dev-Edition).

> Integration status: this document describes the current 2026.3.6.0 integration working tree. The remote branch was created from the older repository. Adding this README alone does not transfer the newer application code; the integration files must also be committed before these build instructions apply there. Build scripts do not publish or merge changes.

## Editions

| | Public | Dev |
|---|---|---|
| Optional taskbar shaping and lock-key notifications | Yes | Yes |
| Media/volume flyouts, media widget, visualizer, next-track flyout | No | Yes |
| Installers | MSI, EXE bootstrapper, unsigned MSIX | MSI only |
| Source history | Immutable `versions/<version>/` folders | Evolving branch, no version folders |
| App branding | FluentTB | FluentTB Dev |

Both editions start in the background and use the original theme-aware FluentTB tray artwork. Settings open through the tray. Dev is an edition name, not a Debug configuration: shipping builds use Release.

The shared `FluentTB` mutex prevents two simultaneous taskbar owners. Different MSI identities allow separate installations, not simultaneous operation. A branch in a publicly visible GitHub repository is also public; naming it Dev does not make its source confidential. Select release assets deliberately to avoid publishing the private MSI.

## Features and limits

Taskbar shaping supports margins, rounding, dynamic regions and Windows Widgets visibility. Disabling its switch restores the Windows taskbar while other features continue. The media flyout has a separate monitor selection, defaulting to System's choice. Display selections use existing monitor indices; verify them after changing topology or display ordering.

Caps/Num/Scroll Lock report the Windows toggle state. Insert reports a key press only: individual applications manage overwrite mode. Numeric NumPad 0 is not Insert while Num Lock is enabled. Visualization captures system audio, not guaranteed isolated audio from one app. Native Windows auto-hide currently suspends shaping to avoid flicker.

## Source layout

- `src/FluentTB.Desktop`: full Dev WPF host, flyouts, settings and localization.
- `src/FluentTB`: shared taskbar/Win32 engine, producing `FluentTB.Taskbar.dll`.
- `src/FluentTB.Public`: limited Public host without media/audio dependencies.
- `src/Shared`: shared version information and lock-key classification.
- `src/FluentFlyout.SourceGenerators`: full settings UI generator.
- `src/Installer`: authoritative packaging and source-export scripts.
- `assets/branding`: original artwork and icon-generation scripts.
- `tests`, `docs`: verification and release-specific limitations.
- `licenses`, `THIRD_PARTY_NOTICES.md`: required component attribution.

Existing `FluentFlyoutWPF` and `FluentFlyout` namespaces are retained intentionally. Blind renaming can break XAML, generated code and settings serialization. User-facing branding does not replace upstream copyright notices.

## Build prerequisites

Windows 11 and the **.NET 10 SDK** are required. This is modern .NET, not legacy .NET Framework. Target: `net10.0-windows10.0.22000.0`. Installer builds are self-contained x64. Dependencies are pinned in the project files; do not update them during unrelated changes.

The full host uses WPF-UI, MicaWPF, CommunityToolkit.Mvvm, Dubya.WindowsMediaController, NAudio, NLog and Toolkit notifications. The core uses Hardcodet.NotifyIcon, DesktopBridge.Helpers and Newtonsoft.Json. Do not transfer Dev-only package references into Public.

```powershell
dotnet build src/FluentTB.Desktop/FluentTB.Desktop.csproj -c Release -p:Platform=x64
./src/Installer/Build-Editions.ps1 -Edition Dev
```

MSI packaging needs WiX 3.14 at the path checked by the script. Public additionally needs Inno Setup (`ISCC.exe` on PATH) and the Windows SDK (`makeappx.exe`). `-Edition Private` remains an alias for Dev. Explicitly use `-Edition Dev` on this branch; use `All` only to deliberately prepare both editions.

Output: `src/Installer/Output/v<version>/Dev`. Signing, certificates, Store identity, installing and uploading are separate actions. Never commit generated bin/obj/packages/Output, logs, local settings or reference repositories.

## Versions and MSI updates

Use `Update-Version.ps1 -NewVersion 2026.3.6.0` to set the four-part YEAR.QUARTER.BUILD.REVISION version. MSI maps it to `(year-2000).quarter.(build*100+revision)`; revision must stay below 100. Increment versions for changed releases. Store MSIX submissions also require the assigned identity and Store version constraints.

Preserve these UpgradeCodes for ordinary updates:

- Dev/private: `{3473725A-0928-478A-9610-32F74029B2DB}`.
- Public: `{A1B2C3D4-E5F6-4890-ABCD-1234567890AB}`.

MSIs receive new ProductCodes. A higher version retaining the edition's UpgradeCode replaces the previous installation in a rollback-capable transaction. Do not swap identities or reuse lower versions. This is installer-driven updating, not an automatic download service.

## Public source snapshots; no Dev snapshots

Beginning with 2026.3.6.0, Public builds export `versions/<version>/` before compiling the public app from that source. The snapshot contains the Public dependency closure, licenses and build scripts. Its small FluentTB.Desktop subtree contains linked public UI/resources, not the Dev application.

`source-snapshot.json` records SHA-256 hashes. Identical repeated exports are allowed. Changed source with the same version, or a modified archived file, causes an error; old source is never replaced. Build output and previous archives are excluded. Do not reconstruct historical versions from today's code.

Dev remains a normal evolving branch without per-version source folders. Keep public versions/ changes on the public branch rather than merging archives into Dev to synchronize shared fixes.

## Settings compatibility

- Taskbar JSON normally uses `%LOCALAPPDATA%/FluentTB/fluent-tb.json`; packaged-core behavior uses its existing `%APPDATA%/FluentTB` location. Do not silently relocate settings.
- Dev: `%APPDATA%/FluentTB/flyouts.xml`.
- Public lock-key settings: `%LOCALAPPDATA%/FluentTB/public.json`.
- Core settings may be shared between editions; flyout settings are separate.
- Old JSON without a shape switch defaults to enabled. Explicit false must survive both copy paths, serialization and restart.
- A missing media monitor override inherits the global target display.
- The old NIconSymbol field remains readable, but no longer selects the app logo as tray artwork.
- MSI upgrades must not delete user settings or harvest them into installer components.

## Avoiding branch conflicts

1. Check branch and working-tree status before checkout/merge. Use focused commits per fix or feature.
2. Develop full-host features on Dev-Edition. Transfer shared fixes deliberately into Public, reviewing core, shared code, linked taskbar UI and localization.
3. Do not merge the entire Dev host/media dependencies into Public to obtain a shared fix.
4. Maintain README.Dev.md in the integration tree; use its content as the Dev branch's root README.md. Public snapshots receive their own generated README.
5. Do not resolve archive conflicts by regenerating old versions from current code. Increment the public version and create a new folder.
6. Test both editions for shared changes. Preserve upgrade identities and all license notices.
7. Select only the intended edition's installers for publishing. A local successful build is not a published release or an installed update.

## Verification

```powershell
dotnet run --project tests/FluentTB.Tests/FluentTB.Tests.csproj
dotnet run --project tests/FluentTB.BindingTests/FluentTB.BindingTests.csproj -p:Platform=x64 -- src/FluentTB.Desktop/Pages/FluentTaskbarPage.xaml
./tests/Localization.Tests.ps1
./tests/PublicSource.Tests.ps1
./tests/Release-Editions.Tests.ps1 -Version 2026.3.6.0
```

Installer verification expects both edition outputs; use it after an intentional All build. Also check cold background startup, tray settings access, light/dark Windows tray artwork, shaping off/on and restart while off, both alignments, mixed DPI, media monitor, lock keys/physical NumPad Insert, Explorer restart and TranslucentTB combinations.

Automated checks do not prove live shell behavior. Record existing upstream warnings and unverified combinations in release notes. Ask before taking desktop control, or provide manual test steps instead.

## Credits and licenses

FluentTB: **Shinob1Kai**. RoundedTB: **torchgm and contributors**. FluentFlyout integration: **Hugo Li (unchihugo) and contributors**. FluentFlyout (singular) is separate from the unrelated FluentFlyouts application. Retain applicable GPL/MIT licenses and attribution; Dev branding does not change third-party ownership.
