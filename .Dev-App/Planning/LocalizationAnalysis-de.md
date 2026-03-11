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

---

## Schwächen

### W1 — Keine Thread-Sicherheit ⚠️
`m_translationDict` wird ohne Locks zugegriffen. Falls eine Hintergrund-Coroutine während `ChangeLanguage()` `Translate()` aufruft, ist eine Race-Condition möglich. Die `Thread.CurrentThread`-Kulturaktualisierungen betreffen zudem sämtlichen Code auf diesem Thread global.

### W2 — Keine kontextbewussten Übersetzungen
Der PO-Standard unterstützt `msgctxt` zur Disambiguierung identischer Quellstrings; das System ignoriert dies. Als Workaround müssen Schlüssel manuell mit Präfixen versehen werden, was den Schlüssel-Namespace aufbläht.

### W3 — Stille Fallbacks bei fehlenden Schlüsseln in der Produktion
Der `DebugLoca`-Modus muss manuell aktiviert werden. Fehlende Übersetzungen geben stillschweigend den Schlüsselnamen zurück. Es gibt keine eingebaute Sammlung oder Auswertung nicht übersetzter Strings, sodass Vollständigkeit auf Anhieb nicht messbar ist.

### W4 — Inkonsistente Sprachkennungen
Sprach-IDs sind bei Lookups case-sensitiv, werden aber zur Laufzeit auf Kleinbuchstaben normiert. Eine Nichtübereinstimmung zwischen Excel-Spalten-Headern und PO-Dateinamen (z. B. `en-US` vs. `en_us`) führt zu stillem Ladefehler.

### W5 — Keine automatische Systemspracherkennung
Beim ersten Start fällt das System immer auf `"dev"` zurück. Die OS-Sprache (`CultureInfo.CurrentCulture`) wird nie berücksichtigt; jedes integrierende Projekt muss eigene Erststart-Logik implementieren.

### W6 — Kein Gruppen-Fallback
Wird ein Schlüssel in der angeforderten Gruppe nicht gefunden, fällt das System **nicht** automatisch auf die Standardgruppe zurück. Anwendungen müssen Schlüssel entweder duplizieren oder selbst einen Fallback implementieren.

### W7 — Synchroner Sprachwechsel
Alle PO-Dateien einer Sprache werden synchron in `ChangeLanguageImpl()` geladen. Bei großen Katalogen führt dies zu einem spürbaren Frame-Drop beim Sprachwechsel.

### W8 — Format-String-Platzhalter werden ignoriert
Das System verfolgt `{0}`, `{1}`-Platzhalter in Schlüsseln nicht. Übersetzer sehen nicht, wofür die Platzhalter stehen; vertauschte Platzhalter verursachen stille Laufzeitfehler.

### W9 — TextAsset-Speicher wird nicht freigegeben
Als `TextAsset` geladene PO-Dateien werden beim Sprachwechsel nie entladen. In Projekten mit vielen Sprachen kann sich erheblicher Speicherverbrauch ansammeln.

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
| H2 | **Sprachkennungen normalisieren** — zentrale `NormalizeLanguageId()`-Methode (Ersetze `-` durch `_`, Kleinbuchstaben) | Gering | Gering |
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
| M6 | **Sprach-Cache-Eviction** — `TextAsset`s der vorherigen Sprache entladen | Mittel | Mittel |
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

### MI1 — Google-Sheets-Authentifizierung ❌ Kritisch

**Ort:** `Runtime/Code/Loca/LocaExcelBridge.cs`

Der Google-Sheets-Download verwendet ein einfaches `UnityWebRequest.Get(url)` ohne jede Authentifizierung. Es gibt weder einen OAuth2-Flow noch Unterstützung für Service-Account-JSON oder API-Keys. Die Integration funktioniert stillschweigend nur für öffentlich freigegebene Tabellen. Jede private oder organisationsgebundene Tabelle schlägt ohne hilfreiche Fehlermeldung fehl.

**Folgen:**
- Nicht nutzbar für private Daten (was bei realen Projektübersetzungen die Regel ist)
- Sicherheitsanforderungen (OAuth2 oder Service Account) sind nicht erfüllt
- Nicht produktionstauglich für Teams mit Google-Workspace-Zugangsbeschränkungen

**Was fehlt:** Ein OAuth2-Device-Flow oder Service-Account-Authentifizierungsschritt vor dem Download, mit Credential-Speicherung in den Editor-Einstellungen.

---

### MI2 — PO-Kontext (`msgctxt`) wird nicht geparst ❌ Kritisch

**Ort:** `Runtime/Code/Loca/LocaManagerDefaultImpl.cs` (PO-Parser-Abschnitt)

Der PO-Standard definiert `msgctxt` zur Disambiguierung identischer Quellstrings, die unterschiedliche Übersetzungen benötigen (z. B. „Speichern" als Verb vs. Substantiv in manchen Sprachen). Der Parser überspringt `msgctxt`-Zeilen vollständig. `ProcessedLocaEntry` hat kein Kontext-Feld; das Übersetzungs-Dictionary verwendet nur den Schlüssel. Zwei PO-Einträge mit gleichem `msgid`, aber unterschiedlichem `msgctxt`, kollabieren stillschweigend zu einem einzigen Eintrag.

**Folgen:**
- Keine kontextbewussten Übersetzungen möglich
- Homograph-Disambiguierung (z. B. „Bank" — Finanzinstitut / Flussufer) erfordert manuelle Schlüssel-Präfixe
- Standard-PO-Dateien professioneller Übersetzungswerkzeuge mit `msgctxt` werden falsch geparst

**Was fehlt:** Parsen von `msgctxt` in `ProcessedLocaEntry`; Kontext im Dictionary-Schlüssel einschließen (z. B. `"kontext\u0004schlüssel"`); `Translate(key, context, group)`-Überladung bereitstellen.

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

### MI9 — `UiLocalizedTextMeshProUGUI`: TMP-Unterklasse mit integrierter Lokalisierung ❌ Hoch

**Ort:** `Runtime/Code/Loca/UiAutoLocalize.cs` (wird abgelöst)

`UiAutoLocalize` ist eine separate MonoBehaviour-Komponente neben `TextMeshProUGUI`, die bei Sprachwechseln `Translate()` aufruft. Dieses Zwei-Komponenten-Design ist fragil: Nichts verhindert, dass anderer Code direkt auf `TMP_Text.text` schreibt und damit die Lokalisierung still überschreibt oder von ihr überschrieben wird.

**Vorgeschlagene Architektur:** Eine Unterklasse `UiLocalizedTextMeshProUGUI : TextMeshProUGUI`, die:
- Den `text`-Setter überschreibt: Bei `m_autoLocalize = true` wird der Rohwert als `m_locaKey` gespeichert und sofort die übersetzte Version angezeigt
- Ein `m_isSettingInternally`-Guard-Flag verhindert Endlosschleifen im überschriebenen Setter
- Den Sprachenwechsel-Callback implementiert und `m_locaKey` automatisch neu übersetzt
- In `#if UNITY_EDITOR`: Externe Schreibzugriffe bei aktiviertem Auto-Localize erkennt und ein `Debug.LogWarning` ausgibt
- Bei `m_autoLocalize = false`: Verhält sich exakt wie `TextMeshProUGUI` ohne jeglichen Overhead

**Migrationspfad:**
- `UiAutoLocalize` wird mit `[Obsolete]` und einem Migrationshinweis in `Awake()` markiert
- Ein Editor-Menü-Tool scannt alle Scenes und Prefabs nach `UiAutoLocalize + TMP_Text`-Paaren und ersetzt sie durch `UiLocalizedTextMeshProUGUI`

**Was fehlt:** `Runtime/Code/Loca/UiLocalizedTextMeshProUGUI.cs` (neu), `UiAutoLocalize.cs` deprecaten, `Editor/Loca/UiLocalizedTextMigrationTool.cs` (neues Migrations-Tool).

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
| MI1 | Google-Sheets-Authentifizierung (OAuth2 / Service Account) | 🔴 Kritisch |
| MI2 | PO-`msgctxt`-Parsing und kontextbewusste Übersetzung | 🔴 Kritisch |
| MI4 | `LocaPlurals` Default-/Fallback-Regel für unbekannte Sprachen | 🟠 Hoch |
| MI5 | `ILocaProvider`-Laufzeit-Erweiterbarkeit (Typ nicht hardcodiert) | 🟡 Mittel |
| MI6 | `LocaPreBuildProcessor` Fehlerbehandlung und Abdeckungsvalidierung | 🟡 Mittel |
| MI7 | Laufzeit-Provider-Switching (DLC / Live-Update) | 🔵 Niedrig |
| MI8 | PO-`fuzzy`-Flag / Übersetzerkommentare im Editor anzeigen | 🔵 Niedrig |
| MI9 | `UiLocalizedTextMeshProUGUI` TMP-Unterklasse + Migrations-Tool | 🟠 Hoch |
