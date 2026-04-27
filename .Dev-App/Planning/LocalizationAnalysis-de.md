# Analyse des Lokalisierungssystems — Unity GUI Toolkit

## Architekturübersicht

Das Lokalisierungssystem ist eine produktionsreife Implementierung auf Basis des **Gettext-Standards** mit **PO-Dateien (Portable Object)** sowie zusätzlicher Unterstützung für Excel- und Google-Sheets-Quellen.

### Kernkomponenten

| Komponente | Aufgabe |
|---|---|
| `LocaManager` (abstrakter Singleton) | Zentrale Übersetzungs-API; Singular/Plural; Fallback-Kette |
| `LocaManagerDefaultImpl` | Standardimplementierung; verwaltet Übersetzungs-Dictionaries im Speicher |
| `LocaExcelBridge` | ScriptableObject, das eine XLSX-Datei oder ein Google Sheet einbindet |
| `LocaProviderList` | Registry aller `LocaExcelBridge`-Instanzen (aus Resources geladen) |
| `AdditionalLocaKeys` | Editor-only ScriptableObject für Massen-Schlüsseldefinitionen |
| `UiAutoLocalize` | Komponente, die einen `TMP_Text` anhand eines Lokalisierungsschlüssels automatisch übersetzt |
| `UiLanguageToggle` | UI-Element zum Umschalten der Sprache zur Laufzeit |
| `LocaPlurals` (generierter partieller Code) | Automatisch generierte Pluralregeln pro Sprache (CLDR-Standard) |
| `UiLocalizedTextMeshProUGUI` | TMP-Unterklasse mit integrierter Lokalisierung; löst `UiAutoLocalize` ab |
| `UiRefreshOnLocaChange` | Leichtgewichtige Komponente, die `OnLanguageChanged` ohne `UiThing`-Vererbung auslöst |
| `UiAbstractLocalizedTextByPlayerLevel` | Abstrakte Basisklasse; zeigt je nach Spieler-Level unterschiedliche lokalisierte Texte |
| `LocaJsonKeyProvider` | Editor-only ScriptableObject, das Übersetzungsschlüssel nach Feldname aus JSON-Datendateien extrahiert |
| `UiForceUnlocalizedText` / `UiForceLegacyText` | Migrations-Hilfskomponenten für bewusst unlokalisierte oder veraltete Texte |
| `LocaGettextSheetsSyncer` | Editor-Werkzeug für den Gettext-Push/Pull-Workflow mit Google Sheets |

### Datenfluss

```
Editor: Menü „Process Loca"
  → LocaProcessor durchsucht Szenen / Prefabs / ScriptableObjects / .cs-Dateien
  → Extrahiert Schlüssel: _("key"), _n("s","p",n), __("key"), gettext(), ngettext()
  → Schreibt .pot-Dateien

Übersetzer befüllt .po-Dateien / Excel-Tabelle

Laufzeit:
  LocaManager.ChangeLanguage(id)
  → Lädt .po-Dateien + LocaExcelBridge-Daten für die gewählte Sprache
  → Commit: setzt Language, aktualisiert CultureInfo, speichert PlayerPrefs,
             feuert EvLanguageChanged
  → Alle UiThing-Unterklassen mit NeedsLanguageChangeCallback=true
    rufen OnLanguageChanged() auf
```

---

## Stärken

### S1 — Branchenüblicher Gettext-Workflow
- Vollständige Unterstützung für `_()`, `_n()`, `__()`, `gettext()`, `ngettext()`
- PO/POT-Dateien werden von allen gängigen Übersetzungsplattformen verstanden (Crowdin, Weblate, Lokalise)
- Sprachspezifische Pluralregeln werden automatisch aus den PO-Datei-Headern generiert (CLDR)

### S2 — Zwei Datenquellen, eine API
- PO-Dateien (primär) und Excel/Google Sheets koexistieren über `ILocaProvider`
- Nicht-technische Übersetzer können vertraute Tabellenkalkulationswerkzeuge nutzen
- Google-Sheets-Integration ermöglicht kollaborative Cloud-Übersetzung

### S3 — Automatische Schlüsselextraktion
- Regex-basierter Code-Scan extrahiert Gettext-Muster aus `.cs`-Dateien
- Das Interface `ILocaKeyProvider` erkennt Schlüssel in Komponenten zur Editierzeit automatisch
- Duplikat- und Konflikt-Erkennung mit Warnungen

### S4 — Performance
- O(1)-Dictionary-Lookups pro Übersetzung
- Pluralregeln sind switch-basiert (keine Listeniterationen)
- Lazy Loading von PO-Dateien; kein Overhead pro Frame nach der Initialisierung

### S5 — Erweiterbare Architektur
- Singleton-Setter ermöglicht Dependency-Injection (nützlich für Tests)
- `ILocaProvider` erlaubt eigene zusätzliche Quellen
- Gruppen schaffen logische Übersetzungs-Namespaces

### S6 — Defensive Fallback-Kette
```
Angeforderte Sprache → „dev"-Sprache → Schlüssel selbst (konfigurierbar)
```
- Zweiphasiger Sprachwechsel verhindert Teilzustände: zuerst versuchen, nur bei Erfolg committen
- `CultureInfo` wird synchron gehalten (Datums-/Zahlenformatierung)

### S7 — Editor-Werkzeuge
- Menügesteuerter Schlüssel-Extrakt, POT-Generierung, Pluralregel-Setup
- `LocaExcelBridgeEditor` zur Tabellenkonfiguration
- `SetAllUiLocaGroups` für Massen-Gruppenzuweisung
- Fortschrittsbalken für lange Operationen; vollständige Undo-Unterstützung

### S8 — Gettext-Sheets-Sync-Workflow
- `LocaGettextSheetsSyncer` bietet einen dedizierten Push/Pull-Workflow: neue Schlüssel in ein Google Sheet pushen, Übersetzer-Korrekturen zurück in PO-Dateien pullen
- Push ist additiv: Es werden nur Zeilen angehängt, die noch nicht im Sheet vorhanden sind; bestehende Übersetzerarbeit wird beim Push nie überschrieben
- Pull überschreibt lokale PO-Einträge grundsätzlich, damit Übersetzer-Korrekturen Vorrang haben; verwendet `UNFORMATTED_VALUE`, um Formatierungs-Artefakte von Google Sheets zu vermeiden
- Vor jedem Pull werden zeitgestempelte Backups der lokalen PO-Dateien angelegt
- Das Flag `AutoSyncAfterMerge` in `UiToolkitConfiguration` aktiviert die automatische Synchronisation nach einem Merge-Vorgang

### S9 — Detaillierte POT-Quellenreferenzen
- `LocaProcessor` erzeugt nun vollständige GameObject-Pfade innerhalb von Prefabs und Szenen (z. B. `Canvas/Header/TitleLabel`) als `#:`-Quellenreferenzen
- Für C#-Skripte werden die ersten 30 Zeichen der Quellzeile (ohne führende Leerzeichen) als Kontext aufgenommen
- Übersetzer und Prüfer können den genauen Verwendungsort jedes Schlüssels direkt aus der POT-Datei ableiten

### S10 — JSON-Schlüsselextraktion
- `LocaJsonKeyProvider` ist ein Editor-only ScriptableObject, das Übersetzungsschlüssel anhand konfigurierbarer Feldnamen aus JSON-Datendateien extrahiert
- Ermöglicht datengetriebenem Content (Objektnamen, Beschreibungen, Quest-Texte) die Teilnahme am Standard-POT-Extraktions-Workflow ohne manuelle Schlüssellisten

### S11 — Level-abhängige lokalisierte Texte
- `UiAbstractLocalizedTextByPlayerLevel` stellt eine abstrakte Basisklasse für UI-Komponenten bereit, die je nach aktuellem Spieler-Level unterschiedliche lokalisierte Strings anzeigen
- Implementiert `ILocaKeyProvider`, sodass alle Level-Varianten-Schlüssel automatisch in die POT-Extraktion einbezogen werden
- Konkrete Implementierungen koppeln sich an das Level-Wechsel-Event des Spiels (z. B. `EventsManager.UpdateLevel`)

---

## Schwächen

### W1 — Keine Thread-Sicherheit ⚠️
`m_translationDict` wird ohne Locks zugegriffen. Falls eine Hintergrund-Coroutine während `ChangeLanguage()` `Translate()` aufruft, ist eine Race-Condition möglich. Die `Thread.CurrentThread`-Kulturaktualisierungen betreffen zudem sämtlichen Code auf diesem Thread global.

### W2 — Keine kontextbewussten Übersetzungen ✅ BEHOBEN

`msgctxt`-Parsing ist nun vollständig in `LocaManagerDefaultImpl` implementiert. Kontextbewusste Lookups verwenden die GNU-Gettext-Konvention `"kontext\u0004schlüssel"` (`ComposeContextKey()`); eine `LocaManager.Translate(key, context, group)`-Überladung steht zur Verfügung. Siehe MI2.

### W3 — Stille Fallbacks bei fehlenden Schlüsseln in der Produktion
Der `DebugLoca`-Modus muss manuell aktiviert werden. Fehlende Übersetzungen geben stillschweigend den Schlüsselnamen zurück. Es gibt keine eingebaute Sammlung oder Auswertung nicht übersetzter Strings, sodass Vollständigkeit auf Anhieb nicht messbar ist.

### W4 — Inkonsistente Sprachkennungen ✅
Sprach-IDs sind bei Lookups case-sensitiv, werden aber zur Laufzeit auf Kleinbuchstaben normiert. Eine Nichtübereinstimmung zwischen Excel-Spalten-Headern und PO-Dateinamen (z. B. `en-US` vs. `en_us`) führt zu stillem Ladefehler.
**Behoben**: `LocaManager.NormalizeLanguageId()` konvertiert jede ID in die kanonische BCP-47-Form mit Kleinbuchstaben und Bindestrichen (`zh-tw`, `pt-br`) und gibt eine Warnung aus, wenn die Eingabe normalisiert werden musste – Abweichungen sind sofort in der Konsole sichtbar, ohne das Spiel zu unterbrechen.

### W5 — Keine automatische Systemspracherkennung
Beim ersten Start fällt das System immer auf `"dev"` zurück. Die OS-Sprache (`CultureInfo.CurrentCulture`) wird nie berücksichtigt; jedes integrierende Projekt muss eigene Erststart-Logik implementieren.

### W6 — Kein Gruppen-Fallback
Wird ein Schlüssel in der angeforderten Gruppe nicht gefunden, fällt das System **nicht** automatisch auf die Standardgruppe zurück. Anwendungen müssen Schlüssel entweder duplizieren oder selbst einen Fallback implementieren.

### W7 — Synchroner Sprachwechsel
Alle PO-Dateien einer Sprache werden synchron in `ChangeLanguageImpl()` geladen. Bei großen Katalogen führt dies zu einem spürbaren Frame-Drop beim Sprachwechsel.

### W8 — Format-String-Platzhalter werden ignoriert
Das System verfolgt `{0}`, `{1}`-Platzhalter in Schlüsseln nicht. Übersetzer sehen nicht, wofür die Platzhalter stehen; vertauschte Platzhalter verursachen stille Laufzeitfehler.

### W9 — ~~TextAsset-Speicher wird nicht freigegeben~~ ✅ KEIN PROBLEM
`TryLoadPoText()` lädt die PO-Datei als `TextAsset` nur, um den `.text`-String in ein lokales `string[]` einzulesen. Die `TextAsset`-Referenz ist eine lokale Variable und wird nicht als Member-Feld gespeichert; der Garbage Collector kann sie unmittelbar nach dem Laden einsammeln. Es gibt keinen Speicheraufbau über Sprachwechsel hinweg.

### W10 — Starre Excel-Spaltenkonfiguration
Pluralform-Spalten müssen manuell bezeichnet werden. Es gibt keine automatische Erkennung von Sprachspalten anhand von Header-Namen und keine CSV/TSV-Unterstützung.

### W11 — AssetReadyGate als Single Point of Failure
Feuert `AssetReadyGate` nicht, wird `LocaManager` nie initialisiert. Öffentliche `Translate()`-Aufrufe prüfen nicht, ob `Language` bereits gesetzt ist, und geben still den Schlüssel zurück.

### W12 — Fragile Schlüsselextraktions-Regex
Die Extraktions-Regex unterstützt keine Escape-Zeichen in Anführungszeichen, mehrzeilige Strings oder String-Konkatenation. Komplexe Muster werden still übergangen.

---

## Verbesserungsvorschläge

### Priorität: Hoch

| ID | Verbesserung | Aufwand | Risiko |
|---|---|---|---|
| H1 | **Thread-Sicherheit** — `lock(m_lockObject)` in `Translate()` und `ChangeLanguageImpl()` | Gering | Gering |
| H2 | **Sprachkennungen normalisieren** — zentrale `NormalizeLanguageId()`-Methode, kanonische Form: Kleinbuchstaben + Bindestriche (BCP 47), warnt bei Abweichung ✅ | Gering | Gering |
| H3 | **Fehlende-Schlüssel-Reporting** — fehlende Schlüssel in einem `HashSet` sammeln; Editor-Dump-Methode anbieten | Mittel | Gering |
| H4 | **Gruppen-Fallback-Kette** — bei nicht gefundenem Schlüssel in der Gruppe automatisch auf Standardgruppe zurückfallen | Gering | Gering |
| H5 | **Asynchrones Laden** — `Task.Run()`-Wrapper für `ChangeLanguageImpl()` zur Vermeidung von Frame-Drops | Mittel | Mittel |

### Priorität: Mittel

| ID | Verbesserung | Aufwand | Risiko |
|---|---|---|---|
| M1 | **Systemspracherkennung** beim Erststart (`CultureInfo.CurrentCulture` abfragen) | Gering | Mittel |
| M2 | **Format-String-Platzhalter verfolgen** — `{N}` in POT-Kommentare schreiben; Laufzeitvalidierung | Mittel | Gering |
| M3 | **Excel-Spaltenerkennung** — Sprachkodes automatisch aus Header-Namen ableiten | Mittel | Gering |
| M4 | **Pluralform-Validierung** — Warnung, wenn eine Sprache weniger Formen liefert als erwartet | Gering | Gering |
| M5 | **`EvLanguageChanging`-Event** — UI vor dem Sprachwechsel informieren (Ladeanzeigen) | Gering | Gering |
| M6 | ~~**Sprach-Cache-Eviction** — `TextAsset`s der vorherigen Sprache entladen~~ ✅ Nicht nötig — `TextAsset` wird nicht gehalten | — | — |
| M7 | **Verbesserte Extraktions-Regex** — Escape-Zeichen, Trailing-Comma, `__()`-Lookbehind korrekt behandeln | Mittel | Mittel |

### Priorität: Niedrig

| ID | Verbesserung | Aufwand | Risiko |
|---|---|---|---|
| L1 | **Übersetzungsabdeckungs-Dashboard** (Editor-Fenster) — Schlüssel pro Sprache, Vollständigkeit in % | Mittel | Gering |
| L2 | **CSV/TSV-Import** | Mittel | Gering |
| L3 | **`LocaExcelBridge`-Metadaten** — Übersetzer, letztes Update, Fertigstellungsgrad | Gering | Gering |
| L4 | **POT-Datei-Hash-Caching** — Neu-Extraktion überspringen, wenn Quelldateien unverändert | Gering | Gering |
| L5 | **Konfigurierbarer Debug-Log-Pfad** (statt hartkodiertem `C:\temp`) | Gering | Gering |

---

## Kritische Beobachtung

`AssetReadyGate` ist der einzige Ausfallpunkt der gesamten Initialisierungskette.  
**Empfohlene Absicherung** in `Translate()`:

```csharp
if (Language == null)
{
    UiLog.LogError("Lokalisierung nicht initialisiert – AssetReadyGate hat möglicherweise nicht gefeuert.");
    return _key;
}
```

---

## Fehlende Implementierungen

Diese Funktionen sind entweder architektonisch vorgesehen, teilweise aufgebaut oder vom Standard impliziert — aber nicht tatsächlich implementiert.

### MI1 — Google-Sheets-Authentifizierung ✅ IMPLEMENTIERT

**Ort:** `Runtime/Code/Loca/GoogleServiceAccountAuth.cs`, `Runtime/Code/Loca/LocaGettextSheetsSyncer.cs`

OAuth2-Authentifizierung mittels Google-Service-Account-JSON ist vollständig über `GoogleServiceAccountAuth.cs` implementiert.

**Was umgesetzt wurde:**
- Der Schalter `[Push new keys]` hängt nur Zeilen an, die noch nicht im Sheet vorhanden sind, und lässt bestehende Übersetzerarbeit unberührt
- Der Schalter `[Pull from Sheets]` überschreibt lokale PO-Dateien mit den Sheet-Werten (Übersetzer-Korrekturen haben Vorrang); verwendet `UNFORMATTED_VALUE` für rohe Zellinhalte; legt vor dem Überschreiben zeitgestempelte Backups an
- Das Flag `AutoSyncAfterMerge` in `UiToolkitConfiguration` aktiviert die automatische Synchronisation nach einem Merge-Vorgang

---

### MI2 — PO-Kontext (`msgctxt`) wird nicht geparst ✅ IMPLEMENTIERT

**Ort:** `Runtime/Code/Loca/LocaManagerDefaultImpl.cs`

`msgctxt`-Zeilen werden nun vollständig vom PO-Parser verarbeitet. Der Kontext wird über `ComposeContextKey()` nach der GNU-Gettext-Konvention `"kontext\u0004schlüssel"` in den Dictionary-Schlüssel einbezogen. Eine `LocaManager.Translate(key, context, group)`-Überladung steht für kontextbewusste Lookups bereit. Dies behebt W2.

---

### MI4— `LocaPlurals` ohne Fallback-Regel für unbekannte Sprachen ❌ Hoch

**Ort:** `Assets/Generated/LocaPlurals.cs` (generierte Datei)

Die generierte `switch`-Anweisung deckt nur die Sprachen ab, die beim letzten Ausführen von „Process Loca" vorhanden waren (`dev`, `de`, `en_us`, `lol`, `ru` in der Demo-App). Für jede nicht aufgeführte Sprache verbleiben `nplurals` und `pluralIdx` bei `0`, wodurch jede Plural-Abfrage stillschweigend die Singularform zurückgibt — ohne jede Warnung.

**Folgen:**
- Das Hinzufügen einer neuen Sprache in einer Excel-Tabelle fügt ihre Pluralregeln nicht automatisch hinzu
- Stiller Datenverlust: Pluralübersetzungen werden geladen, aber nie ausgewählt
- Entwickler müssen nach jeder neuen Sprache manuell „Process Loca" ausführen

**Was fehlt:** Ein `default`-Case mit englischähnlichen Regeln (`nplurals=2; plural=(n!=1)`) als sicherer Fallback, sowie ein `LogWarning` bei nicht registrierter Sprache.

---

### MI5 — `ILocaProvider`-Laufzeitladen auf `LocaExcelBridge` hardcodiert ❌ Mittel

**Ort:** `Runtime/Code/Loca/LocaManagerDefaultImpl.cs`

`ReadLocaProviders()` ruft `Resources.Load<LocaExcelBridge>(path)` mit hartkodiertem konkretem Typ auf. Das `ILocaProvider`-Interface ist für Erweiterbarkeit ausgelegt, aber jeder neue Provider-Typ (JSON, REST-API, Datenbank) kann nicht registriert werden, ohne `LocaManagerDefaultImpl` zu ändern. Der JSON-Ausgabepfad existiert bereits (`WriteJson()`), JSON wird jedoch nie als Provider zur Laufzeit eingelesen.

**Was fehlt:** Entweder den Provider-Typ zusammen mit dem Pfad in `LocaProviderList` speichern oder ein Factory-/Registry-Muster verwenden, damit neue `ILocaProvider`-Implementierungen ohne Kerncode-Änderungen hinzugefügt werden können.

---

### MI6 — `LocaPreBuildProcessor` ohne Fehlerbehandlung und Validierung ❌ Mittel

**Ort:** `Assets/Demo/Editor/LocaPreBuildProcessor.cs`

Der Pre-Build-Prozessor besteht aus zwei Log-Zeilen um einen einzigen `LocaProcessor.ProcessLocaProviders()`-Aufruf. Er:
- behandelt oder meldet Download-Fehler von Google Sheets nicht
- prüft nicht, ob alle erwarteten Sprachen nach dem Sync vorhanden sind
- prüft nicht, ob Schlüssel unübersetzt sind
- erkennt veraltete Übersetzungsdaten nicht
- bietet keinen Rollback-Mechanismus bei fehlgeschlagenem Sync

Ein Build kann daher stillschweigend mit fehlenden oder leeren Übersetzungen erfolgreich abschließen.

**Was fehlt:** Rückgabe-/Throw-bei-Fehler, Sprachabdeckungs-Check und eine Option, unübersetzte Schlüssel als Build-Warnungen oder -Fehler zu behandeln.

---

### MI7 — Kein Laufzeit-Provider-Switching ❌ Niedrig

Der Editor unterstützt mehrere `LocaExcelBridge`-Assets und eine `LocaProviderList`-Registry. Zur Laufzeit werden jedoch alle Provider einmalig in `ChangeLanguageImpl()` geladen. Es gibt keine API, um Provider dynamisch hinzuzufügen, zu entfernen oder auszutauschen (z. B. für DLC-Sprachpakete oder Live-Update-Szenarien). Das `ILocaProvider`-Interface hat keinen Laufzeit-`Load`/`Unload`-Lebenszyklus.

---

### MI8 — PO-Übersetzerkommentare werden nicht an den Editor weitergeleitet ❌ Niedrig

PO-Dateien unterstützen `#.` (Übersetzerkommentar), `#:` (Quellenreferenz) und `#,` (Flags wie `fuzzy`). Der PO-Parser verwirft alle Kommentarzeilen. Insbesondere werden `fuzzy`-Einträge — die Übersetzer verwenden, um Strings nach einer Quelländerung als überprüfungsbedürftig zu markieren — stillschweigend als gültige Übersetzungen akzeptiert. Es gibt keinen Editor-Indikator für unsichere oder ungeprüfte Strings.

---

### MI9 — `UiLocalizedTextMeshProUGUI`: TMP-Unterklasse mit integrierter Lokalisierung ✅ IMPLEMENTIERT

**Ort:** `Runtime/Code/Loca/UiLocalizedTextMeshProUGUI.cs`

`UiLocalizedTextMeshProUGUI` ist als `TextMeshProUGUI`-Unterklasse mit vollständig integrierter Lokalisierung implementiert.

**Was umgesetzt wurde:**
- `UiAutoLocalize.cs` bleibt für die Rückwärtskompatibilität erhalten, wird jedoch von `UiLocalizedTextMeshProUGUI` abgelöst
- Das Editor-Werkzeug `ReplaceComponentsWindow` übernimmt Komponenten-Tausch und YAML-Referenz-Aktualisierungen in allen Szenen und Prefabs

---



Das System ist **gut strukturiert und produktionstauglich**. Es integriert erfolgreich Gettext-Workflows, Excel-basierte Übersetzung, Pluralregeln und dynamischen Laufzeit-Sprachwechsel. Die wirkungsvollsten Verbesserungen sind:

1. **Thread-Sicherheit** (Korrektheit)
2. **Fehlende-Schlüssel-Reporting** (Qualitätssicherung)
3. **Sprachkennung-Normalisierung** (Zuverlässigkeit)
4. **Gruppen-Fallback-Kette** (Benutzerfreundlichkeit)
5. **Asynchrones Laden** (Performance)

**Dringendste fehlende Implementierungen:**

| ID | Funktion | Schweregrad |
|---|---|---|
| MI1 | Google-Sheets-Authentifizierung (OAuth2 / Service Account) | ✅ Implementiert |
| MI2 | PO-`msgctxt`-Parsing und kontextbewusste Übersetzung | ✅ Implementiert |
| MI4 | `LocaPlurals` Default-/Fallback-Regel für unbekannte Sprachen | 🟠 Hoch |
| MI5 | `ILocaProvider`-Laufzeit-Erweiterbarkeit (Typ nicht hardcodiert) | 🟡 Mittel |
| MI6 | `LocaPreBuildProcessor` Fehlerbehandlung und Abdeckungsvalidierung | 🟡 Mittel |
| MI7 | Laufzeit-Provider-Switching (DLC / Live-Update) | 🔵 Niedrig |
| MI8 | PO-`fuzzy`-Flag / Übersetzerkommentare im Editor anzeigen | 🔵 Niedrig |
| MI9 | `UiLocalizedTextMeshProUGUI` TMP-Unterklasse + Migrations-Tool | ✅ Implementiert |

---

## Vergleich mit dem Unity Localization Package

### Vorteile des Unity-Pakets gegenüber dem GUI-Toolkit

| Funktion | Unity Built-in | GUI Toolkit |
|---|---|---|
| Asset-Lokalisierung (Sprites, AudioClips, Prefabs) | ✅ Vollständig | ❌ Nur Text |
| Smart Strings (ICU-basierte Variablensubstitution) | ✅ | ❌ |
| Automatische Spracherkennung (OS-Sprache) | ✅ | ❌ (W5, erfordert Projektcode) |
| Asynchroner Sprachwechsel | ✅ Vollständig asynchron | ❌ Synchron (W7) |
| Vorlade-Gruppen | ✅ | ❌ |
| Pseudo-Lokalisierung | ✅ Eingebaut | Teilweise (`dev`-Sprache) |
| Visueller Tabellen-Editor | ✅ StringTable-Editor | ❌ Externer PO-Editor |
| Metadaten/Kommentare pro Eintrag | ✅ | ❌ Verworfen (MI8) |
| XLIFF-Import/-Export | ✅ | ❌ |
| Vorschau im Play-Modus ohne Build | ✅ | ✅ (funktioniert auch im Edit-Modus) |
| Thread-Sicherheit | ✅ | ❌ (W1) |
| Fuzzy-Einträge | ✅ | ❌ (MI8) |
| Platzhalter-Verfolgung in Übersetzungen | ✅ (Smart Strings) | ❌ |
| Konfigurierbare Locale-Fallback-Kette | ✅ | Teilweise (dev → Schlüssel als Fallback) |
| Übersetzerkommentare pro Eintrag | ✅ | ❌ |

### Vorteile des GUI-Toolkits gegenüber Unity Built-in

| Funktion | Unity Built-in | GUI Toolkit |
|---|---|---|
| Gettext PO/POT-Standard | ❌ Kein PO-Support | ✅ Vollständig |
| Crowdin / Weblate / Lokalise-Integration | Eingeschränkt (CSV/XLIFF) | ✅ Direkt über PO |
| Google-Sheets-Push/Pull-Workflow | Über Erweiterungspaket | ✅ Eingebaut, code-getrieben |
| Excel-XLSX-Import | Über Erweiterungspaket | ✅ Eingebaut |
| Entwicklerfreundliche API | `GetLocalizedString(table, key)` – ausführlich | ✅ `_("key")` – prägnant |
| Versionskontrolle-freundlich | ❌ Binäre `.asset`-Tabellen | ✅ Textuelle PO-Dateien, diff-fähig |
| Automatische Schlüsselextraktion aus C# | ❌ Nur manuell | ✅ Regex-Scan + `ILocaKeyProvider` |
| Quellenreferenzen in Templates | ❌ | ✅ GO-Pfad + Zeilenauszug |
| Gruppen / Namespaces | Über Tabellen | ✅ Gruppen-Parameter |
| Keine Addressables-Abhängigkeit | Erfordert Addressables | ✅ Resources-basiert (optional) |
| JSON-Schlüsselextraktion | ❌ | ✅ `LocaJsonKeyProvider` |
| Level-abhängige lokalisierte Texte | ❌ | ✅ `UiAbstractLocalizedTextByPlayerLevel` |
| Kontextbewusste Übersetzungen (msgctxt) | ❌ Kein PO-Kontext | ✅ `ComposeContextKey()` |

---

## Fehlende Funktionen im Vergleich zu Unity Built-in

| Priorität | Funktion | Anmerkungen |
|---|---|---|
| 🔴 Hoch | Asset-Lokalisierung (Sprites, Audio, Prefabs) | Unity unterstützt beliebige Asset-Typen pro Locale |
| 🔴 Hoch | Asynchroner Sprachwechsel | Frame-Drop bei großen Katalogen (W7) |
| 🟠 Mittel | Smart Strings / ICU-Variablensubstitution | Unitys `{count, plural, …}`-Syntax |
| 🟠 Mittel | Pseudo-Lokalisierungs-Werkzeug | Layout-Stresstest mit erweiterten Strings |
| 🟠 Mittel | Automatische Spracherkennung (OS-Sprache) | Derzeit Projektcode erforderlich (W5) |
| 🟡 Niedrig | XLIFF-Import/-Export | Branchenstandard für CAT-Werkzeuge |
| 🟡 Niedrig | Fuzzy-Einträge / Übersetzerkommentare im Editor | PO-Metadaten werden derzeit verworfen (MI8) |
| 🟡 Niedrig | Visueller Tabellen-Editor | Unity bietet einen umfangreichen StringTable-Editor |
| 🟡 Niedrig | Metadaten pro Eintrag (Notizen, Autor, Vollständigkeit) | Unity unterstützt detaillierte Eintrag-Metadaten |
| 🔵 Minimal | Vorlade-Gruppen | Unity kann bestimmte Locale-Gruppen vorladen |
