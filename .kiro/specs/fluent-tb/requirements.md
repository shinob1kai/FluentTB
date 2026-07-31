# Requirements Document

## Introduction

FluentTB ist eine Windows-Desktopanwendung (C#, .NET 6+, WPF), die als Fork von RoundedTB entsteht
und die Taskleisten-Effekte von TranslucentTB nativ integriert – ohne externe C++-DLL, ohne externes
Prozess-Messaging. Die Anwendung läuft als Hintergrundprozess und bietet:

- **Visuelle Effekte** (Clear, Blur, Acrylic, Mica, benutzerdefinierte Farbe) via `SetWindowCompositionAttribute`
- **Layout-Anpassungen** (abgerundete Ecken, Margins, Dynamic Mode, Split Mode) via `SetWindowRgn`
- **Zustandsabhängiges Verhalten** (Desktop, maximiertes Fenster, sichtbares Fenster, Startmenü, Suche, Task View, Energiesparmodus)
- **Multi-Monitor-Unterstützung** für primäre und sekundäre Taskleisten
- **Zielplattform:** Windows 11 (Build ≥ 22000) als Primär, Windows 10 (Build ≥ 17763) als vollständig unterstützte Plattform

Bekannte Probleme von RoundedTB – insbesondere Flackern durch externes TranslucentTB-Messaging,
Dynamic-Mode-Instabilität durch Polling sowie fehlende Anti-Aliasing-Unterstützung – werden durch
architektonische Verbesserungen behoben.

---

## Glossary

- **FluentTB**: Die zu entwickelnde Anwendung (dieser Fork).
- **Taskleiste**: Das Windows-Shell-Fenster der Klasse `Shell_TrayWnd` (primär) bzw. `Shell_SecondaryTrayWnd` (sekundär).
- **Effekt_Engine**: Die Komponente, die `SetWindowCompositionAttribute` mit einer `AccentPolicy` aufruft, um visuelle Effekte auf eine Taskleiste anzuwenden.
- **Layout_Engine**: Die Komponente, die `SetWindowRgn` / `CreateRoundRectRgn` aufruft, um Form und Margins der Taskleiste anzupassen.
- **Zustands_Monitor**: Die Komponente, die den aktuellen Desktop-Zustand (maximiertes Fenster, Startmenü offen usw.) ermittelt und die passende Darstellung auswählt.
- **Konfigurations_Manager**: Die Komponente, die Einstellungen in einer JSON-Datei liest und schreibt.
- **Settings_UI**: Das WPF-Einstellungsfenster, über das Benutzer FluentTB konfigurieren.
- **Tray_Icon**: Das Systemtray-Symbol von FluentTB mit Kontextmenü.
- **UIAutomation_Listener**: Die Komponente, die UIAutomation-Events abonniert, um Icon-Änderungen in der App-Liste zu erkennen (ersetzt Polling).
- **ACCENT_STATE**: Enum-Wert, der den Effekttyp für `SetWindowCompositionAttribute` bestimmt (0–5).
- **AccentPolicy**: Struktur mit `AccentState`, `AccentFlags`, `GradientColor` (RGBA) und `AnimationId`.
- **Dynamic_Mode**: Modus (Windows 11), bei dem die Taskleiste ihre Breite dynamisch an die Anzahl der Icons anpasst.
- **Split_Mode**: Modus (Windows 10), bei dem App-Liste und System-Tray als separate visuelle Segmente dargestellt werden.
- **Segment**: Ein visuell abgetrennter Bereich der Taskleiste (App-Liste, Tray, Widgets).
- **SegmentSettings**: Konfigurationsmodell mit `CornerRadius`, `MarginTop/Bottom/Left/Right` für ein Segment.
- **AppearanceState**: Konfigurationsmodell mit Effekttyp, RGBA-Farbe, Blur-Radius und ShowPeek/ShowLine-Flags für einen Zustand.
- **DWM**: Desktop Window Manager – Windows-Dienst für Fensterkomposition.
- **Mica**: DWM-Effekt unter Windows 11 (Build ≥ 22000) via `ACCENT_ENABLE_HOSTBACKDROP`.
- **Win11**: Windows 11, Build ≥ 22000.
- **Win10**: Windows 10, Build 17763–19045.


---

## Requirements

---

### Requirement 1: Visuelle Effekte via SetWindowCompositionAttribute

**User Story:** Als Benutzer möchte ich die visuelle Darstellung der Taskleiste (transparent, verschwommen,
Acrylic, Mica) einstellen können, damit meine Windows-Oberfläche ein modernes Fluent-Design-Erscheinungsbild erhält.

#### Acceptance Criteria

1. WHEN die Anwendung einen Effekt auf eine Taskleiste anwendet, THE Effekt_Engine SHALL `SetWindowCompositionAttribute` mit einer korrekt befüllten `AccentPolicy`-Struktur aufrufen.
2. WHEN der Benutzer den Effekt **Clear** konfiguriert, THE Effekt_Engine SHALL `ACCENT_ENABLE_TRANSPARENTGRADIENT` (Wert 2) mit einem Alpha-Kanal von 0 in `GradientColor` setzen.
3. WHEN der Benutzer den Effekt **Blur** konfiguriert, THE Effekt_Engine SHALL `ACCENT_ENABLE_BLURBEHIND` (Wert 3) mit dem konfigurierten RGBA-Farbwert in `GradientColor` setzen.
4. WHEN der Benutzer den Effekt **Acrylic** konfiguriert, THE Effekt_Engine SHALL `ACCENT_ENABLE_ACRYLICBLURBEHIND` (Wert 4) mit dem konfigurierten RGBA-Farbwert setzen.
5. WHERE Win11 aktiv ist, WHEN der Benutzer den Effekt **Mica** konfiguriert, THE Effekt_Engine SHALL `ACCENT_ENABLE_HOSTBACKDROP` (Wert 5) setzen.
6. IF Win11 nicht aktiv ist UND der Benutzer den Effekt **Mica** konfiguriert, THEN THE Effekt_Engine SHALL den Effekt automatisch auf **Acrylic** zurückfallen und THE Settings_UI SHALL einen Hinweis anzeigen, dass Mica nur unter Windows 11 (Build ≥ 22000) verfügbar ist.
7. WHEN der Benutzer den Effekt **Benutzerdefinierte Farbe (opak)** konfiguriert, THE Effekt_Engine SHALL `ACCENT_ENABLE_GRADIENT` (Wert 1) mit dem konfigurierten RGBA-Farbwert setzen.
8. WHEN der Benutzer den Effekt **Benutzerdefinierte Farbe (transparent)** konfiguriert, THE Effekt_Engine SHALL `ACCENT_ENABLE_TRANSPARENTGRADIENT` (Wert 2) mit dem konfigurierten RGBA-Farbwert setzen.
9. THE Effekt_Engine SHALL keine externe Prozess-Kommunikation (z. B. Nachrichten an TranslucentTB) verwenden, um visuelle Effekte anzuwenden.
10. WHEN `SetWindowCompositionAttribute` einen Fehlercode ungleich 0 zurückgibt, THE Effekt_Engine SHALL den Fehlercode protokollieren und den vorherigen Effekt beibehalten.

---

### Requirement 2: Zustandsabhängige Darstellung

**User Story:** Als Benutzer möchte ich für jeden Desktop-Zustand (Desktop, maximiertes Fenster,
Startmenü offen usw.) eine eigene Taskleisten-Darstellung festlegen können, damit sich die Taskleiste
kontextsensitiv verhält.

#### Acceptance Criteria

1. THE Zustands_Monitor SHALL die folgenden sieben Zustände erkennen und als exklusiv priorisierte Liste auswerten: `StartMenuOpen`, `SearchOpen`, `TaskViewOpen`, `BatterySaver`, `MaximisedWindow`, `VisibleWindow`, `Desktop`.
2. WHEN der Zustands_Monitor den aktiven Zustand ermittelt hat, THE Zustands_Monitor SHALL die diesem Zustand zugewiesene `AppearanceState`-Konfiguration an die Effekt_Engine und die Layout_Engine übergeben.
3. WHEN das Startmenü geöffnet wird, THE Zustands_Monitor SHALL den Zustand `StartMenuOpen` innerhalb von 150 ms erkennen.
4. WHEN das Suchfeld geöffnet wird, THE Zustands_Monitor SHALL den Zustand `SearchOpen` innerhalb von 150 ms erkennen.
5. WHEN die Task-View geöffnet wird, THE Zustands_Monitor SHALL den Zustand `TaskViewOpen` innerhalb von 150 ms erkennen.
6. WHEN der Energiesparmodus aktiviert oder deaktiviert wird, THE Zustands_Monitor SHALL den Zustand `BatterySaver` innerhalb von 500 ms aktualisieren.
7. WHEN ein Fenster auf demselben Monitor maximiert wird, THE Zustands_Monitor SHALL den Zustand `MaximisedWindow` innerhalb von 150 ms erkennen.
8. WHEN ein nicht-maximiertes Fenster auf demselben Monitor sichtbar ist, THE Zustands_Monitor SHALL den Zustand `VisibleWindow` erkennen, sofern kein höher priorisierter Zustand aktiv ist.
9. WHEN kein anderer Zustand als `Desktop` gilt, THE Zustands_Monitor SHALL den Zustand `Desktop` aktivieren.
10. WHEN ein Zustand für einen bestimmten Monitor deaktiviert ist (optionale Zustände), THE Zustands_Monitor SHALL diesen Zustand überspringen und den nächsten aktiven Zustand in der Prioritätsliste verwenden.
11. THE Zustands_Monitor SHALL für jeden angeschlossenen Monitor unabhängige Zustandsermittlungen durchführen.


---

### Requirement 3: Layout – Abgerundete Ecken und Margins

**User Story:** Als Benutzer möchte ich den Eckradius und die Abstände der Taskleiste vom Bildschirmrand
einstellen können, damit ich ein schwebend wirkendes oder an den Rand gebundenes Layout erzeugen kann.

#### Acceptance Criteria

1. THE Layout_Engine SHALL `SetWindowRgn` mit einer via `CreateRoundRectRgn` erzeugten Region aufrufen, um die Taskleisten-Form anzupassen.
2. THE Layout_Engine SHALL für jedes Segment unabhängige `SegmentSettings` (CornerRadius, MarginTop, MarginBottom, MarginLeft, MarginRight) akzeptieren.
3. WHEN ein Margin-Wert negativ ist, THE Layout_Engine SHALL die entsprechende Kante der Region bis an den Bildschirmrand ausdehnen, sodass keine sichtbare Lücke entsteht.
4. WHEN `CornerRadius` den Wert 0 hat, THE Layout_Engine SHALL eine rechteckige Region ohne Abrundung erzeugen.
5. WHEN `CornerRadius` einen Wert größer als 0 hat, THE Layout_Engine SHALL eine abgerundete Region erzeugen; der Radius wird mit dem DPI-Skalierungsfaktor des Monitors multipliziert, bevor `CreateRoundRectRgn` aufgerufen wird.
6. WHERE Win11 aktiv ist, WHEN `CornerRadius` größer als 0 ist, THE Layout_Engine SHALL zusätzlich `DwmSetWindowAttribute` mit `DWMWA_WINDOW_CORNER_PREFERENCE` = `DWMWCP_ROUND` aufrufen, um systemseitige Anti-Aliasing-Unterstützung zu aktivieren.
7. WHERE Win10 aktiv ist, WHEN `CornerRadius` größer als 0 ist, THE Layout_Engine SHALL `WS_EX_LAYERED` mit Alpha-Blending verwenden, um weiche Ecken anzunähern.
8. WHEN sich die DPI-Skalierung eines Monitors ändert, THE Layout_Engine SHALL die Region neu berechnen und erneut anwenden.
9. WHEN `SetWindowRgn` mit einem Fehler zurückkehrt, THE Layout_Engine SHALL den Fehlercode protokollieren und den vorherigen Zustand beibehalten.

---

### Requirement 4: Dynamic Mode (Windows 11)

**User Story:** Als Windows-11-Benutzer möchte ich, dass die Taskleiste ihre Breite automatisch an die
Anzahl der angehefteten und geöffneten Apps anpasst, damit sie wie ein schwebendes Dock wirkt.

#### Acceptance Criteria

1. WHERE Win11 aktiv ist UND Dynamic Mode aktiviert ist, THE UIAutomation_Listener SHALL UIAutomation-Events (`ElementAdded`, `ElementRemoved`) auf dem App-Listen-Fenster (`MSTaskSwWClass`) abonnieren, anstatt einen Polling-Timer zu verwenden.
2. WHEN ein UIAutomation-Event eintrifft, THE Layout_Engine SHALL die Breite der App-Liste innerhalb von 100 ms neu auslesen und die Region der Taskleiste aktualisieren.
3. WHERE Dynamic Mode aktiv ist, WHEN die Taskleiste zentriert ausgerichtet ist, THE Layout_Engine SHALL den Abstand von rechts (`centredDistanceFromEdge`) symmetrisch auf beide Seiten der Region anwenden.
4. WHERE Dynamic Mode aktiv ist, WHEN die Taskleiste links ausgerichtet ist, THE Layout_Engine SHALL die Region ausgehend vom linken Rand bis zur berechneten rechten Grenze erzeugen.
5. THE Layout_Engine SHALL beim Start von FluentTB die Taskleisten-Ausrichtung explizit aus der Registry (`HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarAl`) lesen, bevor die erste Region gesetzt wird.
6. WHEN Dynamic Mode aktiv ist UND der System-Tray angezeigt wird (`ShowTray = true`), THE Layout_Engine SHALL eine kombinierte Region aus App-Liste und Tray via `CombineRgn` erzeugen.
7. WHEN Dynamic Mode aktiv ist UND das Widgets-Panel angezeigt wird (`ShowWidgets = true`), THE Layout_Engine SHALL eine kombinierte Region aus App-Liste, Tray und Widgets via `CombineRgn` erzeugen.
8. WHEN Dynamic Mode aktiv ist UND die berechnete App-Listen-Breite weniger als 20 logische Pixel beträgt, THE Layout_Engine SHALL die Region nicht aktualisieren und stattdessen die vorherige Region beibehalten.
9. WHEN Dynamic Mode aktiv ist UND die berechnete App-Listen-Breite die gesamte Taskleistenbreite überschreitet, THE Layout_Engine SHALL die Region nicht aktualisieren.
10. WHERE Win10 aktiv ist, THE Anwendung SHALL Dynamic Mode nicht aktivieren und THE Settings_UI SHALL die Option als nicht verfügbar anzeigen.
11. WHEN Dynamic Mode auf einer sekundären Taskleiste (`Shell_SecondaryTrayWnd`) aktiv ist, THE Layout_Engine SHALL dieselbe Logik wie für die primäre Taskleiste anwenden.


---

### Requirement 5: Split Mode (Windows 10)

**User Story:** Als Windows-10-Benutzer möchte ich App-Liste und System-Tray als separate, abgerundete
Segmente darstellen können, damit auch unter Windows 10 ein modernes geteiltes Taskleisten-Layout möglich ist.

#### Acceptance Criteria

1. WHERE Win10 aktiv ist UND Split Mode aktiviert ist, THE Layout_Engine SHALL zwei unabhängige Regionen erzeugen: eine für die App-Liste und eine für den System-Tray.
2. THE Layout_Engine SHALL Split Mode sowohl auf der primären Taskleiste (`Shell_TrayWnd`) als auch auf allen sekundären Taskleisten (`Shell_SecondaryTrayWnd`) anwenden.
3. WHEN Split Mode aktiv ist UND `ShowTray = false`, THE Layout_Engine SHALL nur die Region für die App-Liste setzen und den System-Tray ausblenden.
4. WHEN Split Mode aktiv ist UND `ShowTray = true`, THE Layout_Engine SHALL beide Regionen (App-Liste und Tray) kombiniert als eine via `CombineRgn` erzeugte Region setzen.
5. WHERE Win11 aktiv ist, THE Anwendung SHALL Split Mode nicht aktivieren und THE Settings_UI SHALL die Option als nicht verfügbar anzeigen.
6. WHEN Split Mode aktiv ist UND die Taskleiste vertikal positioniert ist (links oder rechts), THE Layout_Engine SHALL Split Mode für diese Taskleiste deaktivieren und THE Settings_UI SHALL eine Warnung anzeigen.

---

### Requirement 6: Fill on Maximise

**User Story:** Als Benutzer möchte ich, dass die Taskleiste bei einem maximierten Fenster automatisch auf
die volle Bildschirmbreite ausgedehnt wird, damit wichtige Tray-Informationen immer sichtbar sind.

#### Acceptance Criteria

1. WHEN auf dem Monitor, dem die Taskleiste zugeordnet ist, ein Fenster maximiert wird, THE Layout_Engine SHALL `SetWindowRgn` mit `IntPtr.Zero` aufrufen, um die angepasste Region zu entfernen und die Taskleiste auf volle Breite zurückzusetzen.
2. WHEN das maximierte Fenster geschlossen oder wiederhergestellt wird, THE Layout_Engine SHALL die konfigurierte Region erneut anwenden.
3. WHERE Win11 aktiv ist UND `FillOnTaskSwitch = true`, WHEN der Alt+Tab-Umschalter (`XamlExplorerHostIslandWindow`) sichtbar ist, THE Layout_Engine SHALL die Region ebenfalls entfernen.
4. WHEN `FillOnMaximise = false` konfiguriert ist, THE Layout_Engine SHALL die Region unabhängig vom Fensterzustand beibehalten.
5. THE Layout_Engine SHALL die Fill-on-Maximise-Prüfung für jeden Monitor unabhängig durchführen.

---

### Requirement 7: Multi-Monitor-Unterstützung

**User Story:** Als Benutzer mit mehreren Monitoren möchte ich jeden Monitor-Taskleisten unabhängig
konfigurieren können, damit jeder Bildschirm sein eigenes Layout und seinen eigenen Effekt erhält.

#### Acceptance Criteria

1. THE Anwendung SHALL beim Start alle vorhandenen primären (`Shell_TrayWnd`) und sekundären (`Shell_SecondaryTrayWnd`) Taskleisten-Handles ermitteln und in einer internen Liste verwalten.
2. WHEN ein Monitor angeschlossen oder getrennt wird, THE Anwendung SHALL die Taskleisten-Liste innerhalb von 1000 ms aktualisieren.
3. WHEN sich die Anzahl der Taskleisten oder das Handle der primären Taskleiste ändert, THE Anwendung SHALL alle Taskleisten-Handles neu einlesen und die Effekte neu anwenden.
4. THE Layout_Engine SHALL den DPI-Skalierungsfaktor jedes Monitors (`GetDpiForWindow`) individuell berücksichtigen und alle Pixel-Berechnungen mit dem jeweiligen Skalierungsfaktor multiplizieren.
5. THE Zustands_Monitor SHALL für jeden Monitor unabhängig prüfen, ob sich ein maximiertes oder sichtbares Fenster auf demselben Monitor wie die jeweilige Taskleiste befindet (`MonitorFromWindow`).


---

### Requirement 8: Auto-Hide der Taskleiste

**User Story:** Als Benutzer möchte ich die Taskleiste automatisch ausblenden lassen, wenn der Mauszeiger
nicht über ihr ist, damit mehr Bildschirmfläche zur Verfügung steht.

#### Acceptance Criteria

1. WHERE Auto-Hide aktiviert ist, WHEN der Mauszeiger die Taskleisten-Region verlässt, THE Layout_Engine SHALL `SetLayeredWindowAttributes` mit schrittweise abnehmenden Alpha-Werten (255 → 191 → 127 → 63 → 1) aufrufen, wobei zwischen jedem Schritt 15 ms gewartet werden.
2. WHERE Auto-Hide aktiviert ist, WHEN der Mauszeiger in einen 2-Pixel-Streifen am unteren (oder jeweiligen) Bildschirmrand eintritt, THE Layout_Engine SHALL `SetLayeredWindowAttributes` mit schrittweise zunehmenden Alpha-Werten (1 → 63 → 127 → 191 → 255) aufrufen.
3. WHERE Auto-Hide aktiv ist UND die Taskleiste ausgeblendet ist, THE Layout_Engine SHALL `WS_EX_TRANSPARENT` zum erweiterten Fensterstil hinzufügen, damit Mausklicks die Taskleiste durchdringen.
4. WHERE Auto-Hide aktiv ist UND die Taskleiste eingeblendet wird, THE Layout_Engine SHALL `WS_EX_TRANSPARENT` aus dem erweiterten Fensterstil entfernen.
5. WHERE Auto-Hide aktiv ist, WHILE die Effekt_Engine einen Effekt anwendet, THE Effekt_Engine SHALL den Effekt ohne visuelles Flackern anwenden, indem kein externes Prozess-Messaging verwendet wird.

---

### Requirement 9: Konfiguration und Persistenz

**User Story:** Als Benutzer möchte ich meine Einstellungen dauerhaft speichern und zwischen Sitzungen
erhalten, damit ich FluentTB nicht nach jedem Neustart neu konfigurieren muss.

#### Acceptance Criteria

1. THE Konfigurations_Manager SHALL Einstellungen in einer JSON-Datei im Benutzer-Profil-Verzeichnis (`%APPDATA%\FluentTB\config.json`) speichern.
2. WHEN FluentTB gestartet wird UND die Konfigurationsdatei existiert, THE Konfigurations_Manager SHALL die Datei einlesen und das interne Einstellungsmodell befüllen.
3. WHEN FluentTB gestartet wird UND keine Konfigurationsdatei existiert, THE Konfigurations_Manager SHALL eine Standardkonfiguration erzeugen, in die Datei schreiben und die Anwendung mit den Standardwerten initialisieren.
4. WHEN die Konfigurationsdatei existiert UND leer oder syntaktisch ungültig ist, THE Konfigurations_Manager SHALL die Standardkonfiguration laden und eine Warnung ins Log schreiben.
5. WHEN Einstellungen vom Benutzer gespeichert werden, THE Konfigurations_Manager SHALL die Konfigurationsdatei atomar (Schreiben in Temporärdatei, dann Umbenennen) aktualisieren, sodass bei einem Absturz kein korrupter Zustand entsteht.
6. THE Konfigurations_Manager SHALL ein Rund-Trip-Schema für die JSON-Serialisierung unterstützen: FOR ALL gültige Einstellungsobjekte gilt, dass Serialisieren und anschließendes Deserialisieren ein semantisch äquivalentes Objekt ergibt.
7. WHEN Einstellungen gespeichert werden UND `DisableSaving = true` konfiguriert ist, THE Konfigurations_Manager SHALL keinen Schreibvorgang ausführen.
8. THE Konfigurations_Manager SHALL unbekannte JSON-Schlüssel beim Lesen ignorieren, anstatt einen Fehler zu werfen, um die Abwärtskompatibilität bei Konfigurationsänderungen zu gewährleisten.

---

### Requirement 10: Einstellungs-Oberfläche (Settings UI)

**User Story:** Als Benutzer möchte ich eine übersichtliche grafische Oberfläche haben, über die ich alle
FluentTB-Optionen konfigurieren kann, ohne Konfigurationsdateien manuell bearbeiten zu müssen.

#### Acceptance Criteria

1. THE Settings_UI SHALL einen Bereich **„Ecken & Layout"** mit Steuerelementen für `CornerRadius` (0–50 Pixel), unabhängige Margins (−20 bis +50 Pixel pro Seite) sowie Checkboxen für Dynamic Mode und Split Mode bereitstellen.
2. THE Settings_UI SHALL einen Bereich **„Effekte & Stile"** mit einem Dropdown-Menü für jeden der sieben Zustände bereitstellen; das Dropdown SHALL die Optionen `Normal`, `Clear`, `Blur`, `Acrylic`, `Mica`, `Opake Farbe`, `Transparente Farbe` anzeigen.
3. WHEN im Bereich „Effekte & Stile" ein Farbeffekt ausgewählt wird, THE Settings_UI SHALL einen RGBA-Farbwähler einblenden, der die Farbwahl inklusive Alpha-Kanal ermöglicht.
4. WHEN Mica im Effekt-Dropdown ausgewählt wird UND Win10 aktiv ist, THE Settings_UI SHALL die Option grau hinterlegen und einen Tooltip mit dem Hinweis „Mica erfordert Windows 11 (Build ≥ 22000)" anzeigen.
5. THE Settings_UI SHALL eine Checkbox **„Mit Windows starten"** bereitstellen; WHEN diese aktiviert wird, THE Anwendung SHALL einen Autostart-Eintrag in `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` anlegen.
6. WHEN diese Checkbox deaktiviert wird, THE Anwendung SHALL den Autostart-Eintrag aus der Registry entfernen.
7. THE Settings_UI SHALL einen **„Übernehmen"**-Button bereitstellen; WHEN dieser geklickt wird, THE Anwendung SHALL die Einstellungen sofort auf alle Taskleisten anwenden und in der Konfigurationsdatei speichern.
8. THE Settings_UI SHALL beim Schließen des Fensters nicht beendet werden; stattdessen SHALL das Fenster in den Hintergrund minimiert werden, während FluentTB weiterläuft.


---

### Requirement 11: System-Tray-Icon und Schnellzugriff

**User Story:** Als Benutzer möchte ich FluentTB über ein Tray-Icon steuern können, damit ich ohne
das Einstellungsfenster zu öffnen schnell auf häufig genutzte Aktionen zugreifen kann.

#### Acceptance Criteria

1. THE Anwendung SHALL beim Start ein Icon im Windows-Systemtray registrieren.
2. WHEN das Tray-Icon mit der rechten Maustaste angeklickt wird, THE Tray_Icon SHALL ein Kontextmenü mit den Einträgen **„Einstellungen öffnen"**, **„Effekte deaktivieren"** und **„Beenden"** anzeigen.
3. WHEN der Eintrag **„Einstellungen öffnen"** ausgewählt wird, THE Anwendung SHALL das Einstellungsfenster in den Vordergrund bringen.
4. WHEN der Eintrag **„Effekte deaktivieren"** ausgewählt wird, THE Effekt_Engine SHALL alle Effekte entfernen und THE Layout_Engine SHALL alle Regionen auf `IntPtr.Zero` zurücksetzen, bis der Eintrag erneut ausgewählt wird.
5. WHEN der Eintrag **„Beenden"** ausgewählt wird, THE Anwendung SHALL alle angewendeten Regionen und Effekte rückgängig machen und danach den Prozess beenden.
6. WHEN FluentTB beendet wird, THE Anwendung SHALL für jede verwaltete Taskleiste `SetWindowRgn(hwnd, IntPtr.Zero, true)` aufrufen, um den ursprünglichen Zustand wiederherzustellen.

---

### Requirement 12: Ereignisgesteuerte Aktualisierung (UIAutomation statt Polling)

**User Story:** Als Benutzer möchte ich, dass FluentTB die CPU-Last minimiert und Zustandsänderungen
ohne Flackern erkennt, damit die Anwendung als schlanker Hintergrundprozess läuft.

#### Acceptance Criteria

1. THE UIAutomation_Listener SHALL UIAutomation-Events (`AutomationEventHandler` für `AutomationElement.StructureChangedEvent`) auf der App-Liste der Taskleiste abonnieren.
2. WHEN kein UIAutomation-Event vorliegt und keine Fenster-Zustandsänderung stattfindet, THE Anwendung SHALL keinen aktiven Poll-Loop betreiben; stattdessen SHALL die CPU-Nutzung unter 0,5 % auf einem modernen System liegen.
3. WHEN der UIAutomation_Listener ein Event empfängt, THE UIAutomation_Listener SHALL das Event auf dem UI-Thread via `Dispatcher.InvokeAsync` verarbeiten, um Thread-Safety sicherzustellen.
4. WHEN der UIAutomation_Listener nicht initialisiert werden kann (z. B. Accessibility-Dienst nicht verfügbar), THE Anwendung SHALL auf einen Polling-Intervall von 200 ms zurückfallen und eine Warnung ins Log schreiben.
5. THE Anwendung SHALL einen dedizierten infrequenten Timer (Intervall: 1000 ms) für Aufgaben niedrigerer Priorität verwenden (z. B. Tray-Icon-Aktualisierung, Monitor-Änderungsprüfung).

---

### Requirement 13: Plattformkompatibilität

**User Story:** Als Benutzer möchte ich FluentTB sowohl unter Windows 11 als auch unter Windows 10
nutzen können, damit ich nicht gezwungen bin, das Betriebssystem zu wechseln.

#### Acceptance Criteria

1. THE Anwendung SHALL beim Start die Windows-Build-Nummer via `Environment.OSVersion.Version.Build` ermitteln und intern als `IsWindows11` (Build ≥ 22000) und `IsWindows10` (Build 17763–21999) kennzeichnen.
2. WHERE Win11 aktiv ist, THE Anwendung SHALL Dynamic Mode, Mica-Effekt und `DwmSetWindowAttribute`-basiertes Anti-Aliasing aktivieren.
3. WHERE Win10 aktiv ist, THE Anwendung SHALL Dynamic Mode und Mica deaktivieren; Split Mode und WS_EX_LAYERED-basiertes Alpha-Blending SHALL aktiviert sein.
4. IF Build < 17763, THEN THE Anwendung SHALL eine Fehlermeldung anzeigen und den Start verweigern.
5. THE Anwendung SHALL ausschließlich via P/Invoke (keine externe C++-DLL) auf nicht-dokumentierte Windows-APIs (`SetWindowCompositionAttribute`, `DwmSetWindowAttribute`) zugreifen.
6. THE Anwendung SHALL als 64-Bit-Prozess kompiliert und ausgeliefert werden.
7. THE Anwendung SHALL keine dauerhaften Systemänderungen vornehmen; WHEN FluentTB beendet wird, SHALL der vorherige Taskleisten-Zustand vollständig wiederhergestellt sein.

---

### Requirement 14: Fehlerbehandlung und Protokollierung

**User Story:** Als Entwickler und erfahrener Benutzer möchte ich, dass FluentTB Fehler robust behandelt
und aussagekräftige Log-Einträge erzeugt, damit Probleme diagnostiziert werden können.

#### Acceptance Criteria

1. THE Anwendung SHALL eine Protokolldatei unter `%APPDATA%\FluentTB\fluent-tb.log` führen.
2. WHEN eine P/Invoke-Funktion einen Fehler zurückgibt, THE Anwendung SHALL den Windows-Fehlercode (`Marshal.GetLastWin32Error()`), die aufrufende Methode und den Zeitstempel in die Protokolldatei schreiben.
3. WHEN eine unbehandelte Ausnahme in einem Hintergrundthread auftritt, THE Anwendung SHALL die Ausnahme protokollieren, alle Taskleisten in den ursprünglichen Zustand zurückversetzen und den Benutzer via Tray-Benachrichtigung informieren.
4. WHEN eine `TypeInitializationException` im Hintergrundthread auftritt, THE Anwendung SHALL die Ausnahme inklusive `InnerException` protokollieren und die Anwendung geordnet beenden.
5. THE Anwendung SHALL den Verbosity-Level der Protokollierung konfigurierbar halten (`Trace`, `Debug`, `Info`, `Warn`, `Error`); der Standardwert SHALL `Warn` sein.
6. WHEN die Protokolldatei größer als 10 MB wird, THE Anwendung SHALL die Datei rotieren, indem die aktuelle Datei umbenannt (`fluent-tb.log.1`) und eine neue Datei begonnen wird.


---

### Requirement 15: JSON-Konfigurationsformat (Parser/Serialisierer)

**User Story:** Als Benutzer und Entwickler möchte ich, dass das Konfigurationsformat stabil, lesbar und
erweiterbar ist, damit Einstellungen manuell bearbeitet oder per Skript verwaltet werden können.

#### Acceptance Criteria

1. WHEN eine gültige JSON-Konfigurationsdatei eingelesen wird, THE Konfigurations_Manager SHALL ein vollständiges `FluentTBSettings`-Objekt erzeugen, das alle Felder korrekt befüllt.
2. IF eine JSON-Datei syntaktisch ungültig ist, THEN THE Konfigurations_Manager SHALL eine `ConfigurationParseException` mit einer beschreibenden Fehlermeldung werfen, die Zeilennummer und Fehlerursache enthält.
3. THE Konfigurations_Manager SHALL RGBA-Farbwerte im Hexadezimalformat `#RRGGBBAA` (8-stellig) serialisieren und deserialisieren.
4. FOR ALL gültigen `FluentTBSettings`-Objekte gilt: Serialisieren (`Serialize`) und anschließendes Deserialisieren (`Deserialize`) SHALL ein semantisch äquivalentes Objekt zurückliefern (Rund-Trip-Eigenschaft).
5. THE Konfigurations_Manager SHALL beim Deserialisieren unbekannte JSON-Schlüssel ignorieren (Vorwärts-Kompatibilität).
6. WHEN ein Pflichtfeld in der JSON-Datei fehlt, THE Konfigurations_Manager SHALL den konfigurierten Standardwert für dieses Feld verwenden.

---

### Requirement 16: Startverhalten und Einzelinstanz-Sicherstellung

**User Story:** Als Benutzer möchte ich, dass FluentTB immer nur einmal läuft und beim Systemstart
automatisch gestartet werden kann, damit keine Konflikte zwischen mehreren Instanzen entstehen.

#### Acceptance Criteria

1. WHEN FluentTB gestartet wird UND bereits eine Instanz läuft, THE Anwendung SHALL die neue Instanz sofort beenden und stattdessen das Einstellungsfenster der laufenden Instanz in den Vordergrund bringen.
2. THE Anwendung SHALL zur Einzelinstanz-Erkennung einen benannten Mutex verwenden.
3. WHEN Autostart aktiviert ist, THE Anwendung SHALL den vollständigen Pfad der ausführbaren Datei als Wert in `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` unter dem Schlüsselnamen `FluentTB` speichern.
4. WHEN Autostart deaktiviert ist, THE Anwendung SHALL den Registry-Eintrag `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\FluentTB` entfernen, sofern er existiert.
5. WHEN FluentTB beim Autostart ohne sichtbares Einstellungsfenster startet (`/silent`-Flag), THE Anwendung SHALL direkt mit aktivierten Effekten im Hintergrund starten, ohne das Einstellungsfenster anzuzeigen.

