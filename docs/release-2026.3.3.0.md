# FluentTB 2026.3.3.0

## Lock-key notifications

Public and Dev share virtual-key classification for Caps Lock, Num Lock, Scroll Lock and Insert. Windows reports both dedicated Insert and NumPad Insert as VK_INSERT; numeric NumPad 0, decimal and Delete remain unrelated keys. Num Lock reports its actual Windows toggle state. Insert reports only a press, never an inferred application overwrite state. All four switches remain independently configurable.

The underlying Num-Lock and Insert handlers already existed. This release aligns their dispatch, enables Insert by default for new public settings and fixes the public Insert indicator's width/shackle animation. Existing saved choices, including Insert=false, are preserved. The Dev hook now defers flyout work to the dispatcher like the public edition. Public controls use the existing translated setting labels; English/German help explains NumPad and the overwrite limitation.

## Dev edition

The former private edition is named **FluentTB Dev** in the executable metadata, settings window, Home page and MSI. It retains every merged feature and the previous Home layout. The original `design/FluentDev.png` and `design/HeadBannerDev.png` are preserved under `assets/branding/dev`; `Build-DevIcons.ps1` produces a multi-resolution ICO without stretching the source. The public artwork and both editions' tray artwork remain unchanged.

Dev keeps the former private MSI UpgradeCode and settings locations. `Build-Editions.ps1 -Edition Private` remains a compatibility alias for Dev. This is a branding/edition change, not a Debug configuration: both editions are Release builds.

## Packages

- Public: MSI, EXE bootstrapper containing that MSI, unsigned MSIX.
- Dev: MSI only, for personal use; not published.
- Version: 2026.3.3.0; MSI version: 26.3.300.
- Microsoft Store submission still requires the assigned package identity; signing and publication are separate steps.

## Validation

- 147 automated assertions, including independent key switches, ignored numeric/Delete keys, Insert indication and Num-Lock on/off presentation.
- Localization check: 27 taskbar strings in 29 dictionaries, correct sidebar order.
- Dev compile: zero errors; existing upstream warnings remain.
- Original tray PNG hashes match both editions.
- MSI identities, payload versions, license files and edition-specific dependencies validated for Public and Dev; public EXE/MSIX present and Dev remains MSI-only.
- Public desktop test: Insert displayed “Einfg gedrückt”; Num Lock displayed “Num an” and “Num aus”. Num Lock was toggled twice and restored. Numeric NumPad 0 did not produce an Insert notification.
- The automation tool sends NumPad 0 as a numeric virtual key even with Num Lock off, and does not support KP_Insert. A physical NumPad Insert test is still required.
- Dev settings and branding opened successfully. A Dev Insert flyout was not visible in the captured settings-monitor area with the existing default target display; live Dev notification validation remains open. No successful Dev flyout test is claimed.
- Home uses the original PNG for the large Dev logo so WPF does not upscale a 16-pixel ICO frame. Final PNG binding is build-verified; further desktop testing is left to the user.

Manual keyboard checks require permission to control the desktop. Useful cases: dedicated Insert; NumPad 0 with Num Lock off/on; Num Lock on/off; Delete; each notification switch disabled; persistence after restart. Automated virtual-key tests do not replace testing a physical keyboard and its driver.
