# Phase 1: Dynamic-Modus – Zwischenstand vom 12. September 2026

Status: Startblocker und fehlerhafte Erfassung der modernen Taskleiste korrigiert. Der Fehler wurde anhand der Nutzerbilder und Live-Geometrie auf zwei Monitoren reproduziert. Links/zentriert wurden mit dem ersten UIA-Build geprüft; am 12. September kam eine zusätzliche Widgets-Korrektur hinzu. Deren Erfassung ist live geprüft, die vollständige Abnahme des letzten Builds steht noch aus. Phase 2–4 wurden nicht vorgezogen.

## Quellen und Ausgangslage

FluentTB, die als stabile RoundedTB-Referenz bereitgestellte Quelle, fünf Canary-XAML-Dateien und TranslucentTB liegen lokal vor. Der FluentTB-Arbeitsstand enthielt vor Beginn zahlreiche Änderungen, auch an den hier bearbeiteten Dateien. Diese wurden nicht zurückgesetzt. Es wurde kein gemischter Commit aus fremden und neuen Änderungen erstellt.

Der Ausgangsstand baut mit dem installierten .NET SDK 10.0.302 als .NET Framework 4.8 mit 0 Fehlern und 0 Warnungen. Das allein belegt keine Lauffähigkeit.

## Reproduzierter Startblocker

Der ursprüngliche Debug-Build beendet sich beim Laden von App.xaml mit einer PlatformNotSupportedException in Windows.UI.Color.get_A(), aufgerufen durch ModernWpf.ColorsHelper. Beleg: Windows-Anwendungsprotokoll, .NET Runtime, 07.09.2026 17:38:20.

Die expliziten Referenzen verwendeten die plattformneutralen lib/netstandard2.0-Implementierungen von System.Runtime.WindowsRuntime und System.Runtime.WindowsRuntime.UI.Xaml. Die Anwendung muss stattdessen die WinRT-Projektion von .NET Framework verwenden. Die expliziten DLL-Referenzen wurden entfernt, die zusätzlichen NuGet-Referenzinjektionen mit DoNotReferenceWinRT unterbunden und ModernWpfs Assembly-Anforderung 4.0.14.0 auf die installierte Framework-Version 4.0.0.0 umgeleitet.

Danach bleibt der separate Build bin/Phase1/FluentTB.exe aktiv und erreicht den Hintergrund-Loop. Der Nutzer hat den erfolgreichen Start und den Zugriff über die ausgeblendeten Tray-Symbole bestätigt. Dieser laufende Build enthält nur die Startkorrektur, noch nicht den späteren Geometrie-Fix.

## Geometrie-Befunde und Fix

Taskbar.GenerateTaskbarInfo und GetQuickTaskbarRects lesen die Fensterrechtecke. GetVisibleAppButtonBounds versucht über EnumChildWindows, kleinere sichtbare Kindfenster als Icon-Grenzen zu erfassen; ohne Treffer wird das Container-Rechteck verwendet. UpdateDynamicTaskbar erzeugt die GDI-Regionen. Background aktualisiert bei veränderter Taskleisten-, App- oder Tray-Geometrie bzw. Ausrichtungswechsel.

Die bisherige zentrierte Berechnung spiegelte die Grenzen um tbW/2. Beispiel: Taskleiste 1920 Pixel, gemeldete App-Gruppe 750–1100, Sicherheitsabstand 48: Die natürliche gepolsterte Region ist 702–1148. Der Altcode liefert mit Tray 702–1218; ohne Tray bricht er bei Monitorursprung 0 ab. Auf einem Monitor mit negativem Ursprung wurde dasselbe leere Tray-Rechteck dagegen als vorhandener Tray interpretiert. Das ist ein reproduzierter Rechenfehler, keine behauptete Bildschirmbeobachtung.

Die neue reine Methode TryGetDynamicAppBounds folgt den gelieferten App-Grenzen und rechnet je Monitor in relative Koordinaten um. Links bleibt die Region am eingestellten Rand verankert. Die Schätzgrenzen bei 50/75 Prozent und die Spiegelung entfallen. Leere oder außerhalb der Taskleiste liegende App-Rechtecke, ungültige DPI und ein Abschneiden des rechten App-Randes durch den Tray führen zum sicheren Rückfall. Tray-Erkennung beruht auf einem gültigen Rechteck statt dem absoluten Bildschirmwert Left != 0. Ein weiterer Vergleich zwischen App-Breite und absolutem Tray.Left wurde entfernt.

Sowohl ApplyButton_Click als auch der Hintergrund-Loop ignorierten bisher false aus UpdateDynamicTaskbar. Beide wenden jetzt in diesem Fall die volle einfache Region an, statt die alte Clip-Region unverändert stehenzulassen.

## Regressionstests

tests/TaskbarGeometry.Tests.ps1 lädt die gebaute Anwendung unter Windows PowerShell/.NET Framework und prüft deren tatsächliche reine Berechnung per Reflection. Es verändert keine Fenster oder Windows-Einstellungen.

36 Kombinationen: Monitor-X-Ursprung -2560/0/1920, DPI 100/150/200 Prozent, links/zentriert, vorhandenes/leeres Tray-Rechteck. Vor dem Fix: 18 Fehler, ausschließlich zentrierte Fälle. Nach dem Fix: alle 36 bestanden. Zusätzlich sechs ungültige Geometrien: leer, außerhalb links/rechts/oben, Tray-Überlappung, NaN-DPI. Gesamt: 42 bestanden.

Aus dem Repository-Verzeichnis:

```powershell
dotnet build src/FluentTB/FluentTB.csproj -c Debug --no-restore -p:OutputPath=bin/GeometryTests/
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/TaskbarGeometry.Tests.ps1
```

ExecutionPolicy gilt hier nur für den Testprozess; die Windows-Einstellung wird nicht geändert.

## Bestätigte Live-Ursache: alte Win32-Fenster und tatsächliche XAML-Buttons

Die Nutzerbilder zeigen abgeschnittene rechte App-Symbole auf beiden linksbündigen Taskleisten. Die Live-Diagnose bestätigte: MSTaskSwWClass/MSTaskListWClass melden nur 55–231 Pixel; einschließlich 48 Pixel Reserve endet die Region bei 279 Pixeln. Die echten XAML-Buttons reichen im selben Zustand bis 363 Pixel. Figma, ChatGPT und FluentTB liegen teilweise oder vollständig außerhalb der vermeintlichen App-Grenze. Der Fehler betrifft damit ausdrücklich auch Links, nicht nur Zentriert.

TaskbarContentReader liest nun bei moderner Composition-Taskleiste die tatsächlichen UI-Automation-Button-Rechtecke. Start und App-Buttons werden zusammengeführt, SystemTray-Buttons separat. Der zweite Monitor erhält dadurch auch ohne TrayNotifyWnd ein korrektes Uhr-/Tray-Rechteck. UIA läuft auf MTA-ThreadPool-Threads mit CacheRequest; pro Handle ist höchstens eine Abfrage gleichzeitig aktiv. Der Aufrufer wartet höchstens 150 ms, Resultate über 500 ms werden verworfen. Bei fehlenden Daten greift die volle sichere Region. Es werden keine UIA-Event-Abonnements angelegt.

Am 12. September zeigte die Live-Diagnose zusätzlich einen WidgetsButton nahe dem rechten Rand. Dieser verbreiterte die App-Vereinigung fälschlich auf 1703 Pixel und löste den vollen Fallback aus. Der aktuelle Build trennt WidgetsRect von Apps/Tray und führt das Rechteck dem bereits bestehenden ShowWidgets-Codepfad zu. Änderungen daran lösen nun ebenfalls eine Neuberechnung aus. Die neue Erfassung lieferte auf beiden Monitoren korrekt 0–407 relative App-Pixel. Die unveränderte Windows-Konfiguration bleibt linksbündig.

## Live-Testergebnisse und verbleibende Abnahme

tests/TaskbarSnapshot.cs ist ein rein lesendes Testprogramm für die interaktive Sitzung. Es erfasst tatsächliche Button-Rechtecke und prüft deren Mittelpunkte gegen GetWindowRgn/PtInRegion. Das ist eine Prüfung der Clip-/Trefferfläche; kein Ersatz für einen vollständigen visuellen Flacker- oder Klicktest.

| Zustand | Ergebnis |
| --- | --- |
| Links, Dynamic, zwei reale Monitore, erster UIA-Fix (7. September) | 14/9 Schaltflächen geprüft, 0/0 Mittelpunkte außerhalb der gesetzten Regionen |
| Zentriert nach Windows-Umschaltung, gleicher Build | 15/10 Schaltflächen, 0/0 außerhalb; relative App-Grenzen 779–1142 auf beiden Monitoren |
| Auto-Hide ein (12. September, erster UIA-Fix) | Log bestätigt Styling-Pause; Taskleisten sind ausgeblendet, keine unveränderte schmale App-Region wird weitergeschrieben |
| Auto-Hide wieder aus | Log bestätigt Wiederaufnahme; Einstellung anschließend wieder Aus, Ausrichtung Links |
| Widgets sichtbar, letzter Build DynamicFix2 | Erfassung auf beiden Monitoren 0–407 relative App-Pixel statt 0–1703; bestehender Prozess war noch DynamicFix, daher kein Beleg für die bereits angewendete Region des neuen Builds |
| Synthetische Geometrie gegen DynamicFix2 | 42 Fälle bestanden; Build 0 Fehler/0 Warnungen |

Die Messzeiten der vollständigen UIA-Erfassung lagen zuletzt ungefähr zwischen 14 und 40 ms pro Monitor. Das ist eine lokale Stichprobe, kein Leistungsversprechen. Die frühere 5–7-ms-Messung betraf die erste Abfrage ohne die später ergänzten Tray-Geschwister.

Die RoundedTB-README nennt ausdrücklich Issue #98: links bleibt sichtbar, wenn die Ausrichtung nie umgeschaltet wurde. FluentTB liest TaskbarAl beim Anwenden und laufend; ein fehlender Wert wird weiterhin als links interpretiert. Die falschen alten Container-Rechtecke sind jetzt bestätigt und werden auf der modernen Taskleiste nicht mehr verwendet. Eine identische Ursache zu #98 oder ein Erstprofil ohne TaskbarAl ist noch nicht nachgewiesen bzw. getestet.

Aktueller Test-Build: src/FluentTB/bin/DynamicFix2/FluentTB.exe. Am 12. September um 14:02 wurde die noch laufende alte DynamicFix-Instanz durch DynamicFix2 ersetzt. Die neue Instanz läuft mit Dynamic aktiviert und geöffneten erweiterten Einstellungen.

Der anschließende echte Bedienungstest bestätigte den Unterschied auf beiden Monitoren: Dynamic ausgeschaltet und Apply gedrückt ergibt gapIncluded=True; Dynamic wieder eingeschaltet und Apply gedrückt ergibt gapIncluded=False. Gleichzeitig lagen alle 9 Start-/App-Button-Mittelpunkte pro Monitor innerhalb der gesetzten Region (clippedApps=0). Die zusätzliche Gap-Prüfung verhindert, dass eine volle Fallback-Region fälschlich als erfolgreicher Dynamic-Test gilt. Widgets werden bei ShowWidgets=false absichtlich ausgespart und separat von den App-Schaltflächen bewertet.

Noch zu prüfen: letzte Widgets-Korrektur als laufende Anwendung links/zentriert, gezieltes Öffnen/Schließen zusätzlicher Apps, Tray ein/aus und Hover, alle bestehenden Segmentoptionen, reale unterschiedliche DPI (bisher nur synthetisch 100/150/200 Prozent). Die zwei realen Monitore wurden mit gleicher Skalierung getestet.

Windows-Auto-Hide setzt im vorhandenen Code die Region zurück und pausiert Styling. Diese Strategie wurde nicht geändert; die Umschaltung wurde jetzt live getestet, eine ausführliche visuelle Flackerprüfung und FluentTBs eigener AutoHideMode bleiben offen. TranslucentTB-Kombinationstests folgen erst in Phase 3.

Installer, Store-Dateien, Versionsnummern und MSIX-Manifest wurden nicht geändert.

## Nachkorrektur: leerer Icon-Platz und Widgets (12. September, DynamicFix3)

Die vorherige Bewertung von ShowWidgets=false als beabsichtigtes Verhalten entsprach nicht dem Nutzerwunsch. Die vorhandene Nutzerkonfiguration wurde bei gestopptem Prozess gesichert und ShowWidgets aktiviert. Neue Settings verwenden dafür true; explizites false in vorhandenen Dateien bleibt weiterhin lesbar und wirksam. Widget-Regionen werden an den Taskleistenrändern begrenzt.

Die pauschalen 48 Pixel Zusatzabstand waren nach der Umstellung auf echte UIA-Button-Container überflüssig: Diese enthalten bereits den internen Icon-Abstand. Die Region verwendet jetzt die konfigurierten Ränder (hier 3 Pixel). GetWindowRgn zeigt wegen der exklusiven GDI-Grenze rechts tatsächlich 2 zusätzliche Pixel. Die Diagnose erfasst jetzt auch die zusammenhängende App-Region auf deren horizontaler Mittellinie.

DynamicFix3 wurde gebaut (0 Fehler, 0 Warnungen), mit 42 synthetischen Fällen geprüft und gestartet. Windows-Einstellungen wurden über die native Oberfläche bedient; die Taskleisten selbst wurden zusätzlich mit der lesenden Region-Diagnose geprüft:

| Zustand, jeweils beide reale Monitore bei 100 Prozent DPI | Ergebnis |
| --- | --- |
| Links, 8 Start-/App-Buttons | Region relativ 3–365 bei Apps 0–363; 15/11 Button-Mittelpunkte einschließlich Widgets geprüft, keiner abgeschnitten; Zwischenraum ausgespart |
| Zentriert, 8 Start-/App-Buttons | Apps 779–1142, Region 776–1144; 15/11 Mittelpunkte einschließlich links stehender Widgets innerhalb der Region; Zwischenraum ausgespart |
| Zentriert, zusätzlich FluentTB-Einstellungsfenster geöffnet | Windows verschiebt Apps auf 757–1164; Region folgt auf 754–1166; 16/12 Mittelpunkte innerhalb, 9 Start-/App-Buttons je Monitor |
| Windows-Auto-Hide ein und wieder aus | Log bestätigt Pause um 23:25:17 und Wiederaufnahme um 23:25:31; anschließende zentrierte Regionsprüfung erfolgreich |

Messdateien liegen im lokalen Build-Ordner bin/DynamicFix3 als snapshot-left.txt, snapshot-centred.txt und snapshot-centred-extra-window.txt. Diese Stichproben belegen die Geometrie, nicht die vollständige visuelle Flackerfreiheit oder die Klickfunktion jeder Schaltfläche. Phase 1 bleibt für die übrigen Segment-/Hover-Modi, unterschiedliche reale DPI und die vollständige visuelle Abnahme offen. Phase 2 wurde nicht begonnen.
