# FluentTB 2026.3.4.0

- User-supplied screenshot: `src/FluentTB.Desktop/Resources/Onboarding/FloatingTaskbar.png`. Included unchanged in both published editions and displayed at the top of the shared taskbar-shape page. Dev onboarding also introduces taskbar shape as its first step. Uniform scaling preserves the taskbar at the bottom of the artwork.
- Insert notification setting is immediately below the master lock-key switch in both editions. German label: **Einfg / Insert – Tastendruck anzeigen**. Public help is visible inline, not only as a tooltip. The setting still reports a press, not an application-specific overwrite mode. Existing saved preferences are preserved.
- Dev MSI retains UpgradeCode `{3473725A-0928-478A-9610-32F74029B2DB}`. Read-only inspection confirmed this is also the installed Dev 2026.3.3.0 product's identity. MSI version rises from 26.3.300 to 26.3.400; a new ProductCode is generated. The previous product is removed inside the installation transaction so failure can roll it back. The installation folder is recorded in ARPINSTALLLOCATION for future diagnostics.
- Running the newer Dev MSI upgrades the previous Dev/private product. This is installer-driven updating, not an automatic download service. Public retains its separate upgrade identity. User settings live outside the installation folder and are not MSI components.
- No desktop control, installation, uninstall or restart performed for this release. The installed version remains unchanged until the user runs the MSI.

## Manual verification

1. Close FluentTB via its tray menu and run the new Dev MSI. Check Windows Installed Apps for one Dev entry and Home for 2026.3.4.0. Confirm previous settings were retained.
2. Open taskbar shape: the new floating taskbar image should appear above the controls.
3. Open lock-key flyout: enable the master switch and **Einfg / Insert – Tastendruck anzeigen** directly below it. For testing, choose the monitor with the focused window.
4. Press Insert: expect “Einfg gedrückt”. Toggle Num Lock twice: expect on/off and restore the initial state. Physical NumPad 0 with Num Lock off should behave as Insert; with Num Lock on it should remain a numeric key.

Build and MSI-table checks verify package contents and upgrade rules; they do not replace a real installation/rollback test. The earlier 2026.3.3.0 public desktop test passed Insert and both Num-Lock states. Live Dev and physical NumPad checks remain open.

## Completed checks

- Both Release builds and all four installer formats generated successfully. Existing upstream compiler warnings remain.
- 18 immediate taskbar NumberBox binding checks passed; localization validation passed for 29 dictionaries.
- MSI product identities, versions, upgrade rules covering 26.3.300, edition dependencies and licenses passed.
- Original screenshot hash matches both published copies; public MSIX includes the image.
- All four final artifact SHA-256 hashes verified. Actual MSI installation and live UI review are left to the user.
