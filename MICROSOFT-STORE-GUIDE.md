# Microsoft Store Veröffentlichung - FluentTB

## 🎯 Vorteile des Microsoft Store

### Für Entwickler:
- ✅ **Automatische Code-Signierung** (Microsoft signiert für Sie)
- ✅ **Keine Windows Defender Warnungen**
- ✅ **Kostenlose Distribution**
- ✅ **Automatische Updates** für Benutzer
- ✅ **Eingebaute Analytics**
- ✅ **Zahlungsabwicklung** (falls kostenpflichtig)

### Für Benutzer:
- ✅ **Vertrauenswürdige Quelle**
- ✅ **Ein-Klick-Installation**
- ✅ **Automatische Updates**
- ✅ **Einfache Deinstallation**

---

## 💰 Kosten

### Entwickler-Konto:
- **Einmalig:** $19 USD (für Einzelpersonen)
- **Einmalig:** $99 USD (für Unternehmen)
- **Keine jährlichen Gebühren**

### App-Veröffentlichung:
- **Kostenlos:** Für kostenlose Apps
- **15% Gebühr:** Bei kostenpflichtigen Apps oder In-App-Käufen

---

## 📋 Voraussetzungen

### 1. Microsoft Partner Center Konto
- Registrierung: https://partner.microsoft.com/dashboard
- Zahlung: $19 USD (Kreditkarte, PayPal)
- Verifikation: 1-3 Tage

### 2. App-Anforderungen
- ✅ Windows 10/11 kompatibel (HABEN WIR)
- ✅ .NET Framework 4.8 oder höher (HABEN WIR)
- ✅ Keine schädlichen Inhalte (HABEN WIR)
- ✅ Datenschutzrichtlinie (MÜSSEN WIR ERSTELLEN)
- ✅ Screenshots & Beschreibung (MÜSSEN WIR ERSTELLEN)

---

## 🚀 Schritt-für-Schritt Anleitung

### Phase 1: Vorbereitung (JETZT)

#### 1.1. MSIX Package erstellen

**Option A: Automatisch mit Visual Studio**

1. Öffnen Sie das Projekt in Visual Studio 2022
2. Rechtsklick auf Projekt → **Veröffentlichen** → **Microsoft Store**
3. Folgen Sie dem Wizard

**Option B: Manuell mit Windows SDK**

Ich erstelle Ihnen ein Skript:

\`\`\`powershell
# MSIX Package erstellen
cd src/FluentTB
.\Create-MSIX-Package.ps1
\`\`\`

#### 1.2. App-Metadaten vorbereiten

**Benötigte Informationen:**

| Feld | Wert | Status |
|------|------|--------|
| **App Name** | FluentTB | ✅ |
| **Publisher** | Shinob1Kai | ✅ |
| **Version** | 2026.3.1.0 | ✅ |
| **Kurzbeschreibung** | Customize your Windows 11 taskbar | ✅ |
| **Lange Beschreibung** | [siehe unten] | 📝 |
| **Keywords** | taskbar, customization, Windows 11, rounded | ✅ |
| **Kategorie** | Utilities & tools | ✅ |
| **Altersfreigabe** | Everyone | ✅ |
| **Datenschutzrichtlinie URL** | [müssen wir erstellen] | ❌ |
| **Support Email** | [Ihre Email] | ❌ |

#### 1.3. Screenshots erstellen

**Benötigt:**
- ❌ 3-10 Screenshots (1280x720 oder 1920x1080)
- ❌ Optional: Trailer Video
- ❌ Optional: App Icon (300x300 PNG)

**Screenshot-Guide:**
1. Starten Sie FluentTB
2. Öffnen Sie Einstellungen
3. Drücken Sie Win+Shift+S
4. Speichern Sie als PNG

---

### Phase 2: Registrierung (TAG 1)

#### 2.1. Partner Center Konto erstellen

1. Gehen Sie zu: https://partner.microsoft.com/dashboard
2. Klicken Sie auf **"Registrieren"**
3. Wählen Sie **"Einzelkonto"** ($19 USD)
4. Füllen Sie aus:
   - Name: [Ihr Name oder Shinob1Kai]
   - Email: [Ihre Email]
   - Land: Deutschland
   - Steuernummer: [falls vorhanden]
5. Zahlung: Kreditkarte oder PayPal
6. Warten Sie auf Verifizierung (1-3 Tage)

#### 2.2. Publisher-Name reservieren

1. Nach Verifizierung: Dashboard → **"Apps und Spiele"**
2. Klicken Sie auf **"Neue App"**
3. Reservieren Sie: **"Shinob1Kai"** oder **"FluentTB"**

---

### Phase 3: App-Vorbereitung (TAG 2-3)

#### 3.1. MSIX Package erstellen

Ich erstelle Ihnen ein fertiges Skript:

**Datei:** \`src/FluentTB/Create-MSIX-Package.ps1\`

\`\`\`powershell
# Erstellt MSIX Package für Microsoft Store
.\Create-MSIX-Package.ps1
\`\`\`

**Ausgabe:**
- \`FluentTB_2026.3.1.0_x64.msix\` (Store Package)
- \`FluentTB_2026.3.1.0_x64.msixbundle\` (Multi-Arch Bundle)

#### 3.2. Datenschutzrichtlinie erstellen

**Benötigt:** Eine Webseite mit Datenschutzrichtlinie

**Option A: GitHub Pages (Kostenlos)**

1. Erstellen Sie \`PRIVACY-POLICY.md\` im Repo
2. Aktivieren Sie GitHub Pages
3. URL: \`https://shinob1kai.github.io/FluentTB/PRIVACY-POLICY.html\`

**Option B: Google Sites (Kostenlos)**

1. https://sites.google.com
2. Erstellen Sie neue Site
3. Fügen Sie Datenschutztext ein

**Vorlage:** [siehe unten]

#### 3.3. Screenshots & Grafiken

**Benötigte Dateien:**

1. **App Icon** (300x300 PNG)
   - Verwenden Sie: \`src/FluentTB/res/FluentTB.ico\`
   - Konvertieren zu PNG

2. **Hero Image** (1920x1080 PNG) - Optional
   - Zeigt App in Aktion

3. **Screenshots** (mindestens 3)
   - Empfohlen: 1920x1080 PNG
   - Zeigen Sie:
     1. Hauptfenster mit Einstellungen
     2. Advanced Margins Mode
     3. Taskbar mit angewendeten Effekten

---

### Phase 4: Store Submission (TAG 4)

#### 4.1. App-Listing erstellen

1. Partner Center → **"Neue Übermittlung"**
2. Füllen Sie aus:

**Verfügbarkeit:**
- ✅ Alle Märkte
- ✅ Kostenlos

**Eigenschaften:**
- Kategorie: **"Utilities & tools"**
- Unterkategorie: **"System"**

**Altersfreigaben:**
- Automatisch: **"Everyone"**

**Packages:**
- Laden Sie \`FluentTB_2026.3.1.0_x64.msix\` hoch
- System erkennt automatisch: x64, ARM64 (falls kompiliert)

**Store-Einträge:**

**Deutsch:**
- **Titel:** FluentTB - Taskleisten Anpassung
- **Kurz:** Passen Sie Ihre Windows 11 Taskleiste an
- **Lang:** [siehe Vorlage unten]

**Englisch:**
- **Titel:** FluentTB - Taskbar Customization
- **Kurz:** Customize your Windows 11 taskbar
- **Lang:** [siehe Vorlage unten]

**Screenshots:**
- Laden Sie 3-10 Screenshots hoch
- Optional: Trailer Video

#### 4.2. Zertifizierung einreichen

1. Überprüfen Sie alle Felder
2. Klicken Sie auf **"An Store übermitteln"**
3. Warten Sie auf Review (1-3 Tage)

---

## 📝 Vorlagen

### Datenschutzrichtlinie (PRIVACY-POLICY.md)

\`\`\`markdown
# Privacy Policy for FluentTB

**Effective Date:** July 31, 2026  
**Developer:** Shinob1Kai

## Overview

FluentTB is a Windows 11 taskbar customization tool. We respect your privacy and are committed to protecting your personal data.

## Data Collection

**FluentTB does NOT collect, store, or transmit any personal data.**

### What FluentTB Does:
- Stores configuration settings locally on your device (\`%LOCALAPPDATA%\\FluentTB\\fluent-tb.json\`)
- Accesses Windows taskbar APIs to apply customizations
- No internet connection required
- No analytics or telemetry

### What FluentTB Does NOT Do:
- ❌ Does not collect personal information
- ❌ Does not track your usage
- ❌ Does not send data to external servers
- ❌ Does not use cookies
- ❌ Does not access your files or documents

## Data Storage

All settings are stored locally on your device at:
\`C:\\Users\\[YourName]\\AppData\\Local\\FluentTB\\fluent-tb.json\`

This file contains only your taskbar customization preferences (margins, corner radius, etc.).

## Third-Party Services

FluentTB does not use any third-party services, SDKs, or APIs that collect data.

## Children's Privacy

FluentTB does not knowingly collect data from children. The app is safe for all ages.

## Changes to This Policy

We may update this Privacy Policy from time to time. Changes will be posted on this page.

## Contact

For questions about this Privacy Policy, contact:
- GitHub: https://github.com/shinob1kai/FluentTB/issues
- Email: [your-email@example.com]

## Open Source

FluentTB is open source. You can review the code at:
https://github.com/shinob1kai/FluentTB

---

**Last Updated:** July 31, 2026
\`\`\`

### Store Beschreibung (Deutsch)

\`\`\`markdown
FluentTB - Passen Sie Ihre Windows 11 Taskleiste an

Verleihen Sie Ihrer Windows 11 Taskleiste einen modernen Look mit abgerundeten Ecken, individuellen Rändern und Transparenz-Unterstützung.

✨ HAUPTFUNKTIONEN

• Abgerundete Ecken - Anpassbarer Eckradius
• Benutzerdefinierte Ränder - Passen Sie alle Seiten individuell an
• Basis & Erweitert Modi - Einfache oder detaillierte Steuerung
• System Tray Toggle - Win+F2 zum Ein-/Ausblenden
• TranslucentTB Kompatibel - Funktioniert mit Transparenz
• Multi-Monitor Support - Alle Bildschirme unterstützt

🎨 ANPASSUNGSMÖGLICHKEITEN

Machen Sie Ihre Taskleiste einzigartig:
- Ränder von 0-48 Pixeln
- Eckradius von 0-48 Pixeln  
- Negative Werte für Edge-Snapping
- Automatisches Erweitern bei Maximierung
- System Tray bei Hover anzeigen

🚀 EINFACHE BEDIENUNG

1. FluentTB starten
2. Einstellungen mit Schiebereglern anpassen
3. "Übernehmen" klicken
4. Fertig!

💡 BEKANNTE EINSCHRÄNKUNGEN

- Dynamischer Modus derzeit deaktiviert (kommt in zukünftiger Version)
- Windows Autohide nicht unterstützt
- Funktioniert am besten mit Taskleiste oben oder unten

🔒 DATENSCHUTZ

- Keine Datenerfassung
- Keine Internet-Verbindung erforderlich
- Alle Einstellungen lokal gespeichert
- Open Source auf GitHub

📖 OPEN SOURCE

FluentTB ist Open Source! Code ansehen:
https://github.com/shinob1kai/FluentTB

Basierend auf RoundedTB von torchgm.
\`\`\`

### Store Beschreibung (Englisch)

\`\`\`markdown
FluentTB - Customize Your Windows 11 Taskbar

Give your Windows 11 taskbar a modern look with rounded corners, custom margins, and transparency support.

✨ KEY FEATURES

• Rounded Corners - Customizable corner radius
• Custom Margins - Adjust all sides individually
• Basic & Advanced Modes - Simple or detailed control
• System Tray Toggle - Win+F2 to show/hide
• TranslucentTB Compatible - Works with transparency
• Multi-Monitor Support - All screens supported

🎨 CUSTOMIZATION OPTIONS

Make your taskbar unique:
- Margins from 0-48 pixels
- Corner radius from 0-48 pixels
- Negative values for edge snapping
- Auto-expand on maximize
- Show system tray on hover

🚀 EASY TO USE

1. Launch FluentTB
2. Adjust settings with sliders
3. Click "Apply"
4. Done!

💡 KNOWN LIMITATIONS

- Dynamic mode currently disabled (coming in future release)
- Windows autohide not supported
- Works best with taskbar at top or bottom

🔒 PRIVACY

- No data collection
- No internet connection required
- All settings stored locally
- Open source on GitHub

📖 OPEN SOURCE

FluentTB is open source! View code:
https://github.com/shinob1kai/FluentTB

Based on RoundedTB by torchgm.
\`\`\`

---

## 🎨 Screenshot-Ideen

1. **Hauptfenster**
   - Zeigen Sie Basic Mode mit Slider
   - Highlight: "Margin" Slider und "Apply" Button

2. **Advanced Mode**
   - Zeigen Sie alle 4 Margin-Felder
   - Zeigen Sie aktivierte Checkboxen

3. **Taskbar Vorher/Nachher**
   - Links: Standard Windows 11 Taskbar
   - Rechts: Mit FluentTB angepasste Taskbar

4. **About Window**
   - Zeigen Sie "Welcome to FluentTB!" Screen

5. **Multi-Monitor**
   - Zeigen Sie FluentTB auf mehreren Bildschirmen

---

## ⏱️ Timeline

| Phase | Dauer | Kosten |
|-------|-------|--------|
| **Registrierung** | 1-3 Tage | $19 USD |
| **Package Erstellung** | 1 Tag | Kostenlos |
| **Screenshots & Grafiken** | 1 Tag | Kostenlos |
| **Store Submission** | 1 Stunde | Kostenlos |
| **Review** | 1-3 Tage | Kostenlos |
| **Veröffentlichung** | Sofort | Kostenlos |
| **GESAMT** | 5-10 Tage | $19 USD |

---

## 🛠️ Nächste Schritte

### Sofort (heute):
1. ✅ Lesen Sie diesen Guide
2. ❌ Entscheiden Sie über $19 USD Investition
3. ❌ Registrieren Sie Partner Center Konto

### Morgen:
1. ❌ Erstellen Sie Screenshots
2. ❌ Erstellen Sie Datenschutzrichtlinie
3. ❌ Erstellen Sie MSIX Package (ich helfe Ihnen)

### Nach Verifizierung:
1. ❌ Laden Sie MSIX hoch
2. ❌ Füllen Sie Store-Listing aus
3. ❌ Reichen Sie zur Review ein

---

## 💡 Tipps

### Erhöhen Sie Ihre Chancen:
- ✅ Gute Screenshots (professionell, hell, klar)
- ✅ Detaillierte Beschreibung
- ✅ Keywords optimieren
- ✅ Datenschutzrichtlinie bereitstellen
- ✅ Support-Email angeben

### Vermeiden Sie:
- ❌ Schlechte Qualität Screenshots
- ❌ Rechtschreibfehler
- ❌ Fehlende Datenschutzrichtlinie
- ❌ Unvollständige Informationen
- ❌ Zu viele Keywords (Spam)

---

## 📞 Support

**Microsoft Partner Center Support:**
- https://partner.microsoft.com/support

**Fragen zu FluentTB:**
- GitHub Issues: https://github.com/shinob1kai/FluentTB/issues

---

## ✅ Checkliste

Vor der Submission:

- [ ] Partner Center Konto erstellt ($19 USD bezahlt)
- [ ] Publisher Name reserviert
- [ ] MSIX Package erstellt
- [ ] 3-10 Screenshots erstellt
- [ ] Datenschutzrichtlinie online
- [ ] Support Email bereitgestellt
- [ ] Store Beschreibung geschrieben (DE + EN)
- [ ] App Icon vorbereitet (300x300 PNG)
- [ ] Alle Tests bestanden
- [ ] README.md mit Store Link aktualisiert

---

**Möchten Sie starten? Ich helfe Ihnen beim MSIX Package!** 🚀
\`\`\`
