# Porting Plan: unity-gui-toolkit → Unity UI Toolkit (UXML/USS)

## Overview

This document outlines a phased strategy for migrating `de.phoenixgrafik.ui-toolkit` from UGUI (UnityEngine.UI) to the Unity UI Toolkit (UXML/USS). It is intended as a planning reference, not a complete specification.

---

## Scope Assessment

### What becomes obsolete

These subsystems have no meaningful equivalent in UI Toolkit and should be dropped or replaced from scratch:

- **Mesh Modifiers** (`Runtime/Code/Modifiers/`) — `BaseMeshEffectTMP`, `UiBend`, `UiSkew`, `UiFFD`, `UiDistortBase`, `UiGradientBase`, `UiTessellator`, etc. These are built entirely on `BaseMeshEffect`, `VertexHelper`, and `CanvasRenderer` — concepts that do not exist in UI Toolkit. Visual effects must be reimplemented via USS, custom shaders, or `GenerateVisualContent()`.
- **Layout Groups** (`UiGridLayoutGroup`, `UiRadialLayoutGroup`, `UiHorizontalOrVerticalLayoutGroup`) — all extend `LayoutGroup` from UnityEngine.UI. UI Toolkit has its own Flex-based layout system; the underlying algorithms may be partially reusable.
- **Canvas/CanvasScaler/GraphicRaycaster infrastructure** in `UiView` and `UiMain` — the entire rendering layer is replaced by the UI Document + Panel Stack approach.
- **Graphic-based image rendering** in `UiImage` and `UiButtonBase` (CrossFadeColor, material swap on disabled state).
- **EventSystems pointer interfaces** (`IPointerDownHandler`, etc.) on `UiButtonBase` — replaced by UI Toolkit's own event system.

### What can be kept largely unchanged

| Subsystem | Files | Estimated reuse |
|---|---|---|
| Storage & Persistence | `Runtime/Code/Storage/` | ~100% |
| State Machine | `Runtime/Code/StateSystem/` | ~100% |
| Localization core | `Runtime/Code/Loca/` (core only) | ~85% |
| Animation framework | `Runtime/Code/AnimationComponents/` | ~85% |
| General utilities | `Runtime/Code/Helpers/GeneralUtility.cs` etc. | ~95% |
| Style data & config | `UiSkin`, `UiStyleConfig`, `UiStyleManager` | ~90% |
| Bootstrap | `Bootstrap.cs` | ~80% |

### What needs partial rewriting

| Subsystem | Notes |
|---|---|
| `UiThing` | Remove RectTransform requirement; adapt lifecycle to VisualElement |
| `UiPanel` | Keep show/hide state machine and events; replace GameObject.SetActive |
| `UiMain` | Keep navigation stack, pooling factory, scene loading; replace Canvas init |
| `UiAbstractApplyStyle` | Keep generic pattern; replace UGUI component setters with USS style setters |
| `UiRequester` / dialogs | Keep async Task API and dialog patterns; rebuild UI layer |
| `PlayerSettings` | Keep data model and persistence; rebuild control bindings |
| `DateTimePicker` | Keep date/time algorithms; rebuild UI entirely |
| `UiTextContainer` | Keep abstraction concept; replace TMP/UGUI bindings |
| Localization UI | Replace `TextMeshProUGUI`, `Button`, `Toggle` bindings |

---

## Recommended Strategy: Phased Migration with Abstraction Layer

Rather than a full rewrite or a parallel codebase, the recommended approach is to:

1. **Extract all business logic into framework-neutral classes** (most already are, except in the component layer).
2. **Build new UI Toolkit base classes** (`UiThingVE`, `UiPanelVE`, `UiViewVE`) alongside the old ones temporarily.
3. **Migrate subsystem by subsystem**, starting with the independent core systems and working inward toward the UGUI-coupled rendering layer.
4. **Drop the mesh modifier system** — it has no viable lightweight equivalent. Advanced visual effects will be USS/shader-based going forward.

---

## Phases

### Phase 1 — Foundation: New Base Classes
*Prerequisite for everything else*

**Goal**: Define the new `VisualElement`-based equivalents of `UiThing`, `UiPanel`, and `UiView`.

Tasks:
- Define `UiThingVE` as a wrapper or base managing lifecycle events, event bus subscriptions, and the `AddEventListeners`/`RemoveEventListeners` pattern on top of a `VisualElement`.
- Define `UiPanelVE` with the existing show/hide state machine, animation callbacks (`EvOnBeginShow`, etc.), and pool/destroy-on-hide behavior.
- Define `UiViewVE` as a full-screen or layered root element — replaces the Canvas + CanvasScaler + GraphicRaycaster pattern. Decide on a `UIDocument`-per-view vs. single `UIDocument` with panel stack approach.
- Adapt `UiMain` to instantiate `UiViewVE` instances, removing `InitView()` Canvas initialization and sibling-index z-ordering. Replace with visual tree order or `sortingOrder` on `UIDocument`.

Key decisions required:
- **Single UIDocument or multiple?** — Single document with a stacked panel container is simpler and performs better; multiple `UIDocument` components give clearer separation at the cost of complexity.
- **Camera integration** — `UiMain` currently requires a Camera; evaluate whether this is still needed.

---

### Phase 2 — Controls: Core UI Element Wrappers
*Can start in parallel with Phase 1 once interface is defined*

**Goal**: Replace UGUI control wrappers with UI Toolkit equivalents.

- `UiButton` → wraps `UnityEngine.UIElements.Button`
- `UiToggle` → wraps `UnityEngine.UIElements.Toggle`
- `UiSlider` → wraps `UnityEngine.UIElements.Slider`
- `UiTextContainer` → wraps `Label` / `TextField`
- `UiImage` → wraps `Image` (VisualElement with background)
- `UiDropdown` → wraps `DropdownField` or custom popup implementation
- `UiScrollRect` → wraps `ScrollView`, port the ensure-visible and tween-scroll logic

For `UiButtonBase`: replace `IPointerDownHandler` / EventSystems events with UI Toolkit's `RegisterCallback<PointerDownEvent>()` etc.

---

### Phase 3 — Layout System
*Depends on Phase 1*

**Goal**: Provide equivalents for the custom layout groups.

- `UiGridLayoutGroup` → evaluate whether `GridLayout` (UI Toolkit experimental) or Flex wrapping covers the use cases. Port constraint/cell-size algorithm if needed as a custom `VisualElement`.
- `UiRadialLayoutGroup` → reimplement as a custom VisualElement with `generateVisualContent` or absolute positioning math.
- `UiHorizontalOrVerticalLayoutGroup` → likely replaced by standard Flex layout in USS.

---

### Phase 4 — Dialogs and Complex Panels

**Goal**: Port multi-part dialog components.

- `UiRequester` — keep the async Task-based dialog API entirely; rebuild the UXML template.
- `UiPopup` — rebuild as a floating `VisualElement`; anchor-relative positioning can use `WorldBoundingBox` or custom placement logic.
- `UiModal` — port the "click outside to close" pattern using `PointerDownEvent` with `TrickleDown` phase.
- `UiGridPicker`, `UiDateTimePicker` — rebuild UI; keep selection and date/time logic.
- `PlayerSettings` dialog — keep data model and key-binding system; rebuild control bindings.

---

### Phase 5 — Styling System Adapter

**Goal**: Keep the existing skin/style infrastructure and connect it to UI Toolkit.

The `UiSkin`, `UiStyleConfig`, and `UiStyleManager` are framework-independent and need no changes.

Create new style applier variants:
- `UiApplyStyleColor_VE` — applies color as `style.color` or `style.backgroundColor`
- `UiApplyStyleFont_VE` — applies font size / font asset to a `Label`
- `UiApplyStyleUSS_VE` — switches USS classes at runtime for skin changes

The tween-based skin transition (currently `SetSkin(name, tweenDuration)`) can work through the same tween framework since tweening happens against scalar/color values, not UGUI-specific APIs.

---

### Phase 6 — Localization UI Layer

**Goal**: Connect the existing localization core to UI Toolkit text elements.

- Replace `UiLocalizedTextMeshProUGUI` with a variant that targets `Label`.
- Replace `UiLanguageToggle` / `UiLanguageSelectDropdown` with UI Toolkit equivalents.
- The `LocaManager`, provider system, pluralization, and sync tools (Excel, Google Sheets) are entirely unchanged.

---

### Phase 7 — Visual Effects

**Goal**: Provide replacements for the mesh modifier system.

Approach options (not mutually exclusive):
1. **USS transitions and animations** — cover simple color, opacity, size, and transform effects. Covers most interactive feedback (button hover, fade in/out).
2. **Custom shaders with `UxmlElement`** — for gradient fills and distortion effects.
3. **`GenerateVisualContent()` callbacks** — for procedural mesh drawing inside a `VisualElement`.
4. **Drop advanced effects** (bend, FFD, tessellation) — these are rarely critical to gameplay UI and have no lightweight UI Toolkit equivalent.

The animation framework (`UiSimpleAnimation`, `UiSimpleChildrenAnimation`) itself is largely reusable since it uses `DigitalRuby.Tween` and is not UGUI-specific.

---

### Phase 8 — Editor Tooling Update

**Goal**: Update editor inspectors and tools for new component types.

- Editors for `Storage`, `Loca`, `StateSystem`, `StylingSystem` data — minimal changes needed.
- Custom inspectors for `UiThingVE`, `UiPanelVE` — rewrite.
- `UiToolkitConfigurationWindow` — evaluate what config options remain relevant.
- `DoxygenWindow`, asset processors — no changes needed.

---

## What to Drop

| System | Reason |
|---|---|
| `BaseMeshEffectTMP` and all subclasses | No `CanvasRenderer` / `VertexHelper` in UI Toolkit |
| `UiFixTMPMesh` | TMP-in-UGUI specific workaround |
| `UiRubberband` | UGUI rendering assumption |
| `UiDistortGroup` | Depends on mesh modifier system |
| `UiTransitionOverlay` (if Canvas-based) | Needs evaluation |
| UGUI-based `LayoutGroup` subclasses | Replace with Flex / custom VisualElement |
| Camera requirement in `UiMain` | Likely no longer needed |

---

## Key Architectural Decisions (resolve before starting Phase 1)

1. **UIDocument topology**: One global UIDocument per scene, or one per UiView?
2. **Layer/z-ordering**: Use USS `z-index`, visual tree order, or multiple UIDocuments with `sortingOrder`?
3. **Panels vs. VisualElements**: Should `UiView` still map to a prefab+MonoBehaviour that owns a VisualElement tree, or become a pure VisualElement subclass?
4. **UXML for all layouts?** — Pure C# VisualElement construction vs. UXML templates for every panel.
5. **TextMeshPro in UI Toolkit**: TMP works in UI Toolkit but as a `VisualElement` subclass. Decide whether to use it or the built-in `Label`.
6. **Backward compatibility**: Will the new toolkit still support projects using the old UGUI-based API, or is this a hard breaking change?

---

## Effort Estimate (single developer)

| Phase | Estimated duration |
|---|---|
| Phase 1 — Foundation | 3–4 weeks |
| Phase 2 — Controls | 3–4 weeks |
| Phase 3 — Layouts | 2–3 weeks |
| Phase 4 — Dialogs | 3–4 weeks |
| Phase 5 — Styling | 1–2 weeks |
| Phase 6 — Localization UI | 1 week |
| Phase 7 — Visual Effects | 2–4 weeks |
| Phase 8 — Editor tooling | 1–2 weeks |
| **Total** | **~16–24 weeks** |

The wide range in Phase 7 depends heavily on which visual effects are considered essential.

---

## Risk Summary

| Risk | Severity | Mitigation |
|---|---|---|
| No direct mesh modifier equivalent | High | Accept scope reduction; use USS/shaders for core effects |
| UIDocument topology choice locks in architecture | High | Prototype both approaches before committing |
| `UiMain` camera integration complexity | Medium | Evaluate whether camera is still needed with UI Toolkit |
| UXML binding overhead for dynamic data | Medium | Use data binding API (Unity 6+) or manual refresh pattern |
| Style applier system needs full adapter layer | Low | Pattern is clean; effort is mechanical |
