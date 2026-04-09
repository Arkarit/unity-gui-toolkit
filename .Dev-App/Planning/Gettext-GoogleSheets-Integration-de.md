# Gettext ↔ Google Sheets Integration

## Ziel

Entwickler definieren Lokalisierungskeys direkt im Code (`_()`, `gettext()`, etc.).
Diese Keys finden automatisch den Weg in Google Sheets, sodass Übersetzer in ihrer
gewohnten Umgebung arbeiten können. Übersetzungen kommen dann zurück ins Projekt.

---

## Angedachter Datenfluss

```
Code (_(), gettext(), …)
        │
        ▼
  LocaProcessor          ← bereits implementiert
        │ POT-Datei(en)
        ▼
  PoMergeEngine          ← bereits implementiert
        │ PO-Datei(en) (neue Keys leer, bestehende Übersetzungen erhalten)
        ▼
  [PUSH] Google Sheets   ← NEU: nur neue Zeilen hinzufügen, nie überschreiben
        │
  Übersetzer füllen Sheets aus
        │
        ▼
  [PULL] PO-Dateien      ← teilweise implementiert (LocaExcelBridge liest bereits)
        │
        ▼
     Runtime
```

### Push-Richtung (PO → Sheets)
- **Konservativ**: nur neue Zeilen (Keys) anhängen, die noch nicht in Sheets existieren
- Bestehende Übersetzungen werden **niemals** überschrieben
- Leere `msgstr`-Felder werden als leere Zellen eingefügt (Übersetzer füllen aus)
- Wenn Sheets noch leer ist: Header-Zeile anlegen, dann Keys einfügen

### Pull-Richtung (Sheets → PO)
- **Autoritativ**: Sheets ist SSoT für Übersetzungen
- Aktualisiert `msgstr` in PO-Dateien für alle Keys die in Sheets einen Wert haben
- Keys die im PO aber nicht in Sheets existieren → bleiben unberührt (merge, nicht ersetzen)
- SSoT-Protection (`PoSsotProtector`) greift falls jemand PO-Dateien manuell bearbeitet

---

## PO als sinnvoller Zwischenschritt

Obwohl ein direkter Code→Sheets-Weg möglich wäre, lohnt der PO-Zwischenschritt:
- **Lokaler Cache**: Projekt funktioniert offline
- **Git-History**: Übersetzungsänderungen sind nachvollziehbar
- **Plural-Handling**: PO-Format verwaltet Pluralformen sauber; Mapping zu Sheets-Spalten ist bereits in `LocaExcelBridge` implementiert
- **Bereits implementiert**: `LocaProcessor`, `PoMergeEngine` und der PO-Parser sind alle vorhanden; nur die Brücke Richtung Sheets fehlt

---

## Offene Designentscheidungen

Alle Entscheidungen getroffen:

### 1. msgctxt → Sheets-Spalten ✅
`msgctxt` → `KeyPrefix` mappen (in Bridge-Config bereits vorhanden, keine neue Spalte nötig).

### 2. Plural-Formen ✅
Das bestehende Mapping aus `LocaExcelBridge.CollectData()` für Push einfach umkehren.
Kein neues Design erforderlich — nur Implementierung.

### 3. Bridge-Konfiguration: 1:1 oder n:m? ✅
**n:m**: Mehrere `LocaExcelBridge`-Instanzen können jeweils eine andere PO-Gruppe abdecken
und auf separate Sheets zeigen. Eine Bridge pro Gruppe.

### 4. Spaltenformat beim ersten Push ✅
**Quelle: Bridge-Konfiguration** (`m_columnDescriptions`), nicht die PO-Dateien.

Ein **[Create by PO]**-Button im Inspector erzeugt/aktualisiert die Bridge-Spaltenkonfiguration
aus den aktuell im Projekt vorhandenen PO-Dateien:
- Falls die Bridge bereits Spalten hat: Bestätigungsdialog ("Spalten aus PO-Dateien synchronisieren? OK / Abbrechen")
- **Bestehende** Spalten beibehalten
- **Unbekannte** Sprachen (nicht in PO-Dateien) ignorieren
- **Neue** Sprachen aus PO-Dateien ans Ende anhängen

Das hält die Konfiguration explizit und vorhersehbar, bietet aber trotzdem einen
unkomplizierten Einstieg.

---

## Was wegfällt

- **XLSX-Schreiben**: macht keinen Sinn (wer PO→Excel will, nutzt ein externes Tool)
- `LocaExcelBridgePusher` für XLSX-Pfad kann vereinfacht oder entfernt werden
- Push-Logik als eigenständige "Sync"-Aktion implementieren, **nicht** als Teil des allgemeinen Bridge-Mechanismus

---

## Geplante neue Komponenten / Änderungen

| Was | Wo | Beschreibung |
|---|---|---|
| `LocaGettextSheetsSyncer` | `Editor/Loca/` | Sync-Logik PO↔Sheets (Push + Pull) |
| `LocaGettextSheetsSyncerEditor` | `Editor/Components/` | Inspector-UI / Menüpunkte für Sync |
| Erweiterung `LocaExcelBridge` | `Runtime/Code/Loca/` | Methoden für Pull mit Merge-Semantik |
| Erweiterung `LocaProcessor` | `Editor/Loca/` | Optionaler Auto-Sync nach POT-Extraktion |
| Erweiterung `GoogleServiceAccountAuth` | `Runtime/Code/Loca/` | Write-Scope bereits implementiert ✓ |

---

## Nächste Schritte

1. Designentscheidung klären: 1:1 oder n:m Bridge-Gruppen?
2. Sheets-Spaltenformat für Push festlegen (insb. Context-Spalte und Plural-Spalten)
3. Implementierung `LocaGettextSheetsSyncer` (Push + Pull)
4. Integration in `LocaProcessor` (Auto-Sync-Option)
5. Dokumentation in gh-pages aktualisieren
