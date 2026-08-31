# Outer Glow für UiShapeImage

## Overview

Dieses Dokument beschreibt, wie ein **Outer Glow** in `UiShapeImage` und seinen Subklassen als Mesh-Geometrie
erzeugt werden kann, statt ihn je Widget als Bitmap zu bauen. Es ist eine Planungsgrundlage, keine vollständige
Spezifikation.

Der Anlass ist praktisch: Ein Glow wird ständig gebraucht und heute mit Sprites gelöst (so gemacht bei den
BOTW-Chips). Ein Sprite-Glow hat eine feste Auflösung, lässt sich nicht verzerrungsfrei skalieren, nicht zur
Laufzeit tinten und nicht stylen. Ein erzeugter Glow kann alle vier Dinge.

**Was es nicht ist:** ein Performance-Gewinn. Die Overdraw-Fläche eines Ringstapels ist etwa so groß wie die
einer Bitmap mit gleichem Fußabdruck, und der Ringstapel kostet mehr Vertices. Der Gewinn liegt im Authoring,
nicht in der Framezeit.

---

## Problem

`UiShapeImage` blendet Kanten schon aus, aber nur **nach innen**: Jedes Shape legt seine Außenkante exakt auf
den Rect-Rand und schnitzt das Fade-Band nach innen (siehe `UiCircle.GenerateFilled`,
`UiCircle.GenerateFrame`). `Inner` / `Outer` im `Fade`-Enum meinen die beiden Seiten eines *Frame*-Sandwiches,
nicht die Außenseite des Rects.

Es gibt also überhaupt keine Geometrie außerhalb des Shapes — und genau die braucht ein Outer Glow.

![Fade nach innen gegenüber Glow-Ringen nach außen](Outer-Glow-Figures/fig-1-inward-vs-outward.svg)

---

## Was schon da ist

Ein großer Teil der Maschinerie liegt bereits vor und muss nur andersherum gerichtet werden.

| Baustein | Ort | Wiederverwendung |
|---|---|---|
| Ring-zu-Ring-Streifen-Emitter mit Vertexfarben je Kante | `UiShapeImage.EmitFrameStripFromPerimeters` | unverändert |
| Perimeter-Ping-Pong-Puffer („current outer" / „next inner") | `UiShapeImage.s_perimA` / `s_perimB` | unverändert |
| Polygon-Offset entlang der Normalen, Bisektor, Miter-Limit | `UiStar.BuildInsetMiter` | Vorzeichen |
| Uniform-Scale-Offset für konvexe reguläre Polygone | `UiStar.BuildInsetUniformScale` | unverändert |
| Zielfarbe des Ausblendens und eine Größe bis 200 | `m_fadeColor`, `m_fadeSize` | Muster |
| Corner-Fan- und Edge-Quad-Emitter des Rounded Rect | `UiRoundedImage.AddCorner`, `AddEdgeQuad`, `AddQuad` | Muster |

Im Tooltip von `m_fadeSize` steht schon *„can also be used for other purposes (e.g. soft shadow)"* — die
Absicht war da, die Richtung nicht.

---

## Geometrie

### Der Rounded Rect braucht keine Normalen-Extrusion

Der exakte Offset eines Rounded Rect mit Eckradius `r` um die Strecke `d` nach außen ist ein Rounded Rect mit
`rect.Inflate(d)` und Radius `r + d`. Gleiche Kreismittelpunkte, größere Radien. Keine Bisektoren, kein
Miter-Limit, keine Selbstüberschneidungsprüfung — und es degeneriert richtig: Bei `r = 0` bekommt der
Offset-Ring den Radius `d`, ein Shape mit scharfen Ecken also einen runden Glow an der Ecke. Genau so soll ein
Glow aussehen. Ein Miter hinterlässt dort stattdessen eine quadratische Ecke, die d·√2 in die Diagonale reicht.
(An einem spitzen Vertex — einer Sternzacke — erzeugt derselbe Miter tatsächlich eine Spitze; dafür gibt es das
Miter-Limit.)

![Ecken-Offset: analytisch radius + d gegenüber Miter](Outer-Glow-Figures/fig-2-corner-offset.svg)

Für `UiStar` und jedes künftige Shape mit freiem Perimeter bleibt die Normalen-Extrusion das richtige
Werkzeug — und dort ist sie schon geschrieben, siehe Phase 5.

### Ein Ring ist ein Frame-Streifen

Ein Ring ist strukturell dasselbe, was `GenerateFrame` heute schon emittiert: vier Seiten-Quads und vier
Corner-Fans. Nur die Radien unterscheiden sich.

![Triangulierung eines Glow-Rings](Outer-Glow-Figures/fig-3-ring-mesh.svg)

Pro Ring: `2 * (4*seg + 4)` Vertices und `4 * (4*seg + 4)` Dreiecke. Bei `cornerSegments = 5` und drei Ringen
sind das etwa 145 Dreiecke — neben den Füllkosten belanglos.

### Der Falloff-Exponent entscheidet, die Ringzahl kaum

Vertexfarben interpolieren über jedes Band linear, der Alpha-Verlauf ist also stückweise linear und seine
Steigung springt an jeder Ringgrenze. Die Erwartung war, dass sich das als Mach-Bänder zeigt. Mit der echten
Interpolation je Band gerendert tut es das nicht: Ein Glow mit einem Ring ist von einem mit sechs Ringen
visuell nicht zu unterscheiden — auch bei voller Spitzen-Alpha und bis hinunter zu einem schmalen 14-px-Glow,
wo der Gradient pro Pixel am steilsten ist.

Was jeder weitere Ring bringt, ist ein zusätzlicher Stützpunkt auf der Zielkurve, und drei Stützpunkte liegen
schon so nah an ihr, dass das Auge sie nicht mehr davon trennen kann. Der Regler, der das Aussehen ändert, ist
der **Falloff-Exponent** — mit einem Ring ist das Profil immer linear, egal was der Exponent sagt, und das ist
das einzige echte Argument, über einen Ring hinauszugehen.

![Ringzahl und Falloff-Exponent im Vergleich](Outer-Glow-Figures/fig-4-falloff.svg)

Folge für den Entwurf: `m_glowRings` auf 3 vorbelegen, die Obergrenze niedrig halten und die Aufmerksamkeit
stattdessen in den Falloff stecken.

---

## Phasen

### Phase 1 — Ringe nach außen für `UiRoundedImage`

`N` Ringe außerhalb des Shapes emittieren, Ring `i` aus `rect.Inflate(d*(i+1))` mit Radius `radius + d*(i+1)`,
Alpha aus einem Falloff über den normierten Abstand. Neue serialisierte Felder in `UiShapeImage`:
`m_glowSize`, `m_glowColor`, `m_glowRings` (Vorbelegung 3), `m_glowFalloff`. Die Gap-Spans (`m_spanLeft`
usw.) müssen je Ring aufgelöst oder bewusst ignoriert werden — ein Glow um einen unterbrochenen
Frame ist heute undefiniert.

### Phase 2 — Die `UiGradientBase`-Bounds-Regression beheben

`UiGradientBase.ModifyMesh` nimmt über `UiMeshModifierUtility.GetBounds` die Bounds des **gesamten** Meshes
und lerpt jeden Vertex. Glow-Ringe vergrößern diese Bounds, der Gradient auf dem Shape wird also in die Mitte
seines Wertebereichs gestaucht *und* die Glow-Ringe werden mitgefärbt. Das ändert das Aussehen jedes
bestehenden Prefabs, das `UiRoundedImage` mit `UiGradientSimple` kombiniert, und muss deshalb gelöst sein,
bevor das Feature angeschaltet wird.

![UiGradientBase-Bounds-Regression](Outer-Glow-Figures/fig-6-gradient-bounds.svg)

Möglichkeiten: die Bounds nur aus den Shape-Vertices bestimmen, oder Glow-Vertices markieren und vom Modifier
überspringen lassen. Das Zweite ist allgemeiner (es schützt auch künftige Geometrie außerhalb des Rects) und
braucht einen Marker-Kanal — ein UV2-Flag oder eine Vertex-Bereichsgrenze, die dem Modifier mitgegeben wird.

### Phase 3 — Der Übergang zwischen AA-Fade und Glow

Mit aktivem Fade nach innen erreicht Alpha exakt am Rect-Rand die 0, und dort beginnt der Glow mit seinem
Maximum. Das ist eine Unstetigkeit und liest sich als dünner transparenter Ring um das Shape — am schlimmsten
genau bei zurückhaltenden Glows, also im Normalfall.

![Alpha-Querschnitt: die Naht](Outer-Glow-Figures/fig-5-seam.svg)

Empfehlung: Bei aktivem Glow das äußere Fade-Band unterdrücken und Ring 0 mit Alpha 1 in der **Shape**-Farbe
beginnen lassen, über seine Breite hinweg zur Glow-Farbe überblendend. Dann liefert der Glow das
Antialiasing, und das Profil bleibt monoton.

### Phase 4 — Anbindung an das Style-System

Type-Json der geänderten Komponenten neu erzeugen und im Style-Generator *Write JSON* drücken, sonst wird die
kuratierte Property-Auswahl komplett verworfen. Den Property-Satz für v1 auf `Color`, `float` und `int`
beschränken — von diesen Typen ist bekannt, dass sie durch Style-Config, Style-Vergleich und Tweening
sauber durchlaufen.

### Phase 5 — Shapes mit Perimeter

`UiCircle` braucht einen einzigen radialen Ringstapel nach außen, das ist trivial. `UiStar` kann
`BuildInsetMiter` mit negativem Inset weiterverwenden, aber der Vorzeichenwechsel macht aus den Kerben des
Sterns konkave Vertices — und dort überschneidet sich ein Miter nach außen selbst. Das bestehende Miter-Limit
begrenzt die Länge, verhindert die Überschneidung aber nicht; ein großer Glow an einem spitzen Stern klappt
also in sich zusammen. Entweder je Vertex gegen die Längen der Nachbarkanten klemmen, oder den Glow bei
Sternen auf die Uniform-Scale-Strategie beschränken.

### Phase 6 — Inner Glow, Dokumentation, A/B-Nachweis

Der Inner Glow ist derselbe Ringstapel nach innen statt nach außen und fällt fast gratis mit ab — er
*überlappt* aber die Füllung und muss deshalb nach ihr emittiert werden. Danach wandert der nutzerseitige
Absatz nach `BEST-PRACTICES.md`, dieses Dokument wird gelöscht, und das Ergebnis wird direkt gegen die
bestehenden Bitmap-Glows der Chips gestellt, um zu klären, ob es sie wirklich ersetzt.

---

## Zu entscheiden, bevor Phase 1 beginnt

1. **Blend-Modus.** Mit Standard-UI-Blending funktioniert ein heller Glow auf dunklem Grund und verschwindet
   auf hellem fast. Ein echter Glow will meist additiv oder Screen. In v1 nur Normal und die Material-Variante
   später, oder gleich mitlösen (ein zweites Material heißt ein zweiter Draw Call, sofern es nicht batcht)?
2. **Parametrisierung des Falloffs.** Das ist der eigentliche visuelle Regler und verdient deshalb die
   bessere Antwort: ein float-Exponent, oder ein `Gradient` / eine `AnimationCurve`? Eine Kurve ist schöner zu
   autorieren, aber Unitys `Gradient` hat keine brauchbare Wertsemantik für den Style-Vergleich — das ist eine
   eigene Baustelle. Ein float-Exponent plus Spitzen-Alpha deckt alles ab, was die Figur zeigt.
3. **Innerhalb oder außerhalb des Rects.** Außerhalb ist der natürliche Glow, aber `RectMask2D`, `Mask` und
   ScrollRect-Viewports schneiden ihn ab — ein leuchtendes Listenelement wird an der Viewport-Kante gekappt.
   Soll es einen optionalen Modus geben, der stattdessen das Shape einrückt, damit der Glow im RectTransform
   bleibt?
4. **Farbe von Ring 0.** Von der Shape-Farbe zur Glow-Farbe überblenden (Empfehlung aus Phase 3), oder durchweg
   Glow-Farbe und das AA-Fade unverändert lassen?
5. **Wovon zählt `GlowSize`** — von der Außenkante des Shapes oder vom Rect-Rand? Beides fällt auseinander,
   sobald `m_padding`, `m_useFixedSize` oder `m_sizeOffset` im Spiel sind.
6. **Soll `UiShapeFill` den Glow beschneiden?** Es arbeitet auf dem fertigen Mesh, würde ihn heute also
   mitnehmen. Wahrscheinlich richtig, braucht aber eine Entscheidung und einen Test.
7. **Glow oder Schatten.** Ein versetzter Glow ist ein Drop Shadow. `m_glowOffset` schon in v1, oder zentriert
   bleiben und später darauf zurückkommen?

---

## Aufwandsschätzung (ein Entwickler)

| Phase | Schätzung |
|---|---|
| Phase 1 — Ringe nach außen für den Rounded Rect | 1 PT |
| Phase 2 — `UiGradientBase`-Bounds-Fix | 0,5 PT |
| Phase 3 — Übergang / Naht | 0,5 PT |
| Phase 4 — Style-Anbindung | 0,5 PT |
| Phase 5 — Circle und Star | 1 PT |
| Phase 6 — Inner Glow, Doku, A/B | 0,5 PT |
| **Summe** | **~4 PT** |

Die größte Streuung liegt in Phase 5: Die Klemmung konkaver Vertices beim Stern ist das einzige wirklich neue
Geometrieproblem im ganzen Feature.

---

## Risiken

| Risiko | Schwere | Gegenmaßnahme |
|---|---|---|
| Geänderte `UiGradientBase`-Bounds verändern bestehende Prefabs | hoch | Phase 2 vor der Nutzbarkeit; Screenshot-A/B über die betroffenen Prefabs |
| Naht zwischen Fade und Glow bei niedrigem Glow-Alpha | mittel | Phase 3; äußeres Fade bei aktivem Glow unterdrücken |
| Glow wird von `RectMask2D` / ScrollRect beschnitten | mittel | dokumentieren; den Inset-Modus aus Entscheidung 3 erwägen |
| Miter nach außen überschneidet sich an konkaven Vertices (Stern) | mittel | Klemmung je Vertex, oder bei Sternen nur Uniform Scale |
| Overdraw auf Mobile bei großen Glows | gering | gleiche Ordnung wie die ersetzte Bitmap; `m_glowSize` begrenzen und im Inspector warnen |
| Wachstum der Style-Config um vier Properties | gering | mechanisch; v1 auf Color/float/int beschränken |
