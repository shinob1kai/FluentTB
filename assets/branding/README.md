# FluentTB identity

The original FluentTB logo is the active identity again. `FluentTB-original.png` comes from the original repository asset; `FluentTB-master.png` is its export source copy.

Run `./Build-Icons.ps1` on Windows to generate PNG and ICO sizes from the original logo. No Node.js dependency is needed for this active workflow.

The original theme-specific tray files supplied in the workspace's `design` folder are preserved in `tray/TrayDark.png` and `tray/TrayLight.png`. The build copies these unchanged to the desktop resources and creates the engine's ICO wrappers from the same PNG bytes. They are independent of the coloured app icon and are not overwritten with it.

`FluentTB.svg`, `render-svg.cjs`, and their npm manifest/lockfile retain the rejected vector proposal for historical reference only. They are not consumed by the application or the active icon build.
