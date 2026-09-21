# FluentTB 2026.3.7.0

- Public and Dev: the taskbar shape introduction now explains corner rounding, adjustable spacing, dynamic segments, tray/Windows widget visibility and independent activation. All 29 offered languages are updated.
- Dev: the media flyout target monitor selector is directly below the enable switch, after the introduction.
- Dev: audio recovery remains active while playback, the media widget and its visualization are enabled. Failed starts retry every three seconds, including when the audio endpoint or service becomes available late after logon. Each attempt resolves the current multimedia output endpoint with a fresh enumerator, even if device monitoring failed during initialization.
- Recovery also handles RecordingStopped, device changes, unlock/logon and resume. Lifecycle operations are serialized; disabling or disposing cannot leave a pending start running. Partially initialized captures are released. Normal silence clears the displayed bars without repeatedly restarting WASAPI.
- Public source is frozen separately under versions/2026.3.7.0 before compilation. The 2026.3.6.0 snapshot remains unchanged. Dev has no versioned source archive.

## Verification

- 13 audio recovery checks: repeated initial failures beyond the old retry limit, partial cleanup, healthy silence, restart requests, disable/re-enable, disposal, automatic timer recovery and disabling during startup. These use simulated capture callbacks, not live Windows audio.
- 151 geometry/settings/keyboard assertions and all 29 localization dictionaries passed.
- Release compilation and packaging succeeded for Public MSI/EXE/MSIX and Dev MSI. The Dev compiler still reports 54 existing warnings; there were no build errors.
- All 18 WPF NumberBox binding checks passed. XML inspection confirms media cards are ordered enable, target monitor, background.
- Package checks passed for edition separation, MSI upgrade identities, executable/DLL versions, dependencies, notices and MSIX payload. The MSIX remains unsigned.
- All 119 source hashes in both the previous and new Public snapshots were verified. Published package SHA-256 hashes are recorded in the output folder.
- No desktop control, application restart, installation, Windows reboot or remote Git push was performed.

## Manual Dev test

1. Install the new Dev MSI, enable Windows autostart, the media widget and its visualization.
2. Restart Windows, then play music. The bars should appear without opening Settings or toggling the visualization.
3. If using Bluetooth audio, connect the output device after logon and continue playback. Recovery should occur within a retry interval after Windows makes the endpoint available.
4. Pause/resume playback, change the output device, and disable/re-enable the widget. Verify that bars resume and disappear appropriately.

The actual Windows-logon symptom remains to be verified on the affected machine; the code diagnosis and automated recovery checks do not substitute for that test.
