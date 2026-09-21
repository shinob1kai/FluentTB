# Credits and component notices

- **Shinob1Kai** — creator of the original FluentTB continuation of RoundedTB.
- **RoundedTB / torchgm and contributors** — original taskbar customisation architecture. Original MIT notices are retained with the taskbar source.
- **FluentFlyout / Hugo Li (unchihugo) and contributors** — media widgets, flyouts, settings UI and source generator. Imported from https://github.com/unchihugo/FluentFlyout at commit `4947f69`; original copyright headers and contributor lists are preserved.

The combined application includes GPL-3.0-or-later FluentFlyout code. Its license is included in `LICENSE` and `licenses/FluentFlyout-GPL-3.0.txt`. The original FluentTB MIT notice is preserved in `licenses/FluentTB-MIT.txt`. NuGet dependencies retain their respective licenses; their versions are declared in the project files.

TranslucentTB remains an independent application and reference checkout. It is not merged into the executable.

## Release editions (2026.3.2.0)

The Public application is built from `src/FluentTB.Public` and the shared taskbar engine. It retains the adapted FluentFlyout lock-key graphics and key dispatch, with a reduced settings host. It does not include the media controller, NAudio, media/volume flyouts, next-up or visualizer. Public localization resources are shared with the desktop project. The Private application is built from `src/FluentTB.Desktop` with the complete integration. Both retain applicable original notices; the edition split does not reassign authorship of imported code.

## Installer license text

Both editions retain GPL-3.0-or-later imported code. The installer displays the complete canonical GNU GPL v3, original FluentTB MIT notice and these attributions. The GPL document copyright belongs to the Free Software Foundation; it is distinct from the imported software copyright (Copyright (C) 2025 Hugo Li), which remains attributed here and in source headers. Canonical license source: https://www.gnu.org/licenses/gpl-3.0.txt
