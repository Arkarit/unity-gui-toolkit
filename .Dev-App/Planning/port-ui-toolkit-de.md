# Portierungsplan: unity-gui-toolkit → Unity UI Toolkit (UXML/USS)

## Überblick

Dieses Dokument beschreibt eine schrittweise Strategie zur Migration von `de.phoenixgrafik.ui-toolkit` von UGUI (UnityEngine.UI) zum Unity UI Toolkit (UXML/USS). Es dient als Planungsreferenz, nicht als vollständige Spezifikation.

---

## Umfangsabschätzung

### Was obsolet wird

Diese Subsysteme haben kein sinnvolles Äquivalent im UI Toolkit und sollten gestrichen oder von Grund auf neu erstellt werden:

- **Mesh Modifier** (`Runtime/Code/Modifiers/`) — `BaseMeshEffectTMP`, `UiBend`, `UiSkew`, `UiFFD`, `UiDistortBase`, `UiGradientBase`, `UiTessellator` usw. Diese basieren vollständig auf `BaseMeshEffect`, `VertexHelper` und `CanvasRenderer` — Konzepte, die im UI Toolkit nicht existieren. Visuelle Effekte müssen über USS, Custom Shader oder `GenerateVisualContent()` neu implementiert werden.
- **Layout Groups** (`UiGridLayoutGroup`, `UiRadialLayoutGroup`, `UiHorizontalOrVerticalLayoutGroup`) — alle erweitern `LayoutGroup` aus UnityEngine.UI. Das UI Toolkit besitzt ein eigenes Flex-basiertes Layoutsystem; die zugrundeliegenden Algorithmen können teilweise weiterverwendet werden.
- **Canvas/CanvasScaler/GraphicRaycaster-Infrastruktur** in `UiView` und `UiMain` — die gesamte Rendering-Schicht wird durch den UIDocument + Panel-Stack-Ansatz ersetzt.
- **Graphic-basiertes Bild-Rendering** in `UiImage` und `UiButtonBase` (CrossFadeColor, Material-Wechsel im deaktivierten Zustand).
- **EventSystems-Pointer-Interfaces** (`IPointerDownHandler` usw.) in `UiButtonBase` — ersetzt durch das eigene Eventsystem des UI Toolkits.

### Was weitgehend unverändert übernommen werden kann

| Subsystem | Dateien | Geschätzter Wiederverwendungsgrad |
|---|---|---|
| Storage & Persistenz | `Runtime/Code/Storage/` | ~100 % |
| State Machine | `Runtime/Code/StateSystem/` | ~100 % |
| Lokalisierungs-Core | `Runtime/Code/Loca/` (nur Core) | ~85 % |
| Animations-Framework | `Runtime/Code/AnimationComponents/` | ~85 % |
| Allgemeine Utilities | `GeneralUtility.cs` usw. | ~95 % |
| Style-Daten & Config | `UiSkin`, `UiStyleConfig`, `UiStyleManager` | ~90 % |
| Bootstrap | `Bootstrap.cs` | ~80 % |

### Was teilweise umgeschrieben werden muss

| Subsystem | Hinweise |
|---|---|
| `UiThing` | RectTransform-Abhängigkeit entfernen; Lifecycle auf VisualElement anpassen |
| `UiPanel` | Show/Hide-State-Machine und Events behalten; GameObject.SetActive ersetzen |
| `UiMain` | Navigation-Stack, Pooling-Factory, Scene-Loading behalten; Canvas-Init ersetzen |
| `UiAbstractApplyStyle` | Generisches Muster behalten; UGUI-Setter durch USS-Style-Setter ersetzen |
| `UiRequester` / Dialoge | Async-Task-API und Dialog-Muster behalten; UI-Layer neu aufbauen |
| `PlayerSettings` | Datenmodell und Persistenz behalten; Control-Bindings neu aufbauen |
| `DateTimePicker` | Datum/Uhrzeit-Algorithmen behalten; UI vollständig neu aufbauen |
| `UiTextContainer` | Abstraktionskonzept behalten; TMP/UGUI-Bindings ersetzen |
| Lokalisierungs-UI | `TextMeshProUGUI`-, `Button`-, `Toggle`-Bindings ersetzen |

---

## Empfohlene Strategie: Phasenweise Migration mit Abstraktionsschicht

Statt eines vollständigen Rewrites oder paralleler Codebases wird empfohlen:

1. **Alle Geschäftslogik in framework-neutrale Klassen auslagern** (die meisten sind es bereits, außer in der Component-Schicht).
2. **Neue UI-Toolkit-Basisklassen** (`UiThingVE`, `UiPanelVE`, `UiViewVE`) vorübergehend neben den alten aufbauen.
3. **Subsystem für Subsystem migrieren**, beginnend bei den unabhängigen Core-Systemen und schrittweise zur UGUI-gekoppelten Rendering-Schicht vorarbeiten.
4. **Mesh-Modifier-System fallenlassen** — es gibt kein leichtgewichtiges Äquivalent. Fortgeschrittene visuelle Effekte werden künftig USS/Shader-basiert sein.

---

## Phasen

### Phase 1 — Fundament: Neue Basisklassen
*Voraussetzung für alles weitere*

**Ziel**: VisualElement-basierte Äquivalente für `UiThing`, `UiPanel` und `UiView` definieren.

Aufgaben:
- `UiThingVE` definieren — verwaltet Lifecycle-Events, Event-Bus-Subscriptions und das `AddEventListeners`/`RemoveEventListeners`-Muster auf Basis eines `VisualElement`.
- `UiPanelVE` definieren mit der bestehenden Show/Hide-State-Machine, Animations-Callbacks (`EvOnBeginShow` usw.) und Pool/Destroy-on-Hide-Verhalten.
- `UiViewVE` definieren als Vollbild- oder geschichtetes Root-Element — ersetzt das Canvas + CanvasScaler + GraphicRaycaster-Muster. Entscheidung: Ein `UIDocument` pro View oder ein einziges `UIDocument` mit Panel-Stack?
- `UiMain` anpassen: `UiViewVE`-Instanzen erzeugen, `InitView()` Canvas-Initialisierung und Sibling-Index-Z-Ordering entfernen, durch Visual-Tree-Order oder `sortingOrder` auf `UIDocument` ersetzen.

Zu treffende Grundsatzentscheidungen:
- **Ein UIDocument oder mehrere?** — Ein einzelnes Dokument mit geschachteltem Panel-Container ist einfacher und performanter; mehrere `UIDocument`-Komponenten ermöglichen klarere Trennung auf Kosten der Komplexität.
- **Kamera-Integration** — `UiMain` benötigt aktuell eine Kamera; prüfen, ob das weiterhin notwendig ist.

---

### Phase 2 — Controls: Kern-UI-Element-Wrapper
*Kann parallel zu Phase 1 starten, sobald das Interface definiert ist*

**Ziel**: UGUI-Control-Wrapper durch UI-Toolkit-Äquivalente ersetzen.

- `UiButton` → wraps `UnityEngine.UIElements.Button`
- `UiToggle` → wraps `UnityEngine.UIElements.Toggle`
- `UiSlider` → wraps `UnityEngine.UIElements.Slider`
- `UiTextContainer` → wraps `Label` / `TextField`
- `UiImage` → wraps `Image` (VisualElement mit Background)
- `UiDropdown` → wraps `DropdownField` oder eigene Popup-Implementierung
- `UiScrollRect` → wraps `ScrollView`; Ensure-Visible- und Tween-Scroll-Logik portieren

Für `UiButtonBase`: `IPointerDownHandler` / EventSystems-Events durch UI-Toolkit-`RegisterCallback<PointerDownEvent>()` ersetzen.

---

### Phase 3 — Layoutsystem
*Benötigt Phase 1*

**Ziel**: Äquivalente für die benutzerdefinierten Layout Groups bereitstellen.

- `UiGridLayoutGroup` → prüfen, ob `GridLayout` (UI Toolkit, experimentell) oder Flex-Wrapping die Anwendungsfälle abdeckt; Constraint/Cell-Size-Algorithmus bei Bedarf als Custom `VisualElement` portieren.
- `UiRadialLayoutGroup` → als Custom VisualElement mit `generateVisualContent` oder absolutem Positionierungsmathe neu implementieren.
- `UiHorizontalOrVerticalLayoutGroup` → vermutlich durch Standard-Flex-Layout in USS ersetzt.

---

### Phase 4 — Dialoge und komplexe Panels

**Ziel**: Mehrteilige Dialog-Komponenten portieren.

- `UiRequester` — vollständige async-Task-basierte Dialog-API behalten; UXML-Template neu aufbauen.
- `UiPopup` — als schwebendes `VisualElement` neu aufbauen; Anker-relative Positionierung über `WorldBoundingBox` oder eigene Platzierungslogik.
- `UiModal` — "Außen-Klick-zum-Schließen"-Muster mit `PointerDownEvent` im `TrickleDown`-Phase portieren.
- `UiGridPicker`, `UiDateTimePicker` — UI neu aufbauen; Auswahl- und Datum/Uhrzeit-Logik behalten.
- `PlayerSettings`-Dialog — Datenmodell und Key-Binding-System behalten; Control-Bindings neu aufbauen.

---

### Phase 5 — Styling-System-Adapter

**Ziel**: Bestehende Skin/Style-Infrastruktur beibehalten und mit UI Toolkit verbinden.

`UiSkin`, `UiStyleConfig` und `UiStyleManager` sind framework-unabhängig und müssen nicht verändert werden.

Neue Style-Applier-Varianten erstellen:
- `UiApplyStyleColor_VE` — wendet Farbe als `style.color` oder `style.backgroundColor` an
- `UiApplyStyleFont_VE` — wendet Font-Größe / Font-Asset auf ein `Label` an
- `UiApplyStyleUSS_VE` — wechselt zur Laufzeit USS-Klassen bei Skin-Änderungen

Die Tween-basierte Skin-Transition (aktuell `SetSkin(name, tweenDuration)`) kann über dasselbe Tween-Framework weiterarbeiten, da Tweening gegen skalare/Farbwerte operiert, nicht gegen UGUI-spezifische APIs.

---

### Phase 6 — Lokalisierungs-UI-Schicht

**Ziel**: Bestehenden Lokalisierungs-Core mit UI-Toolkit-Textelementen verbinden.

- `UiLocalizedTextMeshProUGUI` durch eine Variante ersetzen, die auf `Label` zielt.
- `UiLanguageToggle` / `UiLanguageSelectDropdown` durch UI-Toolkit-Äquivalente ersetzen.
- `LocaManager`, Provider-System, Pluralisierung und Sync-Tools (Excel, Google Sheets) bleiben unverändert.

---

### Phase 7 — Visuelle Effekte

**Ziel**: Ersatz für das Mesh-Modifier-System bereitstellen.

Mögliche Ansätze (nicht gegenseitig ausschließend):
1. **USS-Transitions und -Animationen** — decken einfache Farb-, Opacity-, Größen- und Transform-Effekte ab. Ausreichend für die meisten interaktiven Feedbacks (Button-Hover, Fade-In/Out).
2. **Custom Shader mit `UxmlElement`** — für Farbverläufe und Verzerrungseffekte.
3. **`GenerateVisualContent()`-Callbacks** — für prozedurale Mesh-Zeichnung innerhalb eines `VisualElement`.
4. **Fortgeschrittene Effekte streichen** (Bend, FFD, Tessellierung) — diese sind selten spielkritisch und haben kein leichtgewichtiges UI-Toolkit-Äquivalent.

Das Animations-Framework (`UiSimpleAnimation`, `UiSimpleChildrenAnimation`) ist weitgehend wiederverwendbar, da es `DigitalRuby.Tween` nutzt und nicht UGUI-spezifisch ist.

---

### Phase 8 — Editor-Tooling aktualisieren

**Ziel**: Editor-Inspektoren und Werkzeuge für die neuen Komponententypen aktualisieren.

- Editoren für `Storage`-, `Loca`-, `StateSystem`-, `StylingSystem`-Daten — minimale Änderungen notwendig.
- Custom Inspectors für `UiThingVE`, `UiPanelVE` — neu schreiben.
- `UiToolkitConfigurationWindow` — prüfen, welche Konfigurationsoptionen noch relevant sind.
- `DoxygenWindow`, Asset Processors — keine Änderungen notwendig.

---

## Was gestrichen werden sollte

| System | Begründung |
|---|---|
| `BaseMeshEffectTMP` und alle Unterklassen | Kein `CanvasRenderer` / `VertexHelper` im UI Toolkit |
| `UiFixTMPMesh` | TMP-in-UGUI-spezifischer Workaround |
| `UiRubberband` | UGUI-Rendering-Voraussetzung |
| `UiDistortGroup` | Abhängig vom Mesh-Modifier-System |
| `UiTransitionOverlay` (falls Canvas-basiert) | Muss evaluiert werden |
| UGUI-basierte `LayoutGroup`-Unterklassen | Durch Flex / Custom VisualElement ersetzen |
| Kamera-Anforderung in `UiMain` | Vermutlich nicht mehr nötig |

---

## Wichtige Architekturentscheidungen (vor Phase 1 klären)

1. **UIDocument-Topologie**: Ein globales UIDocument pro Szene oder eines pro UiView?
2. **Layer / Z-Ordering**: USS-`z-index`, Visual-Tree-Reihenfolge oder mehrere UIDocuments mit `sortingOrder`?
3. **Panels vs. VisualElements**: Soll `UiView` weiterhin einem Prefab+MonoBehaviour entsprechen, das einen VisualElement-Baum besitzt, oder zu einer reinen VisualElement-Unterklasse werden?
4. **UXML für alle Layouts?** — Rein C#-basierter VisualElement-Aufbau vs. UXML-Templates für jedes Panel.
5. **TextMeshPro im UI Toolkit**: TMP funktioniert im UI Toolkit als `VisualElement`-Unterklasse. Entscheiden, ob TMP oder das eingebaute `Label` verwendet wird.
6. **Rückwärtskompatibilität**: Soll das neue Toolkit weiterhin Projekte unterstützen, die die alte UGUI-basierte API verwenden, oder ist dies ein harter Breaking-Change?

---

## Aufwandsschätzung (einzelner Entwickler)

| Phase | Geschätzte Dauer |
|---|---|
| Phase 1 — Fundament | 3–4 Wochen |
| Phase 2 — Controls | 3–4 Wochen |
| Phase 3 — Layouts | 2–3 Wochen |
| Phase 4 — Dialoge | 3–4 Wochen |
| Phase 5 — Styling | 1–2 Wochen |
| Phase 6 — Lokalisierungs-UI | 1 Woche |
| Phase 7 — Visuelle Effekte | 2–4 Wochen |
| Phase 8 — Editor-Tooling | 1–2 Wochen |
| **Gesamt** | **~16–24 Wochen** |

Die große Spanne in Phase 7 hängt stark davon ab, welche visuellen Effekte als unverzichtbar eingestuft werden.

---

## Risikoübersicht

| Risiko | Schwere | Gegenmaßnahme |
|---|---|---|
| Kein direktes Mesh-Modifier-Äquivalent | Hoch | Scope-Reduzierung akzeptieren; USS/Shader für Core-Effekte nutzen |
| UIDocument-Topologie-Wahl fixiert Architektur | Hoch | Beide Ansätze prototypisch testen vor der Entscheidung |
| Kamera-Integration in `UiMain` komplex | Mittel | Prüfen, ob Kamera mit UI Toolkit noch benötigt wird |
| UXML-Binding-Overhead bei dynamischen Daten | Mittel | Data-Binding-API (Unity 6+) oder manuelles Refresh-Muster verwenden |
| Style-Applier-System benötigt vollständige Adapter-Schicht | Niedrig | Muster ist sauber; Aufwand ist mechanisch |
