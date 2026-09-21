# FluentTB / FluentFlyout integration preview

## Layout

- `src/FluentTB.Desktop`: .NET 10 WPF executable, shared tray, FluentFlyout settings and media/volume/lock-key/next-track flyouts. `Integration` connects it to the taskbar engine; `Pages/FluentTaskbarPage` exposes FluentTB settings.
- `src/FluentTB`: taskbar engine library. `Taskbar` owns geometry, `Interop` owns Win32, `Models` owns persisted settings, `Services` owns background lifecycle, and `Views` retains the hidden engine adapter.
- `src/FluentFlyout.SourceGenerators`: imported FluentFlyout source generator.
- `src/Installer`: installer and MSIX staging scripts.
- `tests/FluentTB.Tests`: current .NET 10 regression runner; older standalone PowerShell test scripts are historical .NET Framework harnesses.
- `assets/branding`: new master logo and reproducible Windows icon conversion.

Original FluentFlyout namespaces and copyright headers are retained for comparison with upstream. The separate reference clone is untouched. Dependencies and versions are recorded in the project files.

## Settings and lifecycle

Taskbar JSON remains at `%LOCALAPPDATA%/FluentTB/fluent-tb.json`. Flyout settings use `%APPDATA%/FluentTB/flyouts.xml`; standalone FluentFlyout settings are not overwritten. The initial media-widget preferences migrate once. One process and one tray own both modules. The upstream updater, telemetry and remote experiment enrollment are disabled in this integration; it must not install standalone FluentFlyout over FluentTB.

The widget adopts FluentTB's physical top/bottom margins and GDI corner diameter (WPF uses half that diameter as its radius). Content controls its width. Its rounded child region is published after successful creation and included in the parent's dynamic taskbar region. This avoids the parent clipping its media controls and avoids a rectangular widget background.

## Validation and remaining work

130 assertions cover taskbar geometry across monitor origins, alignments and DPI, settings migration/copying/persistence, and widget insets/corners including separate margins and excessive-margin fallback. Debug build succeeds with 56 existing warnings in the integrated source. UI startup and the new icon/credits have been checked.

Live check (Integration5): two 1920×1080 displays, left-aligned taskbar, margins 3, corner diameter 7. The secondary-monitor widget region is rounded (`cornerIncluded=False`), with bounds `1521,3–1637,44` in taskbar coordinates. The GDI region's bottom/right exclusion matches the taskbar's existing `CreateRoundRectRgn` convention. Both taskbars report eight app buttons and zero clipped app centers. Widget enablement was restored to its initial off state after the diagnostic.

This is an integration preview. Remaining work includes the imported nullable/asynchronous warnings, obsolete premium/update messaging and promotional assets, completing real mixed-DPI/Explorer-restart/auto-hide and TranslucentTB regression matrices, and validating newly published installer payloads. Earlier phase notes are historical results, not proof of all combinations in this merged executable. Canary features still require the requested interpretation review before implementation.

## Embedded media visualizer

The visualizer is now a child of the media widget's content panel, before or after the media controls. Its width participates in the widget's own measurement; only one rounded region is applied, with no separate position or hover background. Clicking the visualizer follows the media widget's normal action. Legacy position values map to before/after; the old independent-click setting is retained in persisted settings but no longer exposed.

Capture runs only while the widget and visualizer are enabled and the displayed media session is playing. Pause/no session clears the bars and stops capture. The watchdog no longer accesses a timer being disposed from an audio callback. Audio format values are cached before capture begins to avoid reading a released capture during pause.

The inherited WASAPI source is the default output mix, not per-process capture. Other audible apps can affect the spectrum while media is playing; this is stated in the settings description. Process-isolated capture remains separate work.

Live Spotify playback/pause was exercised. The combined secondary-monitor region measured `1438,3–1637,44` with rounded corners, compared with the earlier media-only region `1521,3–1637,44`. Spotify was paused again after the test. The enabled widget/visualizer preferences were preserved.
