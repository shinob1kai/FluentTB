# FluentTB Installer Scripts

This directory contains installer scripts for FluentTB in multiple formats.

## Available Installers

### 1. PowerShell Installer (.ps1)
**Files:** `Install-FluentTB.ps1`, `Uninstall-FluentTB.ps1`

**Usage:**
```powershell
# Install (requires Administrator)
.\Install-FluentTB.ps1

# Install with options
.\Install-FluentTB.ps1 -CreateDesktopShortcut -AddToStartup

# Uninstall
.\Uninstall-FluentTB.ps1
```

**Features:**
- No additional tools required
- Installs to `C:\Program Files\FluentTB`
- Creates Start Menu shortcuts
- Registers with Windows uninstaller
- Optional desktop shortcut and startup registration

### 2. InnoSetup Installer (.exe)
**File:** `FluentTB-Setup.iss`

**Requirements:**
- Download and install [Inno Setup](https://jrsoftware.org/isdl.php)

**Build Instructions:**
```batch
# Compile the installer
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" FluentTB-Setup.iss
```

**Output:** `Output\FluentTB-Setup-1.0.0.exe`

**Features:**
- Professional Windows installer with GUI
- Automatic process detection and termination
- Uninstaller included
- Multi-language support (English, German)
- Custom installation directory
- Desktop icon and startup options

### 3. WiX Toolset Installer (.msi)
**File:** `FluentTB.wxs`

**Requirements:**
- Download and install [WiX Toolset](https://wixtoolset.org/)

**Build Instructions:**
```batch
# Compile the WiX object
candle FluentTB.wxs

# Link to create MSI
light FluentTB.wixobj -out FluentTB.msi -ext WixUIExtension
```

**Output:** `FluentTB.msi`

**Features:**
- Standard Windows MSI installer
- Group Policy deployment support
- Repair and modify capabilities
- Windows Installer logging
- Corporate environment friendly

## Before Building

1. **Build the application** in Release mode:
   ```batch
   cd FluentTB
   dotnet build FluentTB.csproj -c Release
   ```

2. **Verify build output** exists:
   - Check `FluentTB\bin\Release\` contains `FluentTB.exe` and dependencies

## Installation Paths

- **Application:** `C:\Program Files\FluentTB\`
- **User Data:** `%LOCALAPPDATA%\FluentTB\`
  - Configuration: `fluent-tb.json`
  - Logs: `fluent-tb.log`
  - Crash logs: `FluentTB-crash.log`

## Uninstallation

The uninstallers preserve user data in `%LOCALAPPDATA%\FluentTB\` by default.

To completely remove including settings:
```powershell
Remove-Item -Path "$env:LOCALAPPDATA\FluentTB" -Recurse -Force
```

## Notes

- All installers require Administrator privileges
- User settings are stored separately from the application
- Uninstalling will NOT delete user configuration files
- Compatible with Windows 11 only

## Troubleshooting

**"Build output not found" error:**
- Ensure you've built the project in Release configuration
- Check that `FluentTB\bin\Release\FluentTB.exe` exists

**PowerShell execution policy error:**
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

**WiX build errors:**
- Ensure WiX Toolset is installed and in PATH
- Add `-ext WixUIExtension` to light.exe command for UI support
