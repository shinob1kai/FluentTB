# Privacy Policy for FluentTB

**Effective Date:** July 31, 2026  
**Developer:** Shinob1Kai  
**Contact:** https://github.com/shinob1kai/FluentTB/issues

---

## Overview

FluentTB is a Windows 11 taskbar customization tool that respects your privacy. This Privacy Policy explains how we handle data when you use FluentTB.

---

## Data Collection

**FluentTB does NOT collect, store, or transmit any personal data.**

### What FluentTB Does:

✅ **Local Configuration Storage**
- Stores your taskbar settings in a local configuration file on your device
- Location: `%LOCALAPPDATA%\FluentTB\fluent-tb.json`
- Contains only: margin values, corner radius, transparency settings, checkbox states
- Data never leaves your computer
- You have full control and can delete this file at any time

✅ **Windows API Access**
- Accesses Windows taskbar APIs to apply visual customizations (transparency, color, position)
- Required for the app to function properly
- Uses only official Windows APIs
- No data is collected, logged, or transmitted from these API calls

✅ **Update Checks (Optional)**
- Checks GitHub Releases API for new versions (optional feature)
- Only transmits: HTTP GET request to public GitHub API
- No personal data included in request
- You can disable update checks in settings

✅ **Offline Operation**
- Core functionality works completely offline
- No internet connection required for customization features
- No background data uploads or synchronization

### What FluentTB Does NOT Do:

❌ **No Personal Information Collection**
- Does not collect your name, email, or contact information
- Does not access your Windows account information
- Does not collect device identifiers

❌ **No Usage Tracking**
- Does not track how you use the app
- Does not record button clicks or settings changes
- No analytics or telemetry

❌ **No Personal Data Transmission**
- Does not send personal data to external servers
- Optional update check only queries public GitHub API
- No background data uploads of your settings or usage
- No authentication or user accounts required

❌ **No File Access**
- Does not access your documents, photos, or files
- Only reads/writes its own configuration file
- No permission to access other data

❌ **No Cookies or Tracking**
- Does not use cookies
- Does not use tracking pixels
- Does not use web beacons

---

## Data Storage

All FluentTB settings are stored locally on your device at:

```
C:\Users\[YourUsername]\AppData\Local\FluentTB\fluent-tb.json
```

**This file contains:**
- Margin values (top, bottom, left, right)
- Corner radius setting
- Checkbox states (show tray, fill on maximize, etc.)
- Window position/size preferences

**This file does NOT contain:**
- Personal information
- Passwords or credentials
- Browsing history
- Any identifying information

You can view or delete this file at any time. Deleting it will reset FluentTB to default settings.

---

## Third-Party Services

**FluentTB does not use any third-party tracking or analytics services.**

### Used Services (Optional):

✅ **GitHub API (Optional)**
- Purpose: Check for app updates only
- What's sent: HTTP GET request to `https://api.github.com/repos/shinob1kai/FluentTB/releases/latest`
- No personal data transmitted
- No authentication required
- Can be disabled in settings
- GitHub Privacy Policy: https://docs.github.com/en/site-policy/privacy-policies/github-privacy-statement

### NOT Used:

- ❌ No analytics SDKs (Google Analytics, Mixpanel, etc.)
- ❌ No crash reporting services (Sentry, Crashlytics, etc.)
- ❌ No advertising networks
- ❌ No social media integrations
- ❌ No cloud storage services
- ❌ No user authentication services

---

## Permissions

FluentTB requires the following Windows permissions:

| Permission | Purpose | Data Access | Can be Disabled? |
|------------|---------|-------------|------------------|
| **Taskbar API** | Apply visual customizations | Read/modify taskbar properties only | No (core feature) |
| **Local Storage** | Save your settings | Write to %LOCALAPPDATA%\FluentTB\ only | No (core feature) |
| **System Tray** | Display tray icon | No data access | Yes (minimize to tray optional) |
| **Internet Access** | Check for updates | GitHub API only (optional) | Yes (disable in settings) |
| **Startup** | Launch on Windows start | No data access | Yes (optional) |

### FluentTB does NOT request or use:
- ❌ Camera or microphone access
- ❌ Location access
- ❌ File system access (beyond its own configuration folder)
- ❌ Browser history or bookmarks
- ❌ Contacts or calendar access
- ❌ Clipboard access (beyond standard copy/paste in UI)
- ❌ Screen recording or screenshots
- ❌ USB device access
- ❌ Bluetooth access

---

## Children's Privacy

FluentTB does not knowingly collect data from users of any age, including children under 13.

The app is safe for all ages as it:
- Does not collect any personal information
- Does not have online features
- Does not have chat or social features
- Does not display advertisements

**Age Rating:** Everyone (suitable for all ages)

---

## Microsoft Store

If you downloaded FluentTB from the Microsoft Store, Microsoft may collect:
- Download/install statistics
- Crash reports (if you opt-in to Windows Error Reporting)
- Store reviews and ratings

This data is collected by Microsoft, not by FluentTB. See Microsoft's Privacy Policy:
https://privacy.microsoft.com/

---

## Open Source

FluentTB is **open source software**. You can:

✅ **Review the Code**
- Full source code: https://github.com/shinob1kai/FluentTB
- Verify no data collection
- Audit security and privacy

✅ **Contribute**
- Submit improvements
- Report issues
- Suggest features

✅ **Build from Source**
- Compile your own version
- Ensure no modifications

---

## Updates

FluentTB may receive updates that:
- Fix bugs and security vulnerabilities
- Add new features
- Improve performance and stability
- Enhance Windows 11 compatibility

**Update Policy:**
- ✅ We will never add data collection or tracking in updates
- ✅ Privacy Policy changes will be announced prominently
- ✅ You can review changes on GitHub before updating
- ✅ Updates are optional (except critical security fixes via Microsoft Store)

**Update Methods:**
1. **Microsoft Store:** Automatic updates (managed by Microsoft)
2. **Manual Download:** Check GitHub Releases manually
3. **In-App Update Check:** Optional feature (uses GitHub API)

If we make significant changes to this Privacy Policy, we will:
1. Update the "Effective Date" at the top
2. Announce changes on GitHub: https://github.com/shinob1kai/FluentTB/releases
3. Include changelog in update notes
4. Provide 30-day notice for major privacy changes
5. Allow users to continue using previous version if desired

---

## Data Security

Although FluentTB does not collect personal data, we take security seriously:

✅ **Local Storage Security**
- Settings file uses standard Windows NTFS file permissions
- Only your user account can access the configuration file
- No passwords, credentials, or sensitive data stored
- Configuration file is human-readable JSON (no encryption needed)

✅ **Minimal Network Exposure**
- Only optional GitHub API calls for update checks
- Uses HTTPS (encrypted connection) for all network requests
- No custom servers or backend infrastructure
- No risk of server breaches (no server exists)

✅ **Code Security**
- Open source code available for security audits
- No obfuscation or hidden functionality
- Regular dependency updates for security patches
- Follows secure coding practices

✅ **Windows Security**
- Runs with standard user privileges (no admin required for normal use)
- No driver installation
- No system file modifications
- Uses only official Windows APIs

---

## Your Rights

Since FluentTB does not collect personal data, there is no data to:
- Request access to
- Request deletion of
- Request correction of
- Port to another service

However, you can:
- ✅ Delete the app and all its data at any time
- ✅ View/edit the settings file manually
- ✅ Reset to defaults by deleting the config file

---

## Contact

For questions about this Privacy Policy or FluentTB:

**GitHub Issues:**  
https://github.com/shinob1kai/FluentTB/issues

**GitHub Discussions:**  
https://github.com/shinob1kai/FluentTB/discussions

**Developer:**  
Shinob1Kai

We typically respond within 1-3 business days.

---

## Legal

**Jurisdiction:** This app is developed by an individual developer (Shinob1Kai) and complies with international privacy standards.

**Compliance:**
- ✅ **GDPR** (EU) compliant: No personal data collection, processing, or storage
- ✅ **CCPA** (California) compliant: No data sales or sharing
- ✅ **COPPA** (USA) compliant: No children's data collection
- ✅ **PIPEDA** (Canada) compliant: No personal information handling
- ✅ **DPA** (UK) compliant: No data processing activities

**Data Protection Officer:** Not required (no personal data processing)

**Legal Basis for Processing:** Not applicable (no data processing occurs)

**License:** MIT License  
See: https://github.com/shinob1kai/FluentTB/blob/main/LICENSE

---

## Summary (TL;DR)

✅ **FluentTB is privacy-friendly:**
- No personal data collection
- No tracking or analytics
- Open source (audit the code yourself)
- Settings stored locally only on your device
- Optional update checks only (GitHub API, no personal data sent)
- No user accounts or authentication
- Works offline for all core features

❌ **FluentTB does NOT:**
- Collect, store, or transmit personal information
- Track your usage, behavior, or preferences
- Sell or share your data with anyone
- Use analytics, telemetry, or crash reporting services
- Access your files, documents, or other applications
- Require registration or login
- Display advertisements

---

**Last Updated:** July 31, 2026  
**Version:** 1.0  
**For:** FluentTB v2026.3.1

---

*This Privacy Policy may be updated periodically. Check this page for the latest version.*
