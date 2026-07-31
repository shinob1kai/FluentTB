# FluentTB Auto-Update System

## ✨ Features

FluentTB hat ein eingebautes Auto-Update System, das:

- ✅ **Automatisch prüft** - Einmal pro Tag beim Start
- ✅ **GitHub Releases nutzt** - Keine eigene Server-Infrastruktur nötig
- ✅ **Silent Check** - Stört nicht, wenn keine Updates vorhanden
- ✅ **Ein-Klick Update** - Download + Installation automatisch
- ✅ **Version Tracking** - Zeigt immer verfügbare Updates an
- ✅ **Manueller Check** - "Updates" Button im Hauptfenster

---

## 🔧 Wie es funktioniert

### 1. Automatischer Check (beim Start)

```csharp
// Prüft einmal pro Tag automatisch
if (UpdateManager.ShouldCheckForUpdates())
{
    var update = await UpdateManager.CheckForUpdatesAsync();
    
    if (UpdateManager.IsNewerVersion(update.TagName))
    {
        UpdateManager.ShowUpdateNotification(update);
    }
}
```

**Was passiert:**
1. App startet
2. Prüft, ob letzte Überprüfung > 24 Stunden her
3. Fragt GitHub Releases API ab
4. Vergleicht Version
5. Zeigt Notification bei neuer Version

### 2. Manueller Check

**UI Button:**
- Location: Hauptfenster
- Button Text: "Updates"
- Funktion: `checkUpdateButton_Click`

**Was passiert:**
1. Benutzer klickt "Updates"
2. Button zeigt "Checking..."
3. Kontaktiert GitHub API
4. Zeigt Ergebnis:
   - ✅ Update verfügbar → Download-Dialog
   - ℹ️ Aktuell → "You're up to date"
   - ❌ Fehler → Fehlermeldung

### 3. Download & Installation

```csharp
await UpdateManager.DownloadAndInstallUpdate(update);
```

**Ablauf:**
1. Findet EXE Installer in GitHub Release Assets
2. Downloaded zu `%TEMP%\FluentTB-Setup-2026.3.1.exe`
3. Zeigt Progress Bar (MB/MB)
4. Fragt Benutzer: "Ready to Install?"
5. Startet Installer
6. Schließt FluentTB automatisch

---

## 📁 Dateien

### UpdateManager.cs
Location: `src/FluentTB/UpdateManager.cs`

**Klassen:**
- `UpdateManager` - Haupt-Update-Logik
- `UpdateInfo` - GitHub Release Info
- `UpdateAsset` - Release Asset (EXE, MSI, ZIP)
- `UpdateProgressWindow` - Download Progress UI

**Methoden:**

| Methode | Beschreibung |
|---------|--------------|
| `CheckForUpdatesAsync()` | Prüft GitHub für neue Releases |
| `IsNewerVersion()` | Vergleicht Versionen |
| `GetCurrentVersion()` | Aktuelle App-Version |
| `ShowUpdateNotification()` | Zeigt Update-Dialog |
| `DownloadAndInstallUpdate()` | Download + Install |
| `ShouldCheckForUpdates()` | Prüft 24h Intervall |
| `SaveLastUpdateCheck()` | Speichert letzten Check |

### MainWindow.xaml
- ✅ Update Button hinzugefügt
- Position: Links neben "Help & About"
- Event: `checkUpdateButton_Click`

### MainWindow.xaml.cs
- ✅ Event Handler hinzugefügt
- ✅ Auto-check beim Start (kann hinzugefügt werden)

---

## 🌐 GitHub API Integration

### Endpoint
```
https://api.github.com/repos/shinob1kai/FluentTB/releases/latest
```

### Response Format
```json
{
  "tag_name": "v2026.3.1",
  "name": "FluentTB v2026.3.1",
  "body": "Release notes...",
  "html_url": "https://github.com/shinob1kai/FluentTB/releases/tag/v2026.3.1",
  "published_at": "2026-07-31T12:00:00Z",
  "assets": [
    {
      "name": "FluentTB-Setup-2026.3.1.exe",
      "browser_download_url": "https://github.com/.../FluentTB-Setup-2026.3.1.exe",
      "size": 12000000
    }
  ]
}
```

### Headers
```
User-Agent: FluentTB-UpdateChecker
Accept: application/vnd.github.v3+json
```

---

## 💾 Lokale Speicherung

### update-check.json
Location: `%LOCALAPPDATA%\FluentTB\update-check.json`

**Format:**
```json
{
  "LastCheck": "2026-07-31T14:30:00Z"
}
```

**Zweck:**
- Speichert letzten Update-Check Zeitstempel
- Verhindert zu häufige API-Abfragen
- 24-Stunden Cooldown

---

## 🔢 Versionierung

### Format: `YEAR.QUARTER.BUILD`

Beispiele:
- `2026.3.1` = Jahr 2026, Q3, Build 1
- `2026.4.2` = Jahr 2026, Q4, Build 2
- `2027.1.1` = Jahr 2027, Q1, Build 1

### Vergleich
```csharp
// Entfernt 'v' Prefix
newVersion = "v2026.3.1".TrimStart('v'); // → "2026.3.1"

// Konvertiert zu System.Version
var currentVersion = new Version("2026.3.1"); // → 2026.3.1.0
var remoteVersion = new Version("2026.4.1");  // → 2026.4.1.0

// Vergleicht
bool isNewer = remoteVersion > currentVersion; // true
```

---

## 📋 GitHub Release Checklist

Wenn Sie ein neues Release erstellen:

### 1. Version in Code aktualisieren

**AssemblyInfo.cs:**
```csharp
[assembly: AssemblyVersion("2026.4.1.0")]
[assembly: AssemblyFileVersion("2026.4.1.0")]
```

**FluentTB.csproj:**
```xml
<Version>2026.4.1</Version>
<AssemblyVersion>2026.4.1.0</AssemblyVersion>
<FileVersion>2026.4.1.0</FileVersion>
```

### 2. Build & Installer erstellen

```powershell
cd src/FluentTB
dotnet build FluentTB.csproj -c Release

cd ../Installer
.\Build-Installer.ps1
```

### 3. GitHub Release erstellen

**Tag:** `v2026.4.1`

**Release Title:** `FluentTB v2026.4.1 - [Beschreibung]`

**Release Notes:** (Markdown)
```markdown
## 🎉 What's New

- Feature 1
- Feature 2
- Bug fix 1

## 📥 Downloads

- **FluentTB-Setup-2026.4.1.exe** - Recommended
- **FluentTB.msi** - MSI Package
- **FluentTB-2026.4.1-Portable.zip** - Portable

## 🐛 Bug Fixes

- Fixed issue #1
- Fixed issue #2
```

**Assets Upload:**
1. ✅ `FluentTB-Setup-2026.4.1.exe`
2. ✅ `FluentTB.msi`
3. ✅ `FluentTB-2026.4.1-Portable.zip`

### 4. Publish

- ✅ Mark als "Latest Release"
- ✅ NICHT als "Pre-release" markieren
- ✅ Publish Release

**Innerhalb 24 Stunden:**
- Alle Benutzer werden benachrichtigt
- Auto-Update Check erkennt neue Version

---

## 🧪 Testing

### Test Auto-Check

```powershell
# Löschen Sie die letzte Check-Zeit
Remove-Item "$env:LOCALAPPDATA\FluentTB\update-check.json"

# Starten Sie FluentTB
cd src\FluentTB\bin\Release
.\FluentTB.exe

# Update Check sollte sofort laufen
```

### Test Manual Check

1. Starten Sie FluentTB
2. Klicken Sie "Updates" Button
3. Sollte zeigen:
   - "Checking..." während Abfrage
   - "Update available" oder "Up to date"

### Test mit Fake Version

**Temporär ändern in AssemblyInfo.cs:**
```csharp
[assembly: AssemblyVersion("2020.1.1.0")] // Alte Version
```

Build → Test → Update sollte erkannt werden

---

## ⚙️ Konfiguration

### Update-Check deaktivieren (optional)

Wenn Benutzer Updates deaktivieren möchten:

**Option 1: Config File**
```json
{
  "AutoUpdateCheck": false
}
```

**Option 2: Registry**
```
HKEY_CURRENT_USER\Software\FluentTB
DisableUpdateCheck = 1
```

*Aktuell nicht implementiert - kann hinzugefügt werden*

---

## 🔒 Sicherheit

### TLS 1.2 Required
```csharp
ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
```

### Signatur-Prüfung
- GitHub HTTPS garantiert Authentizität
- Kein Man-in-the-Middle möglich
- Download direkt von GitHub CDN

### User Confirmation
- Benutzer muss Download bestätigen
- Benutzer muss Installation bestätigen
- Kein automatischer Silent Install

---

## 📊 Analytics (Optional)

**Aktuell NICHT implementiert:**

Könnten hinzufügen:
- Download-Statistik über GitHub API
- Crashreports via Telemetry
- Usage Analytics

**Aber: Datenschutz wichtig!**
- Nur mit Opt-In
- DSGVO-konform
- Transparent dokumentiert

---

## 🐛 Troubleshooting

### "Update check failed"
**Ursachen:**
- ❌ Keine Internetverbindung
- ❌ GitHub API Rate Limit (60 req/hour)
- ❌ GitHub Server down
- ❌ Firewall blockiert

**Lösung:**
- Prüfen Sie Internetverbindung
- Warten Sie 1 Stunde (Rate Limit)
- Versuchen Sie manuellen Download

### "Download failed"
**Ursachen:**
- ❌ Nicht genug Speicherplatz
- ❌ Temp-Ordner nicht beschreibbar
- ❌ Download unterbrochen

**Lösung:**
- Freier Speicherplatz: >50 MB
- Temp-Ordner: `%TEMP%` prüfen
- Manuell herunterladen

### "Install failed"
**Ursachen:**
- ❌ FluentTB läuft noch
- ❌ Keine Admin-Rechte
- ❌ Antivirus blockiert

**Lösung:**
- FluentTB schließen
- Installer "Als Administrator" ausführen
- Antivirus temporär deaktivieren

---

## 🔮 Zukunft

### Geplante Features:

1. **Delta Updates**
   - Nur Differenz herunterladen
   - Schneller + kleiner

2. **Background Updates**
   - Download im Hintergrund
   - Installation beim nächsten Start

3. **Beta Channel**
   - Opt-In für Canary/Beta Builds
   - Frühzeitiges Feedback

4. **Update History**
   - Changelog-Viewer
   - "Was ist neu" Dialog

5. **Rollback**
   - Zurück zur vorherigen Version
   - Bei Problemen

---

## 📚 Ressourcen

**GitHub API Docs:**
- https://docs.github.com/en/rest/releases/releases

**C# WebClient:**
- https://docs.microsoft.com/en-us/dotnet/api/system.net.webclient

**Semantic Versioning:**
- https://semver.org/

---

## ✅ Vorteile

| Feature | Vorteil |
|---------|---------|
| **GitHub Hosting** | Kostenlos, zuverlässig, schnell |
| **Keine eigene Infrastruktur** | Keine Server-Kosten |
| **Transparent** | Benutzer sehen Updates auf GitHub |
| **Versionskontrolle** | Git-basiert |
| **Automatisch** | Kein manueller Check nötig |

---

## 🆚 Alternativen

### Squirrel.Windows
- Pro: Beliebtes Framework
- Con: Zusätzliche Dependency

### ClickOnce
- Pro: Microsoft-Standard
- Con: Komplexe Config

### WinSparkle
- Pro: Etabliert
- Con: C++ Library

### Eigene Lösung (AKTUELL)
- Pro: Volle Kontrolle
- Pro: Einfach
- Pro: Transparent
- Con: Muss selbst warten

---

**FluentTB Update System - Einfach, Transparent, Effektiv!** ✨
