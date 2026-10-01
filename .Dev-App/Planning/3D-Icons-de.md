# 3D-Icons

## Überblick

Dieses Dokument plant **dynamische 3D-Icons**: UI-Elemente, die ein 3D-Objekt zeigen, das eine Kamera in eine
Render Texture rendert – jedes mit eigener Lichtstimmung, unbeeinflusst voneinander und von der Szene. Es ist
eine Planungsgrundlage für die Umsetzung im Branch `3d-icons`, keine vollständige Spezifikation.

Eine frühere Implementierung dieses Features (außerhalb dieses Packages) hat mehrere Wochen gedauert. Fast nichts
davon ging in den offensichtlichen Teil – Kamera, Render Texture, RawImage. Die Zeit ging in die **Isolation**
(irgendetwas aus der Szene oder einem anderen Icon schlägt immer durch), in **Randfälle des Lebenszyklus**
(Edit Mode, Pooling, asynchrones Laden, verlorene Texturen) und in die **Bildqualität** (Alpha-Säume,
Bildausschnitt). Der Plan ist so geordnet, dass diese Punkte früh gelöst und *nachgewiesen* werden, bevor
Komfortfunktionen darauf aufbauen.

Der Autor der früheren Implementierung bestätigt diese Reihenfolge der Schwierigkeiten: Das Icon selbst war
einfach; die Arbeit war ein Kampf gegen Unitys globales Environment-Handling. „Könnte es eine Szene geben ohne
Beeinflussung durch das globale Environment, wäre das alles trivial gewesen.“ Phase 1 beginnt deshalb damit
herauszufinden, wie nah `Camera.scene` genau daran herankommt (siehe *Spike: Szenen-Isolation*).

**Anforderungen**

| # | Anforderung | Konsequenz |
|---|---|---|
| R1 | Zuerst Built-in-Pipeline; URP und HDRP müssen später möglich sein | Pipeline-spezifischer Code von Anfang an hinter einem Interface |
| R2 | Die meisten Icons werden einmal gerendert und bleiben dann eine statische Textur; auf Wunsch rendern sie periodisch neu | Statisch ist der Standard und der billige Pfad; periodisch ist statisch mit Timer |
| R3 | Es gibt einen voll animierten Modus; die Animation bringt das Objekt selbst mit | Das Icon ist nur für die Bühne zuständig, nie für die Animation |
| R4 | Bis zu ca. 100 Icons gleichzeitig, davon höchstens eines animiert | Render-Budget pro Frame, geteilte Ergebnisse, Speicherdisziplin |
| R5 | Lichtstimmungen sind Presets, angelegt als Prefabs | Lichter sind echte `Light`-Komponenten, die ein Designer verschieben kann |
| R6 | Icons sind Prefabs | `UiIcon3D.prefab` in der Library, Client-Varianten je Einsatzzweck |
| R7 | Objekte können dynamisch nachgeladen werden | Laden über `AssetManager` / `CanonicalAssetKey` |
| R8 | Jedes Objekt hat eigenes Licht, isoliert von anderen Icons und der Szene | Isolation über die Zeit statt über den Raum (siehe unten) |
| R9 | Objekte dürfen eigene Lichter mitbringen (z. B. ein leuchtender Kristall) | Objektlichter werden wie Preset-Lichter behandelt: auf die Bühne beschränkt, nur während ihres eigenen Render-Aufrufs aktiv |
| R10 | Bildausschnitt = in das Rect einpassen | Kein einheitlicher Weltmaßstab über Icons hinweg nötig |

**Was das nicht ist:** ein allgemeines „3D in die UI rendern“-System für Zwischensequenzen oder
Charakterbildschirme mit Post-Processing. Eine Bühne, ein Objekt, ein Preset pro Render-Aufruf.

---

## Was schon existiert

| Baustein | Ort | Rolle in diesem Feature |
|---|---|---|
| Render Textures per Keyword verknüpft, bei Bedarf erzeugt | `RenderTextureManager`, `UiRenderTextureProducer`, `UiRawImageRenderTextureConsumer` (`Runtime/Code/Rendering`) | Speicher jedes Icon-Ergebnisses; Teilen identischer Icons über das Keyword |
| Overrides globaler Einstellungen pro Kamera, ohne Reflection, ohne Boxing | `PerCameraSetting<T>`, `UiAbstractPerCameraSettings`, `UiCameraRenderSettings` | Ambient, Fog, Reflection, Schatten pro Preset |
| Abstraktion fürs Laden (Addressables, Resources, direkt) | `AssetManager`, `IAssetProvider`, `CanonicalAssetKey` | Dynamisches Nachladen der Icon-Objekte |
| Pooling | `UiPool`, `IPoolable` | Icons in gepoolten Listeneinträgen |
| Takt im Edit Mode | `EditorUpdater`, `EditorApplication.update` | Vorschau im Edit Mode (nie `delayCall`, siehe CLAUDE.md) |
| UI-Basisklasse | `UiThing` | Basis von `UiIcon3D` |

Beide Rendering-Bausteine sind durch EditMode-Tests abgedeckt (`TestRenderTextureManager`, `TestPerCameraSetting`).

---

## Spike: Szenen-Isolation (Ergebnis)

Unity hält `RenderSettings` (Ambient, Fog, Skybox, Reflection) **pro Szene**, verwendet aber nur die der *aktiven*
Szene. `Camera.scene` beschränkt eine Kamera auf den Inhalt einer Szene und existiert zur Laufzeit. Die Frage war,
ob eine eigene „Icon-Szene“ Isolation geschenkt liefert. `Tests/PlayMode/SpikeIcon3DSceneIsolation.cs` hat es
gemessen (Built-in, Unity 6000.0):

| # | Frage | Ergebnis |
|---|---|---|
| Q1 | Sind Objekte anderer Szenen für eine Kamera mit gesetztem `Camera.scene` unsichtbar? | **Nein** – `Camera.scene` bewirkt bei einer zur Laufzeit erzeugten Szene nichts |
| Q2 | Bleibt das Icon von Lichtern anderer Szenen unbeleuchtet? | **Nein** |
| Q3 | Bleibt die Hauptszene von Lichtern der Icon-Szene unbeleuchtet? | **Nein** |
| Q4/Q5 | Bekommt das Icon das Environment seiner eigenen Szene? | **Nein** – immer das der aktiven Szene |
| Q6 | Schaltet ein Wechsel der aktiven Szene um den Render-Aufruf herum das Environment um? | **Ja**, sofort, im selben Frame, und vollständig |
| Q7 | Verändern Overrides pro Kamera (`UiCameraRenderSettings`) das Bild? | **Ja** für Ambient und Fog |
| Q8 | Ist das Bild mit Overrides identisch mit dem Bild per Szenenwechsel? | Erster Lauf **nein**: Das Orakel hat die **Environment-Reflection** der Szene (Skybox) gefunden, ~0,08 pro Kanal selbst bei Smoothness 0 (Q8a hält das als Befund fest). Mit zusätzlich überschriebener Reflection: **identisch**, maximale Abweichung 0,0000 (Q8b) |
| Q9 | *Setzt* der Override eine Preset-Reflection, statt sie nur abzuschalten? | **Ja** – gelbe Cubemap sichtbar und identisch mit dem Orakel |

**Entscheidung**

- Objekte und Lichter: Eine eigene Szene bringt nichts. Reservierter Layer + Ausmaskieren fremder Lichter bleiben
  wie geplant.
- Environment: Der Wechsel der aktiven Szene wäre die vollständige Lösung, löst aber pro Render-Aufruf zweimal
  `SceneManager.activeSceneChanged` aus – beim animierten Icon zweimal pro Frame, dauerhaft. Spielcode, der auf
  dieses Event hört, geht kaputt; das Toolkit selbst hat so einen Listener (`UiStartupOverlayView` stoppt darauf
  seinen Ablauf). **Im Produktivcode nicht verwendet.**
- Stattdessen wird der Szenenwechsel zum **Test-Orakel**: Er tauscht das komplette Environment, auch Eingänge, an
  die niemand gedacht hat. Jeder Isolationstest rendert einmal mit den Overrides und einmal mit Szenenwechsel; die
  Bilder müssen übereinstimmen (Q8). Eine Abweichung ist ein fehlender Override – gefunden an dem Tag, an dem er
  auftaucht, nicht Wochen später. Damit wird der offene „Kampf gegen das Environment“ zu einer Checkliste, die
  selbst sagt, wann sie vollständig ist.
- Die Testsuite behält Q1–Q6 als Zusicherungen des gefundenen Verhaltens, damit ein Unity-Update, das es ändert,
  auffällt.

---

## Grundidee: Isolation über die Zeit

Jedes Icon auf einen eigenen Layer zu legen scheitert an 32 Layern; alle Icons auf einen Layer zu legen lässt ihre
Lichter aufeinander scheinen. Stattdessen gibt es **eine Bühne** auf **einem reservierten Layer**, und die Icons
werden **nacheinander** gerendert. Während eines einzelnen Render-Aufrufs sind genau ein Objekt und ein Preset
aktiv; alles Globale wird für die Dauer dieses Aufrufs überschrieben und danach wiederhergestellt.

```
für jedes anstehende Icon (innerhalb des Frame-Budgets):
    Objekt-Instanz + Preset-Lichter aktivieren       (alles andere auf der Bühne ist inaktiv)
    Render-Settings des Presets anwenden             (Ambient, Fog aus, Reflection, Schatten)
    fremde Lichter aus dem Icon-Layer ausmaskieren   (Szenenlichter mit "Everything")
    Bühnenkamera rendern -> Scratch-Target (Tiefe, MSAA)
    Scratch auflösen/kopieren -> Speichertextur des Icons (ohne Tiefe, ohne MSAA)
    alles wiederherstellen, deaktivieren
```

Was durchschlagen kann, und was es verhindert:

| Leck | Verhindert durch |
|---|---|
| Szenenobjekte | Bühnenkamera sieht nur den Icon-Layer |
| Icon-Objekte in den Szenenkameras | Validator: keine andere Kamera darf den Icon-Layer sehen (auch nicht die von `UiMain`) |
| Szenenlichter | Culling Mask der Preset-Lichter = nur Icon-Layer; fremde Lichter während des Renderns ausmaskiert |
| Lichter anderer Icons | Nur ein Preset pro Render-Aufruf aktiv |
| Ambient Light, Fog, Skybox | Werte aus dem `UiCameraRenderSettings` des Presets, um den Render-Aufruf herum angewandt |
| **Light Probes der Szene** | `lightProbeUsage = Off` an jedem Renderer einer Icon-Instanz (sonst beleuchten die Probes der Szene an der Bühnenposition das Objekt) |
| Reflection Probes der Szene | `reflectionProbeUsage = Off` oder eine Probe im Preset; Bühne weit außerhalb des Levels |
| Schatten von Szenenobjekten | Schattenwerfer werden über die Layer-Maske der Kamera ausgeschlossen |
| Animiertes Icon in den Renders anderer Icons | Seine Renderer bekommen `forceRenderingOff` und seine eigenen Lichter werden abgeschaltet, während andere rendern |
| Lichter, die das Objekt mitbringt (R9) | Culling Mask auf die Bühne beschränkt; nur aktiv, während ihre Instanz rendert |

---

## Architektur

### Komponenten und Dienste

| Typ | Art | Verantwortung |
|---|---|---|
| `UiIcon3D` | `UiThing`, `IPoolable`, an einem `RawImage` | Was gezeigt wird (Objektquelle, Preset, Modus, Bildausschnitt). Fordert Renders an; rendert nie selbst. Konsument seines Render-Texture-Keywords. |
| `UiIcon3DPreset` | MonoBehaviour auf einem **Preset-Prefab** | Lichtstimmung: Kind-`Light`s, ein `UiCameraRenderSettings` (Ambient, Fog, Reflection, Schatten), Bildausschnitt (Blickrichtung, Projektion, FOV, Rand), Hintergrundfarbe. |
| `UiIcon3DBoundsHint` | MonoBehaviour auf einem **Objekt-Prefab** (optional) | Überschreibt Bounds / Pivot / bevorzugte Blickrichtung, wo die Automatik versagt (Skinned Meshes, Partikel, schräge Pivots). |
| `UiIcon3DRenderer` | statischer Dienst + versteckte Bühne | Besitzt Bühne und Kamera, die Auftragsschlange, das Frame-Budget, geladene Assets und Instanzen. Produzent für alle Icon-Keywords. |
| `Icon3DRenderScope` | `IDisposable`-Struct | Alles, was für einen Render-Aufruf gesetzt und danach wiederhergestellt wird. |
| `Icon3DFitter` | statisch | Bounds -> Kameraabstand / Ortho-Größe / Near / Far. |
| `IIcon3DRenderBackend` | Interface | Pipeline-Spezifisches: Render-Aufruf, Kamera-Setup, Umgebung. Jetzt `BuiltinIcon3DBackend`; URP/HDRP später. |
| `UI/Icon3D` | Shader | UI-Shader mit Premultiplied Alpha, mit Stencil-Masking und `RectMask2D`-Clipping wie `UI/Default`. |

### Statisch, periodisch, animiert

- **Statisch** (Standard): laden -> auf der Bühne instanziieren -> vorbereiten -> einpassen -> einmal rendern ->
  Instanz zerstören. Das geladene Asset bleibt referenziert, damit ein erneutes Rendern (Resize, Preset-Wechsel)
  nur ein billiges Instanziieren kostet.
- **Periodisch**: statisch, alle n Sekunden neu gerendert (z. B. ein Preset, das der Tageszeit folgt, oder ein
  Shader, der sich langsam ändert). Die Instanz bleibt zwischen den Render-Aufrufen erhalten, aber inaktiv, und
  kostet so keine Update-Zeit. Identische periodische Icons teilen sich weiterhin eine Textur und einen Render-Aufruf.
- **Animiert**: Die Instanz bleibt bestehen und aktiv, damit `Animator`, `ParticleSystem`s und Skripte weiterlaufen;
  sie wird jeden Frame gerendert (optional jeden n-ten), als Letztes im Frame, nach dem statischen Stapel.
  Der Bildausschnitt wird einmal berechnet, nicht pro Frame – sonst pumpt das Bild mit der Animation.

### Geteilte Ergebnisse: Das Keyword ist ein Inhalts-Hash

Listen zeigen oft dasselbe Item mehrfach. Das Render-Texture-Keyword eines statischen Icons wird aus allem
abgeleitet, was das Bild bestimmt: `Objekt-Key + Preset + Pixelgröße + Blickrichtung + Fit-Einstellungen`.
Identische Icons bekommen damit **dasselbe Keyword**, der `RenderTextureManager` gibt ihnen **dieselbe Textur**,
und das Objekt wird **einmal** gerendert. Das ist der Hauptgrund, warum der Keyword-Manager zuerst gebaut wurde.
Animierte Icons bekommen immer ein eindeutiges Keyword.

### Speicherdisziplin

Jedes Icon in einer Render Texture mit Tiefenpuffer und MSAA abzulegen verschwendet viel Speicher
(100 x 256² x (4 B Farbe + 4 B Tiefe) x 4 MSAA ≈ 200 MB). Deshalb:

- **Ein gemeinsames Scratch-Target** mit Tiefe und MSAA, per `RenderTexture.GetTemporary` in der Größe des
  größten anstehenden Auftrags, für das eigentliche Rendern.
- **Speichertexturen** (die, die der `RenderTextureManager` für Icons ausgibt): nur Farbe, keine Tiefe, kein MSAA.
  Das Scratch-Ergebnis wird aufgelöst und hineinkopiert.
- Später können die Speichertexturen zu Atlas-Seiten werden (siehe Phase 6), ohne den Rest anzufassen.

### Lichter an Objekten (R9)

Nichts spricht dagegen; sie brauchen nur dieselbe Behandlung wie Preset-Lichter:

- **Culling Mask** auf die Bühne beschränkt (Layer oder Szene, je nach Spike), automatisch an der Instanz gesetzt.
- **Nur während ihres eigenen Render-Aufrufs aktiv.** Für statische Instanzen ist das gegeben (sonst inaktiv). Die
  animierte Instanz ist dauerhaft aktiv, also werden ihre Lichter abgeschaltet, während andere Icons rendern –
  zusammen mit `forceRenderingOff`.
- **Pixel-Light-Budget**: Built-in Forward rendert nur `QualitySettings.pixelLightCount` Lichter pro Pixel, den Rest
  per Vertex/SH. Preset-Lichter plus Objektlichter müssen hineinpassen; das `UiCameraRenderSettings` des Presets
  hebt die Anzahl für den Icon-Render bei Bedarf an, und Preset-Lichter bekommen `LightRenderMode.ForcePixel`.
- Gebackene Lichter an Objekten bewirken zur Laufzeit nichts; der Validator warnt davor.

### Transparenz

Die Kamera löscht auf `(0,0,0,0)`. Kanten (MSAA, Texturfilterung) enthalten dann Farbe, die bereits mit Alpha
multipliziert ist; mit normalem Alpha-Blending gezeichnet ergibt das dunkle Säume. `UI/Icon3D` blendet deshalb mit
`One, OneMinusSrcAlpha`. Auch die Tint-Farbe muss im Vertex-Teil vormultipliziert werden.

Transparente Materialien *am Objekt* (Glas, Partikel) schreiben mit Standard-Blending ein falsches Alpha
(`SrcAlpha, OneMinusSrcAlpha` gilt auch für den Alpha-Kanal). Phase 1 dokumentiert das; Phase 6 bietet eine Lösung.

### Render-Settings an einem Preset ohne Kamera

`UiAbstractPerCameraSettings` verlangt derzeit eine `Camera`, weil es sich an deren Render-Callbacks hängt. Ein
Preset-Prefab darf keine Kamera enthalten (sie würde mitrendern). Änderung: `RequireComponent(Camera)` entfällt;
ohne Kamera registriert sich die Komponente nirgends und wird manuell über die vorhandenen öffentlichen
`Apply()` / `Restore()` angewandt. Der Renderer ruft diese um seinen Render-Aufruf herum auf.

### Pipeline-Abstraktion (R1)

```csharp
public interface IIcon3DRenderBackend
{
    void SetupCamera( Camera _camera );                      // z. B. URP: UniversalAdditionalCameraData, kein Post-Processing
    void Render( Camera _camera, RenderTexture _target );    // Built-in: Camera.Render(); URP: SubmitRenderRequest
    IDisposable ApplyEnvironment( UiIcon3DPreset _preset );  // Built-in: RenderSettings; HDRP: Volume auf einem Preset-Layer
}
```

Notizen für später, damit sich das Interface nicht ändern muss:

- **URP**: `RenderPipeline.SubmitRenderRequest` mit `UniversalRenderPipeline.SingleCameraRequest` (ab 2022.2).
  Alpha bleibt nur ohne Post-Processing erhalten. Limit für zusätzliche Lichter pro Objekt. Ambient kommt wie in
  Built-in aus `RenderSettings`.
- **HDRP**: Umgebung über ein `Volume` pro Preset und die `volumeLayerMask` der Kamera – die Isolation ist dort
  eingebaut. **Feste Belichtung ist Pflicht**, sonst regelt die Auto-Exposure jedes Icon auf Grau. Alpha braucht im
  HDRP-Asset ein Farbpufferformat mit Alpha. Lichter brauchen `HDAdditionalLightData`, Presets also HDRP-Varianten.
- Presets sind daher pipeline-spezifische Prefabs; ihre lichtunabhängigen Daten (Bildausschnitt, Hintergrund)
  sind gemeinsam.

### Konfiguration

- `UiToolkitConfiguration`: reservierter Icon-Layer, Standard-Preset, Frame-Budget, Scratch-MSAA,
  Standard-Auflösungsfaktor für die Speichertexturen.
- **Validator** (Editor + Warnung in Development Builds): eine andere Kamera sieht den Icon-Layer; ein Szenenlicht
  enthält den Icon-Layer (Hinweis, wird ohnehin ausmaskiert); der `Animator` eines Objekts nutzt im animierten
  Modus `cullingMode = CullCompletely`.

---

## Phasen

Jede Phase endet mit etwas Sichtbarem und Getestetem. Aufwände orientieren sich am Tempo der beiden Vorarbeiten.

### Phase 0 – Vorarbeit (erledigt)

- `RenderTextureManager` + Produzent/Konsument-Komponenten, Tests.
- `PerCameraSetting<T>` / `UiAbstractPerCameraSettings` / `UiCameraRenderSettings`, Tests inkl. echtem Render-Aufruf.

### Phase 1 – Isoliertes statisches Rendern (der Kern)

0. Spike zur Szenen-Isolation – erledigt, siehe oben: Ambient, Fog und Environment-Reflection, pro Kamera
   überschrieben, stimmen exakt mit dem Orakel überein. Als Nächstes: den Vergleich aus dem Spike zu einem
   wiederverwendbaren Orakel-Helfer machen, den alle Isolationstests nutzen, und den Befund-Test von „Spike“ auf
   einen dauerhaften Namen umziehen.
1. `UiAbstractPerCameraSettings` ohne Kamera nutzbar machen (siehe oben).
2. Layer-Reservierung in `UiToolkitConfiguration` + Validator.
3. Bühne: versteckter Root (`HideAndDontSave` im Edit Mode, `DontDestroyOnLoad` im Play Mode), weit entfernt; eine
   Kamera, deaktiviert, löscht transparent, Culling Mask = Icon-Layer.
4. `Icon3DRenderScope`: Preset-Lichter an, Render-Settings angewandt, fremde Lichter ausmaskiert, Probes an der
   Instanz aus, Wiederherstellung in umgekehrter Reihenfolge.
5. `BuiltinIcon3DBackend`, Scratch-Target, Auflösen in die Speichertextur.
6. `Icon3DFitter`: Bounds aus den Renderern (Kugel-Fit = drehstabil; Box-Fit = enger), Rand, Near/Far aus den Bounds.
7. Shader `UI/Icon3D` mit Premultiplied Alpha + Material.
8. Eine minimale API ohne Komponente: `UiIcon3DRenderer.RenderStatic(prefab, preset, size) -> Keyword`.

**Fertig, wenn** die Isolationstests unten grün sind und eine Dev-Szene zwei Icons mit gegensätzlichen Presets neben
einer Szene mit starkem farbigem Directional Light und Fog zeigt – ohne sichtbaren Einfluss in irgendeine Richtung.

**Stand:** umgesetzt (`Runtime/Code/Icon3D`, `Runtime/Shaders/UI_Icon3D.shader`, `Editor/Icon3D`), Tests in
`Tests/PlayMode/TestIcon3DRenderer.cs`. Abweichungen vom Plan oben, beim Bauen gefunden:

- **Kein Validator für Szenenkameras.** Statische Instanzen und Preset-Lichter existieren (sind aktiv) nur während
  ihres eigenen Render-Aufrufs; eine Szenenkamera kann sie also nie sehen, egal wie ihre Culling Mask aussieht. Der
  reservierte Layer hält nur Szenenobjekte von der Bühnenkamera fern. Die animierte Instanz (Phase 4) wird außerhalb
  ihres Renderings per `forceRenderingOff` unsichtbar gehalten. Übrig bleibt eine Prüfung auf Szenenobjekte im
  Icon-Layer (Menü + Konfigurationsfenster).
- **Neutraler Ausgangszustand.** Vor den Settings des Presets setzt das Backend ein vollständiges neutrales
  Environment (schwarzes Flat-Ambient, kein Fog, keine Skybox, schwarze Custom-Reflection) – das, was eine leere
  Szene liefert. Was ein Preset offen lässt, ist neutral, nie das der Szene. Der Orakel-Test prüft genau das.
- **Physik an Icon-Objekten bleibt vorerst unangetastet:** Die Runtime-Assembly referenziert die Physik-Module nicht,
  und eine statische Instanz ist nie über einen Physik-Schritt hinweg aktiv. Der animierte Modus braucht es
  (Collider einer dauerhaft aktiven Instanz); geplant über `versionDefines` auf die Physik-Modul-Pakete.

### Phase 2 – Komponente, Presets, Prefabs

1. `UiIcon3D`: Objektquelle (vorerst direkte Prefab-Referenz), Preset, Blickrichtung, Fit-Modus,
   Auflösungsfaktor, Modus (statisch / periodisch + Intervall / animiert); erneutes Rendern bei Größenänderung / Texturverlust / Property-Änderung; Inhalts-Hash als Keyword.
2. `UiIcon3DPreset` + `UiIcon3DBoundsHint`.
3. Library-Prefabs: `UiIcon3D.prefab`, Presets *Neutral*, *Warm*, *Dramatic* – **jedes mit eigener
   Reflection-Cubemap**: Die Basis-Reflection ist schwarz, und Metall reflektiert fast nur seine Umgebung; ein Preset
   ohne Reflection rendert Metall schwarz (gesehen in der Demo-Szene von Phase 1; `Icon3DEnvironmentUtility.CreateGradientCubemap`). Gemäß BEST-PRACTICES legen Clients
   Varianten in einem Rutsch an.
4. Vorschau im Edit Mode (Renderer arbeitet im Edit Mode, Takt über `EditorApplication.update`), auch in der Prefab Stage.

**Fertig, wenn** ein Icon platziert, konfiguriert und im Edit Mode betrachtet werden kann, ohne in den Play Mode zu
gehen, und Domain Reload sowie Szenen-Speichern übersteht, ohne Objekte zu verlieren oder die Szene dirty zu machen.

### Phase 3 – Dynamisches Laden und Skalierung

1. Objektquelle `CanonicalAssetKey` über `AssetManager`; ein Ladevorgang pro Key, referenzgezählt über alle Icons.
2. Platzhalter während des Ladens (Sprite oder nichts); Abbruch, wenn das Icon deaktiviert, gepoolt oder neu
   belegt wird, bevor das Laden fertig ist – das späte Ergebnis darf nie im falschen Icon landen.
3. `IPoolable`-Unterstützung.
4. Auftragsschlange: Frame-Budget (Anzahl und/oder Millisekunden), sichtbare Icons zuerst, Deduplizierung.
5. Texturverlust: `RenderTexture.IsCreated()` false (Device Reset, App-Resume auf Mobilgeräten) -> neu rendern.

**Fertig, wenn** eine Scroll-Liste mit 100 gepoolten Einträgen und 30 verschiedenen, per Addressables geladenen
Objekten ohne Ruckler scrollt, jedes unterschiedliche Icon genau einmal rendert und schnelles Scrollen (Neubelegung
während des Ladens) übersteht.

### Phase 4 – Animierter Modus

1. Dauerhafte Instanz, jeden Frame nach dem statischen Stapel gerendert; optionaler Frame-Teiler.
2. `forceRenderingOff`, während andere Icons rendern.
3. Umgang mit `Animator.cullingMode` (auf `AlwaysAnimate` erzwingen oder warnen), Partikelsysteme, Skripte, die nach
   `Camera.main` suchen (dokumentierte Falle).
4. Ein Icon zwischen statisch und animiert umschalten (z. B. nur das ausgewählte/gehoverte Item animieren).

**Fertig, wenn** ein animiertes Icon zwischen 99 statischen läuft, keines das andere beeinflusst und das Umschalten
des animierten auf statisch es ohne Aufblitzen im aktuellen Frame einfriert.

### Phase 5 – Werkzeuge und Dokumentation

1. Preset-Studio: Editor-Fenster, das ein Raster aus Beispielobjekten x Presets rendert, zum Erstellen von Presets.
2. Debug-Ansicht: alle Keywords des `RenderTextureManager`, Größen, Speicher, Nutzer; Render-Aufrufe pro Frame.
3. Dokumentationsseite + Eintrag in BEST-PRACTICES (Layer-Reservierung, Preset-Varianten, Pipeline-Varianten).

### Phase 6 – Optional

- **Atlas** für statische Icons: Speicherseiten statt einer Textur pro Icon; `RawImage.uvRect`; weniger Draw Calls.
- **Alpha-Korrektur** für transparente Objektmaterialien (eigener Alpha-Pass oder alpha-erhaltender Blend-Override).
- **Shadow Catcher** in Presets: unsichtbarer Boden, der nur Schatten empfängt und ins Alpha schreibt.
- **URP-Backend**, danach **HDRP-Backend**.

---

## Teststrategie

Rendering ist testbar: in eine Textur rendern, Pixel zurücklesen, Farben vergleichen. Jede Isolationsregel bekommt
einen Test dieser Art (Built-in). Wo möglich ist die Erwartung keine handverlesene Farbe, sondern das
**Orakel-Bild**: derselbe Render mit aktiver sauberer Szene (siehe Spike), bei dem das komplette Environment
getauscht ist.

| Test | Aufbau | Erwartung |
|---|---|---|
| Nur Preset-Licht | Weiße Kugel, Preset mit einem roten Licht | Mittelpixel rot, kein Blau |
| Zwei Presets | Dieselbe Kugel, erst rotes, dann blaues Preset | Erstes Ergebnis bleibt nach dem zweiten Render rot |
| Szenenlicht | Directional Light in der Szene, hellgrün, Culling Mask *Everything* | Icon ohne Grün |
| Szenen-Ambient / Fog | Ambient der Szene magenta, Fog an | Icon unbeeinflusst; Szenenwerte danach unverändert |
| Light Probes | Light Probe Group der Szene an der Bühnenposition hell gebacken | Icon unbeeinflusst |
| Transparenz | Kugel auf gelöschtem Hintergrund | Eckpixel Alpha 0, Mitte Alpha 1, Kantenfarbe <= Alpha (vormultipliziert) |
| Bildausschnitt | Objekte sehr unterschiedlicher Größe | Abdeckung der Textur innerhalb eines Toleranzbands |
| Objektlicht | Objekt-Prefab mit eigenem blauem Licht, Icon neben einem animierten Icon | Blau nur im eigenen Icon, nicht im animierten, nicht in der Szene |
| Periodisch | Periodisches Icon mit sich änderndem Preset-Wert | Textur ändert sich nach dem Intervall, nicht vorher |
| Geteiltes Ergebnis | Zwei Icons, gleicher Inhalt | Gleiches Keyword, ein Render-Aufruf |
| Async-Race | Objekt neu zuweisen, während das Laden läuft | Endbild zeigt das zweite Objekt |

Manuell bleiben die Sichtprüfungen: Dev-Szene mit Presets nebeneinander; Preset-Studio.

---

## Risiken

| Risiko | Warum es wahrscheinlich ist | Gegenmaßnahme |
|---|---|---|
| Ein neues Isolationsleck taucht spät auf | Built-in hat viele globale Eingänge (SH-Ambient, Probes, Reflection, Schatten, Fog-Keywords); bestätigt als Hauptaufwand der früheren Implementierung | Zuerst der Spike; Pixeltest pro Leck; jedes neue Leck bekommt zuerst einen Test |
| Ambient-Override in Built-in nicht pro Kamera sichtbar | Ambient wird als SH hochgeladen; wann genau Unity das liest, ist undokumentiert | Als Erstes in Phase 1 prüfen; Ausweg: `RenderSettings.ambientProbe` direkt setzen |
| Lebenszyklus im Edit Mode | Domain Reload, Prefab Stage, Szenen-Speichern, versteckte Objekte | Bühne mit `HideAndDontSave`, Freigabe bei `beforeAssemblyReload`, Tests im Edit Mode |
| Races zwischen Async und Pooling | Ladevorgänge enden, nachdem das Icon wiederverwendet wurde | Auftrags-Token; Ergebnis wird nur angewandt, wenn das Token noch passt |
| Verlust des Texturinhalts | Render Textures verlieren bei Device Reset ihren Inhalt | `IsCreated()`-Prüfung pro Frame für statische Icons |
| Animator-Culling | `BasedOnRenderers` hält eine manuell gerenderte Kamera eventuell für „nicht sichtbar“ | Erzwingen oder warnen (Phase 4) |
| Speicher bei 100 Icons | Tiefe + MSAA pro Textur | Scratch-Target + schlanke Speichertexturen, später Atlas |
| Unterschiede der SRPs | Alpha, Belichtung, Lichtlimits | Backend-Interface ab Phase 1; pipeline-spezifische Presets |

---

## Entscheidungen

| Frage | Entscheidung |
|---|---|
| Wo hat die frühere Implementierung ihre Zeit verloren? | Environment-Handling / Isolation, wie unter *Risiken* vermutet. Bekommt den ersten Platz (Spike) und einen Pixeltest pro Leck. |
| Dürfen Objekte eigene Lichter mitbringen? | Ja (R9). |
| Statische Icons periodisch neu rendern? | Ja, auf Wunsch (periodischer Modus). |
| Einheitlicher Maßstab über Icons hinweg? | Nein, in das Rect einpassen (R10). |
