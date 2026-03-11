# Align gettext / pgettext Signaturen mit GNU gettext und die nicht‑standardmäßige _() Überladung kennzeichnen

## Zusammenfassung

Der Code enthält derzeit eine nicht‑standardmäßige pgettext/_() API‑Form (Reihenfolge von Kontext und msgid unterscheidet sich von GNU gettext und es gibt eine zusätzliche `_()` Überladung, die einen Kontext akzeptiert). Das bricht die Parität mit den gettext‑Konventionen und erschwert die POT‑Extraktion sowie das Verständnis für Mitwirkende. Dieses Dokument schlägt eine kompatibilitätsorientierte Refaktorisierung vor, um die API an den Standard anzugleichen und Extraktion sowie Tests zu aktualisieren.

## Hintergrund / Aktuelles Verhalten

- Wichtige Dateien: `Runtime/Code/Components/LocaMonoBehaviour.cs`, `Runtime/Code/Loca/LocaManager.cs` (und verwandte Implementierungen).
- Aktuelle Helfer:
  - `_(msgid)` — Standard‑Einzelargument‑Form (OK).
  - `_(msgid, context, group)` — eine Überladung, die den Kontext nach dem msgid akzeptiert (nicht‑standardmäßig).
  - `pgettext(msgid, context, group)` — implementiert mit `(msgid, context, group)` Reihenfolge (nicht‑standardmäßig).
- GNU gettext‑Konvention: `pgettext(context, msgid)` (Kontext zuerst); Tools wie `xgettext` erwarten diese Signatur (oder müssen speziell konfiguriert werden).

## Warum das wichtig ist

- Extraktionswerkzeuge (xgettext, benutzerdefinierte Scanner) und erfahrene Mitwirkende erwarten die standardisierte `pgettext(context,msgid)` Signatur; die aktuelle Reihenfolge kann zu fehlenden Extraktionen und Verwirrung führen.
- Abwärtskompatibilität mit minimaler Störung ist wichtig — viele Aufrufstellen und Tests hängen von den aktuellen Helfern ab.

## Vorgeschlagene Lösung (Kompatibilitäts‑zuerst)

1. Standard‑Signatur hinzufügen:
   - `protected static string pgettext(string context, string msgid, string group = null)` hinzufügen, die an `LocaManager.Instance.Translate(msgid, context, group)` delegiert.
2. Veraltete Wrapper für bestehende nicht‑standard Überladungen bereitstellen:
   - Die bestehende `pgettext(msgid, context, group)` und die Unterstrich‑Überladung `_(msgid, context, group)` mit `[Obsolete("Use pgettext(context, msgid, group) or gettext/_(msgid)")]` markieren und so implementieren, dass sie an die neue Standard‑Signatur weiterleiten, um das Laufzeitverhalten zu erhalten.
3. Die Einzelargument‑Form `_()` (gettext Shortcut) unverändert belassen.
4. Extraktions‑Tooling und Dokumentation aktualisieren:
   - Extraktionsskripte/Dokus so aktualisieren, dass `pgettext(context,msgid)` gesucht wird.
   - Für `xgettext` ein Beispiel konfigurieren: `--keyword=pgettext:1c,2` oder eine äquivalente Scanner‑Konfiguration.
   - Scanner so konfigurieren, dass sie vorübergehend beide Signaturen erkennen, bis die Migration abgeschlossen ist.
5. Tests und Dokumentation:
   - Unit‑Tests hinzufügen, die `pgettext(context,msgid)` mit `Translate(msgid, context)` vergleichen und sicherstellen, dass die veralteten Wrapper identische Übersetzungen liefern.
   - Lokalisierungsdokumentation aktualisieren (EN und `-de.md`) mit Migrationshinweisen.
6. Migrationsplan:
   - Phase A (nicht‑brechend): Standard‑`pgettext(context,msgid)` und veraltete Weiterleitungs‑Wrapper hinzufügen; Doks und Extraktion aktualisieren.
   - Phase B (optionale Entfernung): Nach einer Deprecation‑Periode die alten Überladungen in einem späteren Major Release entfernen.

## Implementierungs‑Hinweise / Zu ändernde Dateien

- `Runtime/Code/Components/LocaMonoBehaviour.cs`: Standard‑`pgettext(context,msgid)` hinzufügen, alte Überladungen `[Obsolete]` markieren, XML‑Docs aktualisieren.
- `Runtime/Code/Loca/LocaManager.cs` und `LocaManagerDefaultImpl.cs`: Sicherstellen, dass `Translate`‑Überladungen verfügbar bleiben; Unit‑Tests ergänzen, die Gleichheit prüfen.
- Tests: `TestPgettextSignatures` in `Tests/Editor` hinzufügen.
- Extraktionstooling: xgettext/Scanner mit `--keyword=pgettext:1c,2` konfigurieren; Änderung dokumentieren.
- Dokumentation: `Documentation/Localization` aktualisieren und eine `-de.md` Kopie anlegen.

## Akzeptanzkriterien

- Neue `pgettext(context,msgid[,group])` API ist vorhanden und dokumentiert.
- Alte, nicht‑standard Überladungen funktionieren weiterhin, sind aber `[Obsolete]` und leiten an die neue API weiter.
- Extraktionsanleitungen sind aktualisiert.
- Unit‑Tests prüfen die Gleichheit zwischen neuer und alter Signatur und laufen grün.
- Migrationshinweis in der Lokalisierungsdokumentation vorhanden.

## Vorgeschlagene Labels

`area:localization`, `type:refactor`, `priority:medium`, `breaking-change:no` (Phase A)

## PR‑Checkliste

- [ ] `pgettext(context,msgid[,group])` implementieren
- [ ] `[Obsolete]` Wrapper für Legacy‑Signaturen hinzufügen
- [ ] `LocaMonoBehaviour` XML‑Dokumentation aktualisieren
- [ ] Extraktionsskripte aktualisieren und Beispiel für xgettext hinzufügen
- [ ] Unit‑Tests ergänzen (`TestPgettextSignatures`)
- [ ] Lokalisierungsdokumentation (EN und `-de.md`) aktualisieren
- [ ] Editmode‑Tests ausführen und grünes Ergebnis sicherstellen
- [ ] Folge‑TODO zur Entfernung der veralteten Wrapper nach der Deprecation‑Periode

---

Hinweis

Dieser Ansatz bewahrt die Laufzeitkompatibilität und bewegt die API in Richtung der Standard‑gettext‑Semantik. Phase A ist nicht‑brechend und erlaubt eine schrittweise Migration, bevor Legacy‑Wrappers in einem Major Release entfernt werden.
