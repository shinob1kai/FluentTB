# FluentTB 2026.3.2.0 — lokale Release-Builds

Stand: 14. September 2026. Windows 11, x64, .NET 10, self-contained.
Es wurde nichts auf GitHub oder im Microsoft Store veröffentlicht.

## Ausgaben und Dateien

| Ausgabe | Inhalt | Ausgabeformate |
| --- | --- | --- |
| Public | Taskleistenform und Sperrtasten-Flyout, Startseite, Spracheinstellungen und Komponentenhinweise | MSI, EXE-Installer, MSIX |
| Private | Bestehender kompletter FluentFlyout-Merge mit Medien-/Lautstärke-Flyouts, Widget, Visualisierung und nächstem Titel | Nur MSI |

Pakete: `src/Installer/Output/v2026.3.2.0/Public/` und `Private/`.
`SHA256.json` enthält die Prüfsummen. Die privaten Pakete werden nicht nach `docs/assets` kopiert.

Der EXE-Installer startet das enthaltene öffentliche MSI. Beide verwenden dieselbe MSI-Installationsidentität. Private hat einen eigenen Upgrade-Code und den Installationsordner `FluentTB Private`.
Der gemeinsame Mutex `FluentTB` verhindert gleichzeitige Taskleisten-Manipulation durch beide Ausgaben. Vor dem Wechsel die laufende Ausgabe über ihr Tray-Menü beenden; ein zweiter Start öffnet sonst die Einstellungen der laufenden Instanz.

Das MSIX ist **unsigniert**. Es kann ohne passende Signierung nicht regulär installiert werden. Publisher im Manifest: `CN=Shinob1Kai`. Für die Weitergabe muss ein passendes Herausgeberzertifikat verwendet werden; es wurde kein Zertifikat erzeugt oder in den Windows-Vertrauensspeicher importiert. Auch EXE/MSI haben keine Authenticode-Signatur.

## Oberfläche und Version

- Beide Startseiten zeigen die tatsächliche Assembly-Version und Build-Konfiguration. Fehlende MSIX-Paketidentität wird nicht mehr als Debug-Build interpretiert.
- Updateprüfung gegen `shinob1kai/FluentTB/releases/latest`, mit Fehlerstatus bei fehlgeschlagenen Anfragen. Kein automatisches Ersetzen der privaten Ausgabe durch einen öffentlichen Download.
- Startseite vor Taskleistenform; Public enthält keine Mediennavigation oder Medien-Abhängigkeiten.
- Shinob1Kai ist der FluentTB-Autor. Erforderliche Hinweise zu RoundedTB/FluentFlyout stehen weiterhin in den Komponentenhinweisen und Lizenzdateien.
- Der Standardabstand des privaten Medien-Widgets bleibt -10 px; bereits gespeicherte Abstände bleiben erhalten.
- Ursprüngliches FluentTB-Logo und ursprüngliche Tray-Symbole.

Versionsquelle: `Directory.Build.props`. `Update-Version.ps1 -NewVersion 2026.3.3.0` aktualisiert auch die manuelle AssemblyInfo des Taskleistenmoduls. MSI hat eigene begrenzte Versionsfelder: `2026.3.2.0` wird intern als `26.3.200` geführt (Jahr minus 2000, Quartal, Build × 100 + Revision). EXE, Assemblies, UI und MSIX tragen `2026.3.2.0`. Revisionen müssen unter 100 bleiben, damit die MSI-Reihenfolge eindeutig bleibt.

## Nachbauen

```powershell
./src/Installer/Build-Editions.ps1
# Optional: -Edition Public oder -Edition Private
```

Benötigt .NET-10-SDK, Inno Setup, WiX 3.14 und Windows SDK (MakeAppx). Build-Installer.ps1 ist ein kompatibler Einstieg in diesen neuen Ablauf. Der Build stoppt bei Fehlern; MSI-ICE- und MSIX-Validierung werden nicht deaktiviert.
Alte `FluentTB.wxs`/`FluentTB-Setup.iss` gehören zum vorherigen Packaging-Ablauf und werden vom neuen Builder nicht verwendet.

## Prüfungen und Grenzen

- 130 bestandene Prüfungen für Geometrie, Settings-Kopien, Migration und JSON.
- 18 bestandene Prüfungen für sofortige Aktualisierung aller sechs NumberBox-Bindings.
- 27 Taskleisten-Texte in 29 Sprachdateien strukturell geprüft; keine sprachliche Begutachtung durch Muttersprachler.
- Beide Home-Seiten und die Navigation live geprüft. Öffentliche Taskleisten-Seite zeigt die geladenen Werte 3 und 7. Live-Updateprüfung erfolgreich.
- Sperrtasten-Anzeige mit Feststelltaste live sichtbar geprüft. Windows-Tastenstatus nach den Tests zurückgeschaltet. Verhalten unter exklusivem Direct3D-Vollbild folgt dem ursprünglichen FluentFlyout-Ansatz.
- `tests/Release-Editions.Tests.ps1`: MSI-Inhalt und Identitäten, App-Dateiversionen, Lizenzen, MSIX-Payload und Version, Abwesenheit von NAudio/WindowsMediaController in Public sowie gewünschte Paketformate.
- Private übernimmt vorhandene Compiler-Warnungen aus der Integration (50 Warnmeldungen im letzten vollständigen Publish, einschließlich WPF-Zwischenkompilierung). Public ließ sich ohne Warnungen veröffentlichen.
- Installer wurden gebaut und validiert, aber nicht auf diesem Arbeitsrechner installiert/deinstalliert. Clean-VM-Installation, Upgrades älterer Inno/MSI-Installationen, signiertes MSIX, ARM64 sowie vollständige gemischte-DPI-/Auto-Hide-/Explorer-Neustart-/TranslucentTB-Matrizen bleiben gesonderte Abnahmetests.

Die ursprüngliche Canary-Featureinterpretation und die umfassende Stabilitätsprüfung aus den früheren Projektphasen werden durch diese Editionstrennung nicht als erledigt erklärt.
