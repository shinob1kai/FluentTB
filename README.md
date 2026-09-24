# FluentTB

**Customize your Windows 11 taskbar with rounded corners, margins and dynamic segments — with optional lock-key notifications.**

[![Version](https://img.shields.io/badge/version-2026.3.11.0-blue)](https://github.com/shinob1kai/FluentTB/releases/latest)
[![Windows 11](https://img.shields.io/badge/platform-Windows%2011-0078D4)](https://www.microsoft.com/windows)
[![License](https://img.shields.io/badge/license-GPL--3.0--or--later-green)](LICENSE)

This is the **Public edition** of FluentTB, developed by **Shinob1Kai** as a continuation of RoundedTB. It includes taskbar shaping and the lock-key flyout. Transparency and blur can be provided by the separate TranslucentTB application.

## ✨ Features

- **Rounded Corners** — Customize the taskbar corner radius.
- **Custom Margins** — Adjust the overall spacing or individual top, bottom, left and right margins.
- **Basic & Advanced Modes** — Use simple controls or fine-tune each margin independently.
- **Dynamic Mode** — Fit visible taskbar segments around app icons, with support for left and centered Windows alignment.
- **Show/Hide System Tray** — Control tray visibility, including the Win+F2 shortcut.
- **TranslucentTB Compatibility** — A compatibility option for use alongside TranslucentTB's transparency and blur.
- **Fill on Maximize** — Expand the taskbar when a window is maximized; also supports filling during task switching.
- **Multiple Monitors** — Monitor-aware taskbar geometry and DPI handling.
- **Optional Taskbar Shaping** — Disable taskbar shaping while continuing to use the lock-key flyout.
- **Lock-Key Flyout** — Show Caps Lock, Num Lock and Scroll Lock status, plus an Insert keypress notification. Insert reports a keypress, not an application's insert/overwrite mode.
- **Background Startup** — Launch quietly into the notification area; open Settings from the tray icon.
- **Translations** — Taskbar settings are available in all 29 offered app languages.

## 📥 Download

Download **FluentTB Public 2026.3.11.0** from [GitHub Releases](https://github.com/shinob1kai/FluentTB/releases/latest):

| Download | Purpose |
| --- | --- |
| [FluentTB-Public-2026.3.11.0-x64-Setup.exe](https://github.com/shinob1kai/FluentTB/releases/download/v2026.3.11.0/FluentTB-Public-2026.3.11.0-x64-Setup.exe) | Recommended EXE installer; launches the same MSI installation wizard. |
| [FluentTB-Public-2026.3.11.0-x64.msi](https://github.com/shinob1kai/FluentTB/releases/download/v2026.3.11.0/FluentTB-Public-2026.3.11.0-x64.msi) | Direct Windows Installer package. |
| [Exact build source ZIP](https://github.com/shinob1kai/FluentTB/releases/download/v2026.3.11.0/FluentTB-Public-2026.3.11.0-source.zip) | Archived source used to build the installers, including its hash manifest. |
| [SHA256SUMS.txt](https://github.com/shinob1kai/FluentTB/releases/download/v2026.3.11.0/SHA256SUMS.txt) | Checksums for the installers and exact-source archive. |

The MSI and EXE are currently not code-signed; Windows may show an unknown publisher.

This release targets **Windows 11 x64** and includes its .NET runtime. There is no portable application package for this version. GitHub's automatic “Source code” archives contain source, not a portable executable.

## 🚀 Installation

### Using the Installer (Recommended)

1. Download the Public EXE installer above.
2. Run it and follow the Windows Installer wizard, including the license agreement.
3. Launch **FluentTB** from the Start menu.
4. Open Settings from the FluentTB tray icon. It may be under **Show hidden icons**.

### Using MSI

1. Download the Public MSI above.
2. Double-click it and complete the installation wizard.
3. Launch FluentTB from the Start menu and open Settings from the tray icon.

The application creates its per-user settings directory automatically on first launch. Existing settings are retained during updates. A newer Public MSI upgrades the previous Public installation; Public and Dev use separate installer identities. Run only one edition at a time.

## 📖 Usage

### Basic Margin Mode

1. Open **Taskbar shape** in Settings and enable shaping.
2. Adjust **Margin** and **Corner radius**.
3. Click **Apply**.

### Advanced Margin Mode

1. Enable the advanced margin controls.
2. Set top, bottom, left and right spacing individually.
3. Click **Apply** and check the result on each monitor.

### Dynamic Mode

Enable dynamic mode to fit taskbar segments around the app icons. Configure tray and widget visibility as needed. For transparency or blur, configure TranslucentTB separately and enable the FluentTB compatibility option.

### Lock-Key Flyout

Open the lock-key flyout settings and choose which keys should show notifications. Num Lock displays its current on/off status. Insert displays a keypress because editing mode is controlled by individual applications.

### Keyboard Shortcuts

- **Win+F2** — Toggle system tray visibility.

## 🛠️ Building from Source

### Prerequisites

- Windows 11 and the **.NET 10 SDK**.
- For MSI packaging: **WiX Toolset 3.14**, at the path checked by the build script.
- For the EXE wrapper: **Inno Setup**, with `ISCC.exe` on PATH.
- For MSIX packaging: the **Windows SDK**, including `makeappx.exe`.

### Build Steps

```powershell
# Clone the latest public source
 git clone https://github.com/shinob1kai/FluentTB.git
 cd FluentTB

# Build the application
 dotnet build FluentTB.slnx -c Release -p:Platform=x64

# Create Public MSI, EXE and MSIX packages
 ./src/Installer/Build-Editions.ps1
```

The source tree is self-contained. The build creates a versioned output directory beside the checkout: `../Outputs/<version>/`. Its `src/` folder preserves the source used to build the installers. Changed source requires a new version if that version's snapshot already exists.

See [build and release details](docs/BUILDING.md) for settings paths, tests, MSI upgrade identities and archive rules.

## 📂 Project Structure

```text
FluentTB/                         # Git checkout (locally also named Source/)
├── src/
│   ├── FluentTB.Public/          # Public WPF application and lock-key flyout
│   ├── FluentTB/                 # Taskbar engine and settings persistence
│   ├── FluentTB.Desktop/         # Linked taskbar UI/resources only; no Dev host
│   ├── Shared/                   # Keyboard and release helpers
│   └── Installer/                # Installer, license and source archive scripts
├── tests/                        # Geometry, settings, archive and package checks
├── licenses/                     # Preserved component license notices
├── Directory.Build.props         # Application version
├── edition.json                  # Public edition identity
├── LICENSE                       # GNU GPL v3 text
├── THIRD_PARTY_NOTICES.md         # Credits and component notices
├── README.md
└── VERSION.txt
```

## 🌿 Editions & Branches

- **[main](https://github.com/shinob1kai/FluentTB/tree/main)** — Latest public source.
- **[release/2026.3.11.0](https://github.com/shinob1kai/FluentTB/tree/release/2026.3.11.0)** — Preserved source branch for this public version.
- **[Dev-Edition](https://github.com/shinob1kai/FluentTB/tree/Dev-Edition)** — Separate source-only edition with the full FluentFlyout integration, media widgets, audio visualization and additional flyouts. Build it yourself; Dev installers are not offered as public downloads.

## 🐛 Known Issues & Testing

Taskbar behavior can differ between Windows builds, Explorer replacements, auto-hide and mixed-DPI monitor configurations. Not every combination has been tested. TranslucentTB compatibility also depends on the active Windows/taskbar setup.

Version 2026.3.11.0 passed 165 core assertions, plus package, license-dialog and source-snapshot verification. Fresh-profile storage initialization was tested in isolated directories; a full clean-machine installation still needs confirmation.

When reporting a problem, include the Windows build, FluentTB version, monitor/DPI configuration, taskbar alignment, auto-hide state and whether TranslucentTB is running.

## 🤝 Contributing

Contributions are welcome! Submit focused pull requests and include relevant tests. Target `main` for Public changes and `Dev-Edition` for Dev-only changes. Keep installer output, local settings and signing keys out of commits.

## 📝 License

The current combined application includes FluentFlyout-derived GPL code and is distributed under **GPL-3.0-or-later**. See [LICENSE](LICENSE) and [component notices](THIRD_PARTY_NOTICES.md).

The original FluentTB [MIT notice](licenses/FluentTB-MIT.txt) is preserved. It does not replace the license of the combined application. The interactive MSI installer includes the complete license text and requires acknowledgement before continuing.

## 👤 Author

**Shinob1Kai** — creator of FluentTB, the continuation of RoundedTB.

- GitHub: [@shinob1kai](https://github.com/shinob1kai)

## 🙏 Acknowledgments

- [RoundedTB](https://github.com/torchgm/RoundedTB) by torchgm and contributors — original taskbar architecture.
- [FluentFlyout](https://github.com/unchihugo/FluentFlyout) by Hugo Li (unchihugo) and contributors — adapted UI, translations and flyout components. This is separate from the unrelated FluentFlyouts application.
- WPF-UI and its maintained fork, Hardcodet.NotifyIcon.Wpf, Newtonsoft.Json and other dependencies retain their respective notices. The current Public UI uses WPF-UI; ModernWPF belonged to the earlier application.
- [TranslucentTB](https://github.com/TranslucentTB/TranslucentTB) — separate taskbar transparency application.

## 📊 Version History

See [GitHub Releases](https://github.com/shinob1kai/FluentTB/releases) for published versions and [VERSION.txt](VERSION.txt) for the local version.

### Current Public Version: 2026.3.11.0

- Independent Public edition with taskbar shaping and lock-key notifications.
- Optional shaping, background startup and updated tray branding.
- Dynamic taskbar geometry and translated settings.
- Correct installer license text and versioned source snapshots.
- **First-start fix:** create the AppData directory before opening configuration/log files; preserve existing settings and tolerate a locked log file.
