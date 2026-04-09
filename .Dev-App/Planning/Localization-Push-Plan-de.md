# Implementierungsplan: Lokalisierungs-Push & Gettext↔Spreadsheet‑Integration

## Ziel
Ziel ist es, Push-/Export‑Funktionen in die Lokalisierungs‑Toolchain zu integrieren, sodass Übersetzungen zurück in lokale Excel‑Dateien und Google Sheets geschrieben werden können. Außerdem soll die Pipeline so erweitert werden, dass POT → PO → (optional) Spreadsheet als auditierbarer Workflow abläuft.

Der Umfang umfasst Editor‑Werkzeuge (UI und Scripts), sichere Handhabung von Service‑Account‑Schlüsseln, Tests/CI sowie Dokumentations‑Updates. Das Laufzeitverhalten von `ILocaProvider` bleibt unverändert; diese Arbeit betrifft primär Editor‑ und Build‑Werkzeuge.

## Zusammenfassung des vorgeschlagenen Workflows
1. Quelltext auf `_()`, `pgettext()`, `_n()` usw. scannen und POT‑Templates aktualisieren.
2. POT‑Änderungen in vorhandene PO‑Dateien mergen (Editor‑Merge: neue `msgid` anlegen, vorhandene `msgstr` bewahren wenn möglich).
3. Optional: Aktualisierte PO‑Daten an eine oder mehrere konfigurierte `LocaExcelBridge`‑Instanzen schicken und dort in `.xlsx` oder Google Sheets schreiben.

Damit schließt sich der Loop: Übersetzer arbeiten in Tabellen, Code‑Änderungen werden in die Übersetzungsassets zurückgeführt.

## Kurzbewertung: Nutzen & Risiken
- Nutzen: Vereinfachung des Übersetzer‑Workflows, weniger Copy/Paste, klare Audit‑Spur von Quelltext → Übersetzungen, schnellere Verteilung von Updates.
- Risiken: Verlust von Kontext/Pluralmapping (msgctxt ↔ Schlüssel, msgstr[n] Indizes), Merge‑Konflikte, unbeabsichtigtes Überschreiben manuell gepflegter Übersetzungen, Timeouts/Performance bei großen Sheets und Sicherheitsrisiken bei unsicherer Handhabung von Service‑Account‑Schlüsseln.
- Gegenmaßnahmen: per‑Bridge Opt‑in, Dry‑Run/Preview, konservatives Merge‑Default (nur neue Keys + leere `msgstr` automatisch füllen), Metadaten & automatische Backups vor Überschreiben.

## Grobe Implementierungsphasen

Phase 0 — Design & Entscheidungen (keine Code‑Änderungen)
- XLSX‑Writer auswählen (ClosedXML empfohlen vs. EPPlus vs. CSV‑Fallback) und auf Unity‑Editor‑Kompatibilität prüfen.
- Merge‑Strategie festlegen (overwrite vs. merge‑only vs. interaktives Review).
- UI‑Flow entscheiden: automatischer Push nach Extraktion oder explizite "Push to Bridges"‑Aktion.
- Gewünschte Google‑Scopes (Schreibrechte) und CI‑Secret‑Handling klären.

Phase 1 — Google Sheets Push (Editor)
- Push‑API implementieren (entweder in `LocaExcelBridge` oder in neuem `LocaPushService`): (a) nimmt `ProcessedLoca`/PO‑Daten entgegen, (b) mapped auf Spaltenlayout, (c) verwendet `spreadsheets.values.batchUpdate` oder ersetzt/erstellt Tabellenblätter.
- `GoogleServiceAccountAuth` erweitern, optional Schreib‑Scopes anfordern, Token‑Caching und Prüf‑Helper für Service‑Account‑Email/Permissions ergänzen.
- Editor‑Button/Inspector: `Process` → `Push to Google` mit Optionen (Dry‑Run, Overwrite‑Policy, Ziel‑Sheet‑ID).
- Logging und Bestätigungsdialoge; Push nur nach expliziter Freigabe.
- Abnahmekriterium: Kleines Übersetzungstabelle kann erfolgreich gepusht werden; Dry‑Run zeigt Änderungen an.

Phase 2 — Lokaler Excel‑Export
- `.xlsx`‑Writing mit gewählter Bibliothek implementieren, Spalten für Key/Context und Sprach‑Pluralspalten erzeugen.
- Inspector‑Aktion `Push to .xlsx` (Merge in bestehende Datei oder neue Datei erzeugen).
- Fallback: CSV‑Export für CI/Minimalfälle.
- Abnahmekriterium: Export öffnet korrekt in Excel/LibreOffice und enthält erwartete Spalten.

Phase 3 — Gettext ↔ Spreadsheet‑Pipeline Integration
- `LocaProcessor` optional erweitern, sodass nach POT‑Extraktion ein Merge‑Schritt ausgeführt wird.
- Merge‑Engine implementieren: (a) vorhandene PO laden, (b) POT‑Diff anwenden, (c) sicher mergen (Übersetzungen bewahren; Konflikte markieren), (d) aktualisierte PO schreiben.
- UI: Auswahl der Bridges und Sprachen, Dry‑Run/Review‑Screens.
- Abnahmekriterium: "Process Keys → Merge → Push" aktualisiert POT/PO lokal und schreibt nach Bestätigung in ausgewählte Bridges.

Phase 4 — Pull & Spreadsheet als Single Source of Truth (SSoT)
- Pull‑API für `LocaExcelBridge` implementieren: Spreadsheet als autoritative Quelle importieren.
- Mapping‑Regeln: Context‑Spalte, Plural‑Spalten, Metadaten‑Spalten (`last_modified`, `editor`, `revision`).
- UI/Policy: Pipeline‑Modus wählbar (PO‑first, Spreadsheet‑first/SSoT, Hybrid) und deutliche Warnhinweise für generierte PO/Dateien.
- Automatische Header‑Metadaten in generierten PO (Bridge‑ID, URL, Timestamp).
- Backups & Schutz: Editor‑Warnung bei manuellen Bearbeitungen, automatische Backups vor Überschreiben, „Make Local Copy“‑Aktion.
- Abnahmekriterium: Pull funktioniert, generiert PO‑Dateien und warnt vor manuellen Änderungen.

Phase 5 — Tests & CI
- Editor‑Tests: PO‑Merge‑Logik, Mapping → Bridge‑Schema, Push/Pull Dry‑Run, Push/Pull Flows (mit gemockter Google API).
- CI: Skripte für headless Import/Export (Secrets sicher injizieren).
- Abnahmekriterium: Kritische Logik durch Tests abgedeckt und in CI ausführbar.

Phase 6 — Doku, UX‑Polish, Release
- Dokumentation und gh‑pages Seiten aktualisieren (Anleitungen für Push/Pull, Sicherheitshinweise).
- Screenshots, Migration‑Hinweise, Release‑Note mit Opt‑in‑Verhalten.

## Schutz & Backup (wie angefordert)
- PO/POT, die aus einem SSoT‑Bridge generiert wurden, erhalten am Datei‑Anfang einen Header mit Metadaten, z.B.:

```
# Generated from Spreadsheet SSoT
# Bridge: MyGoogleBridge (GUID: 1234-...)
# Source: https://docs.google.com/spreadsheets/d/ABCDEF
# Generated: 2026-03-11T12:05:00Z
# DO NOT EDIT MANUALLY — Use the linked spreadsheet or use "Make Local Copy" to detach.
```

- Editor‑Verhalten beim Öffnen/Bearbeiten dieser Dateien:
  - Warn‑Dialog mit Aktionen: "Abbrechen", "Lokale Kopie erstellen (entkoppeln)", "Trotzdem bearbeiten (Backup erstellen)".
  - Beim Fortfahren: automatische Sicherung nach `Assets/Localization/Backups/{dateiname}.{timestamp}.po` und Protokollierung der Aktion.
  - Menüaktion zum Auflisten und Wiederherstellen von Backups.

- Backup‑Aufbewahrung: Standardmäßig die letzten **10** Backups pro Datei behalten; ältere werden automatisch entfernt (konfigurierbar).

## Merge‑Policy (Empfehlung)
- **Spreadsheet‑first (SSoT)**: Spreadsheet ist autoritär — Pull überschreibt PO (mit Backup und Diff‑Report).
- **PO‑first (Default)**: POT/PO bleiben autoritär; Spreadsheet‑Änderungen nur per Push oder explizitem Merge.
- **Hybrid (empfohlen Default)**: Beim Pull nur leere `msgstr` füllen und neue Keys anhängen; nicht‑leere `msgstr`‑Konflikte werden gemeldet und erfordern manuelle Prüfung.

## Libraries & Abhängigkeiten
- Lesen: ExcelDataReader (bereits vorhanden) — beibehalten.  
- Schreiben: ClosedXML empfohlen (MIT‑Lizenz), EPPlus nur bei Lizenzakzeptanz prüfen; alternativ CSV‑Fallback.  
- Google API: HTTP‑Aufrufe per UnityWebRequest zu Google Sheets REST; OAuth via Service Account JWT‑Flow (bestehende Helper erweitern).

## Offene Design‑Entscheidungen
1. XLSX‑Writer: ClosedXML empfohlen (außer Du bevorzugst EPPlus).  
2. Default Merge: Konservatives Hybrid‑Verhalten (nur leere `msgstr` füllen).  
3. Push‑Verhalten: Explizit vom Nutzer angestoßen (kein automatischer Push nach Extraktion).

## Nächste Schritte
- Bestätige die drei offenen Design‑Entscheidungen oben.  
- Wenn bestätigt: zuerst Pull (SSoT) + Header/Backup/Warnung implementieren (sichere Grundlage), danach Push/Export.  
- Ich erstelle dann die konkreten Tasks/PR‑Checklisten und beginne mit der Implementierung gemäß Phasen.


---

Plan erstellt auf Anfrage und als deutsche Übersetzung des vorhandenen Plans. Passe gern Formulierungen oder Details an, dann übernehme ich die Änderungen und committe sie.