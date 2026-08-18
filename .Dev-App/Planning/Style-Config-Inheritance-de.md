# Vererbung der Style-Konfiguration

## Überblick

Dieses Dokument bewertet, `UiStyleConfig` **vererbbar** zu machen: Die Konfiguration eines Projekts benennt die Package-Konfiguration als Parent und speichert nur noch, was sie überschreibt; alles Übrige wird über den Parent aufgelöst. Es ist eine Planungsgrundlage, keine vollständige Spezifikation.

Ziel ist nicht, eine Aufgabe zu erledigen, sondern eine Problemklasse zu beseitigen: Heute ist die Style-Konfiguration eines Projekts eine **einmalige Vollkopie**. Styles, die der Library hinzugefügt werden, erreichen das Projekt deshalb nie, und die Kopie driftet unbemerkt vom Original weg.

---

## Problem

`clone_style_config` kopiert die Package-Konfiguration einmalig ins Projekt. Danach gibt es weder Merge noch Sync noch Diff. Folgen:

- Die Styles jeder neuen Library-Komponente müssen **zweimal** angelegt werden, einmal je Konfiguration.
- Eine Korrektur oder Ergänzung in der Library **kommt nie an**.
- Die Kopie läuft mit der Zeit auseinander, und niemand kann sagen, wie weit.
- Im Review ist die Abweichung unsichtbar, weil eine echte Änderung zwischen den Kopien untergeht.

An den aktuellen Assets gemessen:

| | Package-Konfiguration | botw-client-Klon |
|---|---|---|
| Style-Instanzen | 1.776 | 2.100 |
| Dateigröße | 524 KB | 609 KB |

Rund **85 % der Projekt-Konfiguration sind also eine Kopie** der Package-Konfiguration; etwa 324 Style-Instanzen sind tatsächlich projekteigen.

Es gibt einen zweiten Konsumenten: `notr-game-client` nutzt das Package (derzeit ein Tag zurück), hat aber **noch keine** eigene Style-Konfiguration übernommen. Die Kosten des heutigen Zustands vervielfachen sich mit jedem Projekt, das das tut.

---

## Machbarkeit

Günstig, und zwar wegen der Art, wie ein Style aufgelöst wird. `UiAbstractApplyStyleBase` speichert **keine Objektreferenz** auf seinen Style, sondern nur Name und Key:

```csharp
[SerializeField][HideInInspector] private string m_name;
public abstract int Key { get; }            // Hash aus Komponententyp + Style-Name

public UiAbstractStyleBase FindStyle()
{
    var styleConfig = StyleConfig;
    UiSkin currentSkin = SkinIsFixed ? styleConfig.GetSkinByName(m_fixedSkinName)
                                     : styleConfig.CurrentSkin;
    return currentSkin.StyleByKey(Key);
}
```

Die Auflösung läuft über **eine einzige Engstelle**, `UiSkin.StyleByKey(int)`, die bei unbekanntem Key `null` liefert. Genau dort gehört das Fallback auf den Parent hin. Für die Änderung der Auflösung müssen **keine serialisierten Daten migriert** werden.

Zwei Randbedingungen prägen den Entwurf:

- **Styles sind `[SerializeReference]`-Objekte inline im ScriptableObject**, keine Sub-Assets. Eine Child-Konfiguration kann einzelne Parent-Styles deshalb nicht „referenzieren", sondern den Parent nur bitten, einen Key aufzulösen.
- **`UiSkin` trägt eine Rück-Referenz `m_config`**, und `CurrentSkin` arbeitet über den Index. Das Fallback muss Skins deshalb **über den Namen** zuordnen, nicht über den Index — zwei Konfigurationen führen ihre Skins nicht garantiert in derselben Reihenfolge.

---

## Zwei Ausbaustufen

### Stufe A — Fallback auf Style-Ebene (empfohlen)

Die Child-Konfiguration speichert nur die Styles, die sie überschreibt. Findet die Auflösung nichts, geht die Anfrage an den gleichnamigen Skin des Parents.

- Keine Änderung an bestehender Semantik.
- Vollständig rückwärtskompatibel: Eine Konfiguration ohne Parent verhält sich exakt wie heute.
- Löst das beschriebene Problem vollständig — ein neuer Library-Style ist in jedem konsumierenden Projekt sofort vorhanden.

### Stufe B — Vererbung auf Wert-Ebene (vorerst nicht empfohlen)

Jeder einzelne Wert könnte erben, statt nur an oder aus zu sein. Das ändert die Bedeutung von `IsApplicable`, dem zentralen Begriff des Styling-Systems: Aus zwei Zuständen würden drei. Betroffen wären die 32 generierten `UiStyleX`/`UiApplyStyleX`-Paare, die Drawer und jede bestehende Konfiguration.

Der Zusatznutzen gegenüber Stufe A ist real, aber schmal: Ein Projekt könnte eine einzelne Farbe überschreiben, ohne den ganzen Style zu kopieren. Erneut zu bewerten, **nachdem** Stufe A eine Weile produktiv gelaufen ist.

---

## Phasen

### Phase 1 — Auflösung

- `[SerializeField] UiStyleConfig m_parent` in `UiStyleConfig` ergänzen.
- Fehlschlag der Auflösung über den Parent bedienen, Skins **über den Namen** zuordnen.
- Zyklen ausschließen, Kettentiefe begrenzen.
- Gilt ebenso für `UiAspectRatioDependentStyleConfig`, da von derselben Basis abgeleitet.

### Phase 2 — Copy-on-Write

Ein Schreibzugriff auf einen geerbten Style muss diesen zuerst in der Child-Konfiguration materialisieren und dann schreiben. Ohne das verschwindet der Schreibvorgang stillschweigend — siehe Risiken.

Betroffene Pfade: `UiAbstractApplyStyleBase.Record()`, die Wertänderungen im Skin-Drawer und `UiStyleWriter` (der Package-eigene Konfigurationen bereits ablehnt und nur den neuen Pfad lernen muss).

### Phase 3 — Editor

- Geerbte Styles schreibgeschützt und optisch unterscheidbar von eigenen.
- **Override** auf einem geerbten Eintrag (in die Child-Konfiguration materialisieren), **Zurück auf geerbt** auf einem eigenen (aus der Child-Konfiguration entfernen).
- Parent-Feld im `UiStyleConfigEditor` und im Konfigurationsfenster sichtbar machen.

### Phase 4 — Conversion-Tool

Siehe unten.

### Phase 5 — Verifikation und Dokumentation

- Dev-App-Szene, die alles durchspielt: geerbter Style, überschriebener Style, projekteigener Style, Skin nur im Parent, Skin nur im Child.
- `BEST-PRACTICES.md` §2 neu fassen: Klonen ist nicht mehr der empfohlene Weg, Erben ist es.
- `CHANGELOG.md`, und ein Hinweis in der KI-Dokumentation, dass ein Style jetzt im Parent liegen kann.

---

## Conversion-Tool

Das Werkzeug, das die Umstellung eines bestehenden Klons ungefährlich macht.

- **Diff** Child gegen Parent, Style für Style und Wert für Wert.
- **Bericht zuerst**: was ist identisch (kann geerbt werden), was weicht ab (bleibt Override), was existiert nur im Child (bleibt ohnehin).
- **Dry-Run als Standard**, Anwenden erst auf Zuruf.
- **Nicht alles oder nichts**: Ein identischer Style kann bewusst als Override **angeheftet** werden, wo ein Nachführen unerwünscht ist.

Der Bericht ist schon für sich nützlich, vor jeder Umstellung: Er beantwortet eine Frage, die heute niemand beantworten kann — **wie weit ist dieser Klon eigentlich abgedriftet?**

---

## Zu klärende Entscheidungen (vor Beginn von Phase 1)

1. **Erbt ein Child-Skin nur Styles aus dem gleichnamigen Parent-Skin, oder kann ein Skin als Ganzes geerbt werden?** Empfehlung: Zuordnung nur über den Skin-Namen; definiert das Child keinen Skin dieses Namens, fällt es vollständig auf den Skin des Parents zurück.
2. **Kettentiefe.** Eine Ebene (Projekt → Package) deckt jeden bekannten Fall ab. Längere Ketten kosten in der Auflösung nichts, vergrößern aber die Fehlerfläche.
3. **Umstellungspolitik für den bestehenden Klon**: alles Identische auf geerbt umstellen, oder ausgewählte Bereiche anheften? Das ist eine Gestaltungs-, keine technische Entscheidung.
4. **Die ungeklärte Skin-Identität.** In `UiStyleConfig.OnSetSkinAlias` steht der Kommentar `//FIXME: The _skin instance is different than the skins in style config - why??!`. Das sollte verstanden sein, **bevor** eine zweite Konfiguration an der Auflösung teilnimmt — sonst debuggt man zwei Unbekannte gleichzeitig.

---

## Aufwandsschätzung (ein Entwickler)

| Posten | PD |
|---|---|
| Phase 1 — Auflösung, namensbasierte Skin-Zuordnung, Zyklusschutz | 0,5 |
| Phase 2 — Copy-on-Write auf allen Schreibpfaden | 0,5 |
| Phase 3 — Editor: geerbt vs. eigen, Override / Zurücksetzen | 1,0 |
| Phase 4 — Conversion-Tool mit Bericht und Dry-Run | 0,8 |
| Phase 5 — Tests, Verifikation in der Dev-App, Dokumentation | 0,7 |
| **Summe (Stufe A)** | **~3,5** |

Stufe B käme mit geschätzt **3 bis 5 PD** obendrauf und ist nicht Teil dieses Plans.

---

## Risiken

**Library-Updates können das Aussehen eines Projekts verändern.** Heute ist ein Klon eingefroren; ein Restyling in der Library kann das Projekt nicht erreichen. Mit Vererbung kann es das — genau das ist der Sinn, aber es muss eine bewusste Entscheidung sein und keine Überraschung nach einem Paket-Update. Milderung: Overrides bleiben angeheftet, nur nicht gesetzte Styles folgen dem Parent, und das Conversion-Tool zeigt vorher genau, welche Styles geerbt würden.

**Stiller Verlust von Schreibvorgängen.** `SkipSavingInPackageFolder` verwirft jeden Speichervorgang, dessen Pfad mit `Packages` beginnt — ohne Fehlermeldung. Ein Schreibzugriff auf einen geerbten Style, der nicht vorher materialisiert wurde, *scheint* deshalb zu funktionieren und ist beim nächsten Laden verschwunden. Diese Falle existiert schon heute, ist aber selten; mit Vererbung würde sie zum Normalfall. Phase 2 dient allein dazu, sie zu beseitigen.

**Zwei Unbekannte gleichzeitig.** Siehe zu klärende Entscheidung 4.

**Geringe Risiken.** Die Auflösung kostet einen zusätzlichen Dictionary-Fehlschlag, und die Applier cachen ihren aufgelösten Style. Merge-Konflikte werden *unwahrscheinlicher*, weil eine umgestellte Child-Konfiguration nur noch einen Bruchteil ihrer heutigen Größe hat.
