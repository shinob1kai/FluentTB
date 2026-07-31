# Microsoft Store Assets Guide

## ⚠️ WICHTIG: Store Assets noch erstellen!

Das MSIX Package wurde erstellt, aber Sie benötigen noch **Store Assets** (Icons/Images).

---

## 📸 Benötigte Assets

### Pflicht (REQUIRED):

| Asset | Größe | Dateiname | Beschreibung |
|-------|-------|-----------|--------------|
| **Square 44x44** | 44x44 px | Square44x44Logo.png | Task Manager Icon |
| **Square 150x150** | 150x150 px | Square150x150Logo.png | Start Menu Tile (mittel) |
| **Wide 310x150** | 310x150 px | Wide310x150Logo.png | Start Menu Tile (breit) |
| **Store Logo** | 50x50 px | StoreLogo.png | Store Listing |

### Optional (aber empfohlen):

| Asset | Größe | Dateiname | Beschreibung |
|-------|-------|-----------|--------------|
| **Large Tile** | 310x310 px | LargeTile.png | Start Menu Tile (groß) |
| **Small Tile** | 71x71 px | SmallTile.png | Start Menu Tile (klein) |
| **Splash Screen** | 620x300 px | SplashScreen.png | App Start Screen |

---

## 🎨 So erstellen Sie die Assets:

### Option 1: Aus FluentTB.ico erstellen (Empfohlen)

**Vorhanden:** `src/FluentTB/res/FluentTB.ico`

**Mit Paint.NET / GIMP / Photoshop:**

1. Öffnen Sie `FluentTB.ico`
2. Resize zu benötigter Größe
3. Speichern als PNG
4. Kopieren nach `src/Installer/Output/MSIX/PackageFiles/Assets/`

### Option 2: Online Tool verwenden

**PWA Image Generator:**
https://www.pwabuilder.com/imageGenerator

1. Upload FluentTB.ico
2. Select "Windows" Platform
3. Download alle Größen
4. Umbenennen + Kopieren

### Option 3: Automatisches Tool

```powershell
# Windows SDK Tool (falls installiert)
MakePri createconfig /cf priconfig.xml /dq en-US
MakePri new /pr . /cf priconfig.xml
```

---

## 📁 Asset Platzierung

**Ziel-Ordner:**
```
src/Installer/Output/MSIX/PackageFiles/Assets/
```

**Sollte enthalten:**
```
Assets/
├── Square44x44Logo.png      (44x44)
├── Square150x150Logo.png    (150x150)
├── Wide310x150Logo.png      (310x150)
├── LargeTile.png            (310x310)
├── SmallTile.png            (71x71)
├── StoreLogo.png            (50x50)
└── SplashScreen.png         (620x300)
```

---

## 🎯 Design Richtlinien

### Farben:
- **Hintergrund:** Transparent ODER #0078D4 (Windows Blue)
- **Icon:** Weiß oder kontrastreich
- **Ränder:** Keine (full bleed)

### Format:
- **Dateiformat:** PNG-24 mit Alpha-Kanal
- **Farbprofil:** sRGB
- **Kompression:** Verlustfrei

### Inhalt:
- ✅ Klares, erkennbares Icon
- ✅ Funktioniert auf hellem UND dunklem Hintergrund
- ❌ Kein Text (außer Logo-Text)
- ❌ Keine detaillierten Grafiken (zu klein)

---

## 🔧 Nach Asset-Erstellung:

### Schritt 1: Assets kopieren

```powershell
# Kopieren Sie alle erstellten Assets nach:
Copy-Item "Ihre-Assets\*.png" -Destination "src\Installer\Output\MSIX\PackageFiles\Assets\"
```

### Schritt 2: Neues ZIP erstellen

```powershell
cd src\Installer\Output\MSIX
Remove-Item "FluentTB-Store-Package.zip" -Force

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory(
    "PackageFiles", 
    "FluentTB-Store-Package.zip"
)
```

### Schritt 3: Zum Partner Center hochladen

1. Gehen Sie zu: https://partner.microsoft.com/dashboard
2. Ihre App → **Neue Übermittlung**
3. **Packages** → Upload `FluentTB-Store-Package.zip`
4. Microsoft erstellt automatisch MSIX daraus

---

## ✅ Checkliste vor Upload:

- [ ] Alle 4 Pflicht-Assets erstellt (44x44, 150x150, 310x150, 50x50)
- [ ] Assets kopiert nach `PackageFiles/Assets/`
- [ ] PNG-Format mit transparentem Hintergrund
- [ ] Neues ZIP erstellt
- [ ] ZIP Größe < 100 MB
- [ ] AppxManifest.xml enthält korrekten Publisher Name
- [ ] Version stimmt (2026.3.1.0)

---

## 🚀 Alternative: Visual Studio nutzen

Wenn Sie Visual Studio haben:

1. Öffnen Sie FluentTB.sln in Visual Studio 2022
2. Rechtsklick auf Projekt → **Publish**
3. Wählen Sie **Microsoft Store**
4. Folgen Sie dem Wizard
5. Visual Studio erstellt Assets automatisch (basic)

---

## 💡 Tipp: Placeholder Assets

**Für schnellen Test können Sie temporär:**

1. FluentTB.ico zu allen Größen resizen
2. Alle mit gleichem Bild (nicht ideal, aber funktioniert)
3. Upload zum Store
4. Später durch professionelle Assets ersetzen

---

## 📞 Hilfe benötigt?

**Asset Design Service (bezahlt):**
- Fiverr: $5-20 für App Icon Set
- 99designs: $299+ für professionelles Design
- Upwork: $50-200 je nach Designer

**Kostenlose Alternativen:**
- Canva (mit Templates)
- Microsoft Designer (AI-powered)
- GIMP (Open Source)

---

## 📊 Beispiel Assets

**Von FluentTB.ico ableiten:**

```
┌─────────────────┐
│                 │
│   [FluentTB]    │  ← Ihr bestehendes Icon
│      Icon       │
│                 │
└─────────────────┘
        ↓
   Resize zu allen Größen
        ↓
┌──────────────────────────┐
│ 44x44, 150x150, 310x150  │
│ 310x310, 71x71, 50x50    │
│ 620x300 (Splash)         │
└──────────────────────────┘
```

---

## ⚠️ Häufige Fehler:

| Problem | Lösung |
|---------|--------|
| **Assets nicht gefunden** | Pfad prüfen: `Assets/*.png` |
| **Falsche Größe** | Exakte Pixel-Größe einhalten |
| **Falsches Format** | PNG mit Alpha-Kanal verwenden |
| **Zu große Datei** | PNG komprimieren (TinyPNG.com) |

---

## 🎉 Nach erfolgreicher Asset-Erstellung:

Ihr Package ist **KOMPLETT** und bereit für:
✅ Microsoft Store Submission
✅ App Certification
✅ Veröffentlichung

**Siehe:** `MICROSOFT-STORE-GUIDE.md` für Upload-Anleitung!
