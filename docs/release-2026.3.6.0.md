# FluentTB 2026.3.6.0

- Both editions start in the background. Startup no longer opens Settings/Home or onboarding. Tray settings actions and the existing explicit settings-open signal remain available.
- Dev tray tooltip says FluentTB Dev. All translated tray commands use FluentTB instead of FluentFlyout.
- Dev always uses the supplied original TrayDark/TrayLight PNGs according to the Windows system theme. The obsolete app-logo/monochrome switch is removed from the settings page; the old persisted field remains compatible. System theme changes refresh the tray icon, and the event handler is removed during cleanup. Public already uses the original corresponding ICOs.
- Public release source is exported before compilation to versions/2026.3.6.0. The public application is compiled from that snapshot. The source manifest contains SHA-256 hashes; existing releases cannot be replaced by changed source. Snapshots exclude generated files and the Dev implementation. There is no per-version Dev source archive.
- README.Dev.md documents architecture, dependencies, edition boundaries, MSI upgrades, settings migration, source archives, branch coordination, credits and test limitations. It is also the root README in the separate local Dev-Edition checkout. That checkout has a local documentation commit; nothing has been pushed. Its remote baseline still needs the integration source changes before the documented new build layout is available there.

## Checks

- Source-export tests passed: filtering, idempotence, refusal to overwrite changed versions, separate new versions and detection of modified archive files.
- Original tray PNG hashes match the supplied design assets in both editions. No upstream app name remains in TrayIcon translation entries across all dictionaries.
- Localization validation passed for 29 dictionaries.
- Public build successfully compiled from the frozen source tree, verifying its dependency closure.
- Final Public MSI/EXE/MSIX and Dev MSI passed payload, version, upgrade-identity and license checks. All four installer hashes match SHA256.json. The 119-file source manifest was rechecked successfully; the snapshot contains no Dev host or generated bin/obj folders.
- No app restart, desktop control, installation or GitHub publication was performed. Cold startup, theme switching and tray interaction still require manual confirmation.

## Manual checks

Close the running FluentTB instance using its tray menu, then install this edition. Start it normally: Settings should stay closed and the tray icon should appear. Open Settings from the tray. Verify the icon against the original in light and dark Windows taskbar themes, and verify that its menu says FluentTB. Opening a second app instance intentionally signals the existing instance to show Settings; that existing behavior is retained.

Previous public versions are not reconstructed from current source. The immutable source-folder policy begins with 2026.3.6.0. Dev changes continue in the Dev-Edition branch.
