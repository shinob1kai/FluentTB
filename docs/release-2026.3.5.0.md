# FluentTB 2026.3.5.0

## Changes

- Both editions expose a persistent taskbar-shape switch at the top of the taskbar page. The switch applies immediately. Disabling cancels the shaping worker and resets taskbar regions/shadows after the last in-flight tick finishes. The worker does not resume until shaping is enabled again. Flyouts and their settings remain independent. Form parameters are retained for re-enabling.
- Old JSON settings default TaskbarShapeEnabled to true; explicit false survives both copy paths and JSON persistence. Disabled startup does not start the shaping worker. Public Home reflects the actual shape state.
- Dev has its own target-display picker at the top of the Media Flyout page. The default inherits System's display setting; an explicit selection affects only the media window. The open animation uses the selected monitor and DPI handling, and the close animation remembers that opening monitor. The list refreshes when the page loads; monitor choices use the existing monitor-index convention and may require reselection after topology/order changes.
- The user's original `src/FluentTB.Desktop/Resources/FluentTBDemoFloatingTaskbar.png` replaces the old screenshot in both packages, the shared taskbar page, and Dev onboarding. The old source image is left in place but is no longer packaged or referenced by active UI.

## Validation

- 151 geometry/settings/keyboard assertions passed, including legacy enabled-default and disabled-state copy/JSON persistence.
- No desktop control, app restart or installation was performed for this change. Practical monitor and taskbar lifecycle verification remains open.

## Manual checks

1. Close FluentTB using its tray menu; install the new edition and verify Home shows 2026.3.5.0.
2. Turn off taskbar shaping. On both monitors the full Windows taskbar should return, including the tray. Confirm Insert/Num-Lock notifications still work; in Dev confirm media flyout and widget still work.
3. Restart FluentTB while shaping is off. It should stay off. Enable it again and confirm previous margins/rounding/dynamic settings return on both monitors. Also try quickly toggling off and on.
4. Dev: choose another monitor in Media Flyout, play media and trigger the flyout. Confirm the chosen screen is used, then select the default to restore System's choice. Check closing animation and differing monitor DPI.
5. Confirm the updated floating-taskbar image is shown.

Release packages are Public MSI/EXE/unsigned MSIX and Dev MSI. Existing MSI upgrade identities and all license notices are retained. Builds are local, not published or installed.

Final validation: all four packages built; MSI identity/upgrade, version, dependency and license checks passed. Both published demo images match the supplied original and the public MSIX includes it. All final SHA-256 hashes verified. 18 NumberBox binding checks and 29-language validation passed. Existing upstream compiler warnings remain; no build errors.
