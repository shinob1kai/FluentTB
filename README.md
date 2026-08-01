# FluentTB

**Customize your Windows 11 taskbar with rounded corners, margins, and transparency.**

[![Version](https://img.shields.io/badge/version-2026.3.1-blue.svg)](https://github.com/shinob1kai/FluentTB/releases)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![Windows 11](https://img.shields.io/badge/platform-Windows%2011-0078D4.svg)](https://www.microsoft.com/windows)

<a href="https://aka.ms/AA12ympe" target="_blank" >
	<img src="https://get.microsoft.com/images/en-us%20light.svg" width="200"/>
</a>

## ✨ Features

- **Rounded Corners** - Add customizable corner radius to your taskbar
- **Custom Margins** - Adjust taskbar margins (top, bottom, left, right)
- **Basic & Advanced Modes** - Simple slider or individual margin control
- **Show/Hide System Tray** - Toggle system tray visibility with Win+F2
- **TranslucentTB Compatible** - Works alongside TranslucentTB for transparency
- **Fill on Maximize** - Auto-expand taskbar when windows are maximized
- **Multiple Monitors** - Full support for multi-monitor setups

## 📥 Download

Download the latest version from the [Releases](https://github.com/shinob1kai/FluentTB/releases) page:

- **FluentTB-Setup-2026.3.1.exe** - Windows Installer (Recommended)
- **FluentTB.msi** - MSI Package
- **FluentTB-2026.3.1-Portable.zip** - Portable Version

## 🚀 Installation

### Using the Installer (Recommended)

1. Download `FluentTB-Setup-2026.3.1.exe`
2. Run the installer
3. Launch FluentTB from Start Menu

### Using MSI Package

1. Download `FluentTB.msi`
2. Double-click to install
3. Launch FluentTB from Start Menu

### Portable Version

1. Download `FluentTB-2026.3.1-Portable.zip`
2. Extract to any folder
3. Run `FluentTB.exe`

## 📖 Usage

### Basic Margin Mode

1. Launch FluentTB
2. Use the **Margin** slider to adjust taskbar margins
3. Click **Apply** to save changes

### Advanced Margin Mode

1. Click **Advanced margins...**
2. Set individual margins for each side
3. Use negative values to attach taskbar to screen edges
4. Click **Apply**

### Keyboard Shortcuts

- **Win+F2** - Toggle system tray visibility

## 🛠️ Building from Source

### Prerequisites

- Visual Studio 2022 or .NET SDK 6.0+
- Windows 11 SDK
- InnoSetup 6+ (for EXE installer)
- WiX Toolset 3.14+ (for MSI installer)

### Build Steps

```powershell
# Clone the repository
git clone https://github.com/shinob1kai/FluentTB.git
cd FluentTB

# Build the application
cd src/FluentTB
dotnet build FluentTB.csproj -c Release

# Create installers (optional)
cd ../Installer
.\Build-Installer.ps1
```

## 📂 Project Structure

```
FluentTB/
├── src/
│   ├── FluentTB/           # Main application source
│   └── Installer/          # Installer scripts
├── LICENSE                 # MIT License
├── README.md              # This file
└── VERSION.txt            # Version information
```

## 🐛 Known Issues

- **Dynamic Mode** - Currently disabled, will be fixed in future update
- **Autohide** - Windows autohide is not supported due to flickering
- **Orientation** - Works best with taskbar at top or bottom

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👤 Author

**Shinob1Kai**

- GitHub: [@shinob1kai](https://github.com/shinob1kai)

## 🙏 Acknowledgments

- Based on [RoundedTB](https://github.com/torchgm/RoundedTB) by torchgm
- Inspired by macOS dock behavior
- Uses [ModernWPF](https://github.com/Kinnara/ModernWpf) for UI theming

## 📊 Version History

See [VERSION.txt](VERSION.txt) for detailed version history.

### Current Version: 2026.3.1

- Initial release with RoundedTB UI
- Dynamic mode temporarily disabled
- Config files moved to %LOCALAPPDATA%\FluentTB\

---

**Note:** This is a Windows 11 taskbar customization tool. Use at your own risk.
