# 3D Icons

## Overview

This document plans **dynamic 3D icons**: UI elements that show a 3D object rendered by a camera into a render
texture, each with its own lighting mood, unaffected by each other and by the scene. It is a planning
reference for the implementation on branch `3d-icons`, not a complete specification.

A previous implementation of this feature (outside this package) took several weeks. Almost none of that time
went into the obvious part - camera, render texture, RawImage. It went into **isolation** (something from the
scene or from another icon always leaks in), **edge cases of the lifecycle** (edit mode, pooling, async loads,
lost textures) and **image quality** (alpha fringes, framing). The plan is ordered so that these are solved
and *proven* early, before comfort features are built on top.

The author of the previous implementation confirms the order of difficulty: the icon itself was easy; the work
was a fight against Unity's global environment handling. "If there were a scene without influence of the global
environment, all of it would have been trivial." Phase 1 therefore starts by finding out how close
`Camera.scene` gets to exactly that (see *Spike: scene isolation*).

**Requirements**

| # | Requirement | Consequence |
|---|---|---|
| R1 | Built-in render pipeline first; URP and HDRP must be possible later | Pipeline-specific code behind one interface from day one |
| R2 | Most icons are rendered once and then stay a static texture; on request they re-render periodically | Static is the default and the cheap path; periodic is static on a timer |
| R3 | A fully animated mode exists; the object brings its own animation | The icon is only responsible for the stage, never for animation |
| R4 | Up to ~100 icons at once, at most one of them animated | Render budget per frame, shared results, memory discipline |
| R5 | Lighting moods are presets, authored as prefabs | Lights are real `Light` components a designer can move |
| R6 | Icons are prefabs | `UiIcon3D.prefab` in the library, client variants per use case |
| R7 | Objects can be loaded dynamically | Loading through `AssetManager` / `CanonicalAssetKey` |
| R8 | Every object has its own lighting, isolated from other icons and the scene | Isolation by time, not by space (see below) |
| R9 | Objects may bring their own lights (e.g. a glowing crystal) | Object lights are treated like preset lights: restricted to the stage, active only during their own render |
| R10 | Framing = fit into the rect | No consistent world scale across icons needed |

**What this is not:** a general "render 3D into UI" system for cutscenes or character screens with
post-processing. One stage, one object, one preset per render.

**Nor a replacement for `Ui3DObject`.** That component puts a 3D object directly into the UI hierarchy, fitted to
its RectTransform by a vertex shader - so it *is* part of the world and lit by the scene. That is a feature, not a
flaw: a weapon in a shooter's HUD lit by the level's light looks great, and an existing project uses it exactly
that way. `Ui3DObject` = deliberately part of the world, `UiIcon3D` = deliberately not. Both stay.

---

## What already exists

| Piece | Location | Role in this feature |
|---|---|---|
| Render textures linked by keyword, created on demand | `RenderTextureManager`, `UiRenderTextureProducer`, `UiRawImageRenderTextureConsumer` (`Runtime/Code/Rendering`) | Storage of every icon result; sharing of identical icons via keyword |
| Per-camera overrides of global settings, no reflection, no boxing | `PerCameraSetting<T>`, `UiAbstractPerCameraSettings`, `UiCameraRenderSettings` | Ambient, fog, reflection, shadow settings per preset |
| Asset loading abstraction (Addressables, Resources, direct) | `AssetManager`, `IAssetProvider`, `CanonicalAssetKey` | Dynamic loading of icon objects |
| Pooling | `UiPool`, `IPoolable` | Icons inside pooled list items |
| Edit-mode ticking | `EditorUpdater`, `EditorApplication.update` | Edit-mode preview (never `delayCall`, see CLAUDE.md) |
| UI base class | `UiThing` | Base of `UiIcon3D` |

Both rendering building blocks are covered by EditMode tests (`TestRenderTextureManager`, `TestPerCameraSetting`).

---

## Spike: scene isolation (result)

Unity keeps `RenderSettings` (ambient, fog, skybox, reflection) **per scene**, but only those of the *active* scene
are used. `Camera.scene` restricts a camera to the contents of one scene and exists at runtime. The question was
whether a separate "icon scene" gives isolation for free. `Tests/PlayMode/SpikeIcon3DSceneIsolation.cs` measured it
(Built-in, Unity 6000.0):

| # | Question | Result |
|---|---|---|
| Q1 | Are objects of other scenes invisible to a camera with `Camera.scene` set? | **No** - `Camera.scene` does nothing for a runtime-created scene |
| Q2 | Do lights of other scenes leave the icon unlit? | **No** |
| Q3 | Do icon-scene lights leave the main scene unlit? | **No** |
| Q4/Q5 | Does the icon get its own scene's environment? | **No** - always the active scene's |
| Q6 | Does switching the active scene around the render call switch the environment? | **Yes**, immediately, within the frame, and completely |
| Q7 | Do per-camera overrides (`UiCameraRenderSettings`) change the image? | **Yes** for ambient and fog |
| Q8 | Is the override image identical to the scene-switch image? | First run **no**: the oracle found the scene's **environment reflection** (skybox) leaking in, ~0.08 per channel even at smoothness 0 (Q8a keeps that as a finding). With reflection overridden as well: **identical**, max difference 0.0000 (Q8b) |
| Q9 | Does the override *set* a preset reflection, not just switch it off? | **Yes** - yellow cubemap visible and identical to the oracle |

**Decision**

- Objects and lights: a separate scene gives nothing. Reserved layer + masking of foreign lights stay as planned.
- Environment: switching the active scene would be the complete solution, but it fires
  `SceneManager.activeSceneChanged` twice per render - for the animated icon twice per frame, forever. Game code
  listening to that event breaks; the toolkit itself has such a listener (`UiStartupOverlayView` stops its run on
  it). **Not used in production.**
- Instead the scene switch becomes the **test oracle**: it swaps the complete environment, including inputs nobody
  thought of. Every isolation test renders once with the overrides and once with the scene switch; the images must
  match (Q8). A difference is a missing override, found the day it appears rather than weeks later. This turns the
  open-ended "fight against the environment" into a checklist that tells us when it is complete.
- The test suite keeps Q1-Q6 as assertions of the behaviour found, so a Unity update that changes it is noticed.

---

## Core idea: isolation by time

Putting every icon on its own layer fails at 32 layers, and putting all icons on one layer lets their lights
shine on each other. Instead there is **one stage** on **one reserved layer**, and icons are rendered **one after
another**. During a single render call exactly one object and one preset are active; everything that is global
is overridden for the duration of that call and restored afterwards.

```
for each pending icon (within the frame budget):
    activate object instance + preset lights          (everything else on the stage is inactive)
    apply preset render settings                      (ambient, fog off, reflection, shadows)
    mask foreign lights out of the icon layer         (scene lights with "Everything")
    render stage camera -> scratch target (depth, MSAA)
    resolve/copy scratch -> icon's storage texture    (no depth, no MSAA)
    restore everything, deactivate
```

What can leak in, and what stops it:

| Leak | Stopped by |
|---|---|
| Scene objects | Stage camera culls only the icon layer |
| Icon objects in the scene cameras | Validator: no other camera may see the icon layer (incl. `UiMain`'s camera) |
| Scene lights | Preset lights' culling mask = icon layer only; foreign lights temporarily masked out during the render |
| Lights of other icons | Only one preset active per render |
| Ambient light, fog, skybox | `UiCameraRenderSettings` values of the preset, applied around the render |
| **Light probes of the scene** | `lightProbeUsage = Off` on every renderer of an icon instance (otherwise the scene's probes at the stage position light the object) |
| Reflection probes of the scene | `reflectionProbeUsage = Off` or a preset-owned probe; stage far outside the level |
| Shadows from scene casters | Casters are culled by the camera's layer mask |
| Animated icon in other icons' renders | Its renderers get `forceRenderingOff` and its own lights are disabled while others render |
| Lights the object brings along (R9) | Culling mask restricted to the stage; active only while their instance renders |

---

## Architecture

### Components and services

| Type | Kind | Responsibility |
|---|---|---|
| `UiIcon3D` | `UiThing`, `IPoolable`, on a `RawImage` | What to show (object source, preset, mode, framing). Requests renders; never renders itself. Consumer of its render texture keyword. |
| `UiIcon3DPreset` | MonoBehaviour on a **preset prefab** | Lighting mood: child `Light`s, a `UiCameraRenderSettings` (ambient, fog, reflection, shadows), camera framing (view rotation, projection, FOV, padding), background colour. |
| `UiIcon3DBoundsHint` | MonoBehaviour on an **object prefab** (optional) | Overrides bounds / pivot / preferred view rotation where automatic framing fails (skinned meshes, particles, odd pivots). |
| `UiIcon3DRenderer` | static service + hidden stage GameObject | Owns the stage and its camera, the request queue, the frame budget, the loaded assets and instances. Producer for all icon keywords. |
| `Icon3DRenderScope` | `IDisposable` struct | Everything that is applied for one render and restored afterwards. |
| `Icon3DFitter` | static | Bounds -> camera distance / ortho size / near / far. |
| `IIcon3DRenderBackend` | interface | Pipeline specifics: render call, camera setup, environment. `BuiltinIcon3DBackend` now; URP/HDRP later. |
| `UI/Icon3D` | shader | Premultiplied-alpha UI shader, with stencil masking and `RectMask2D` clipping like `UI/Default`. |

### Static, periodic, animated

- **Static** (default): load -> instantiate on the stage -> prepare -> fit -> render once -> destroy the instance.
  The loaded asset stays referenced, so a re-render (resize, preset change) is a cheap instantiate.
- **Periodic**: static, re-rendered every n seconds (e.g. a preset that follows the time of day, a shader that
  changes slowly). The instance is kept but inactive between renders, so it costs no update time. Identical
  periodic icons still share one texture and one render.
- **Animated**: the instance stays alive and active so its `Animator`, `ParticleSystem`s and scripts keep running;
  it is rendered every frame (optionally every n-th frame), last in the frame, after the static batch.
  Framing is computed once, not per frame, or the image pumps with the animation.

### Shared results: the keyword is a content hash

Lists often show the same item many times. The render texture keyword of a static icon is derived from
everything that determines the image: `object key + preset + pixel size + view rotation + fit settings`.
Identical icons therefore get the **same keyword**, `RenderTextureManager` gives them the **same texture**, and
the object is rendered **once**. This is the main reason the keyword manager was built first. Animated icons
always get a unique keyword.

### Memory discipline

Storing every icon in a render texture with depth buffer and MSAA wastes a lot of memory
(100 x 256² x (4 B colour + 4 B depth) x 4 MSAA ≈ 200 MB). So:

- **One shared scratch target** with depth and MSAA, `RenderTexture.GetTemporary` sized to the largest pending
  request, used for the actual render.
- **Storage textures** (the ones `RenderTextureManager` hands out for icons): colour only, no depth, no MSAA.
  The scratch result is resolved and copied into them.
- Later the storage can become atlas pages (see phase 6) without touching the rest.

### Lights on objects (R9)

Nothing speaks against them; they only need the same treatment as preset lights:

- **Culling mask** restricted to the stage (layer or scene, depending on the spike), set automatically on the instance.
- **Only active during their own render.** For static instances that is given (inactive otherwise). The animated
  instance is permanently active, so its lights are switched off while other icons render, together with
  `forceRenderingOff`.
- **Pixel light budget**: Built-in forward renders only `QualitySettings.pixelLightCount` lights per pixel, the rest
  per vertex/SH. Preset lights plus object lights must fit; the preset's `UiCameraRenderSettings` raises the count
  for the icon render if needed, and preset lights get `LightRenderMode.ForcePixel`.
- Baked lights on objects do nothing at runtime; the validator warns about them.

### Transparency

The camera clears to `(0,0,0,0)`. Edges (MSAA, texture filtering) then contain colour already multiplied by
alpha, and drawing that with ordinary alpha blending gives dark fringes. `UI/Icon3D` therefore blends with
`One, OneMinusSrcAlpha`. The tint colour has to be premultiplied in the vertex stage as well.

Transparent materials *on the object* (glass, particles) write a wrong alpha with standard blending
(`SrcAlpha, OneMinusSrcAlpha` also applies to the alpha channel). Phase 1 documents this; phase 6 offers a fix.

### Render settings on a preset without a camera

`UiAbstractPerCameraSettings` currently requires a `Camera`, because it binds itself to that camera's render
callbacks. A preset prefab must not contain a camera (it would render). Change: drop `RequireComponent(Camera)`;
without a camera the component only registers nothing and is applied manually via the existing public
`Apply()` / `Restore()`. The renderer calls those around its render call.

### Pipeline abstraction (R1)

```csharp
public interface IIcon3DRenderBackend
{
    void SetupCamera( Camera _camera );                      // e.g. URP: UniversalAdditionalCameraData, no post-processing
    void Render( Camera _camera, RenderTexture _target );    // Built-in: Camera.Render(); URP: SubmitRenderRequest
    IDisposable ApplyEnvironment( UiIcon3DPreset _preset );  // Built-in: RenderSettings; HDRP: volume on a preset layer
}
```

Notes for later, so the interface does not need to change:

- **URP**: `RenderPipeline.SubmitRenderRequest` with `UniversalRenderPipeline.SingleCameraRequest` (2022.2+).
  Alpha survives only without post-processing. Limit of additional lights per object. Ambient comes from
  `RenderSettings` like Built-in.
- **HDRP**: environment via a `Volume` per preset and the camera's `volumeLayerMask` - isolation is built in.
  **Fixed exposure is mandatory**, otherwise auto exposure adapts every icon to grey. Alpha needs a colour buffer
  format with alpha in the HDRP asset. Lights need `HDAdditionalLightData`, so presets need HDRP variants.
- Presets are therefore pipeline-specific prefabs; their non-light data (framing, background) is shared.

### Configuration

- `UiToolkitConfiguration`: reserved icon layer, default preset, frame budget, scratch MSAA, default storage
  resolution scale.
- **Validator** (editor + development build warning): another camera sees the icon layer; a scene light includes
  the icon layer (informational, masked anyway); an object's `Animator` uses `cullingMode = CullCompletely`
  in animated mode.

---

## Phases

Each phase ends with something visible and tested. Effort estimates assume the pace of the two pre-work pieces.

### Phase 0 - Pre-work (done)

- `RenderTextureManager` + producer/consumer components, tests.
- `PerCameraSetting<T>` / `UiAbstractPerCameraSettings` / `UiCameraRenderSettings`, tests incl. a real render.

### Phase 1 - Isolated static render (the core)

0. Scene isolation spike - done, see above: ambient, fog and environment reflection overridden per camera match the
   oracle exactly. Next: turn the spike's comparison into a reusable oracle helper that all isolation tests use, and
   move the findings test from "spike" to a permanent name.
1. `UiAbstractPerCameraSettings` usable without a camera (see above).
2. Layer reservation in `UiToolkitConfiguration` + validator.
3. Stage: hidden root (`HideAndDontSave` in edit mode, `DontDestroyOnLoad` in play mode), far away; one camera,
   disabled, clear to transparent, culling mask = icon layer.
4. `Icon3DRenderScope`: preset lights on, render settings applied, foreign lights masked, probes off on the
   instance, restore in reverse.
5. `BuiltinIcon3DBackend`, scratch target, resolve into storage texture.
6. `Icon3DFitter`: bounds from renderers (sphere fit = rotation stable; box fit = tighter), padding,
   near/far from bounds.
7. `UI/Icon3D` premultiplied shader + material.
8. A minimal API without component: `UiIcon3DRenderer.RenderStatic(prefab, preset, size) -> keyword`.

**Done when** the isolation tests below pass and a dev scene shows two icons with opposite presets next to a
scene with a strong coloured directional light and fog, with no visible influence in either direction.

**Status:** implemented (`Runtime/Code/Icon3D`, `Runtime/Shaders/UI_Icon3D.shader`, `Editor/Icon3D`), tests in
`Tests/PlayMode/TestIcon3DRenderer.cs`. Deviations from the plan above, found while building it:

- **No validator for scene cameras.** Static instances and preset lights only exist (are active) during their own
  render call, so a scene camera can never see them, whatever its culling mask. The reserved layer only keeps scene
  objects out of the stage camera. The animated instance (phase 4) will be kept invisible with `forceRenderingOff`
  outside its own render. What remains is a check for scene objects on the icon layer (menu + configuration window).
- **Baseline environment.** Before the preset's settings, the backend applies a complete neutral environment (black
  flat ambient, no fog, no skybox, black custom reflection) - what an empty scene gives. Whatever a preset leaves
  open is neutral, never the scene's. The oracle test checks exactly that.
- **Physics on icon objects is left alone** for now: the runtime assembly does not reference the physics modules,
  and a static instance is never active across a physics step. The animated mode needs it (colliders of a
  permanently active instance); planned via `versionDefines` on the physics module packages.

### Phase 2 - Component, presets, prefabs

1. `UiIcon3D`: object source (direct prefab reference for now), preset, view rotation, fit mode, resolution
   scale, mode (static / periodic + interval / animated); re-render on size change / texture loss / property change; content-hash keyword.
2. `UiIcon3DPreset` + `UiIcon3DBoundsHint`.
3. Library prefabs: `UiIcon3D.prefab`, presets *Neutral*, *Warm*, *Dramatic* - **each with its own reflection
   cubemap**: the baseline reflection is black, and metal reflects almost nothing but its environment, so a preset
   without reflection renders metal black (seen in the phase 1 demo scene; `Icon3DEnvironmentUtility.CreateGradientCubemap`). Following BEST-PRACTICES, clients
   create variants in bulk.
4. Edit-mode preview (renderer works in edit mode, ticking via `EditorApplication.update`), also in the Prefab Stage.

**Done when** an icon can be placed, configured and previewed in edit mode without entering play mode,
survives domain reload and scene save without leaking objects or dirtying the scene.

**Status:** implemented (`UiIcon3D`, `UiIcon3DBoundsHint`, `StandardIcon3D.prefab`, presets Neutral/Warm/Dramatic with
fixed reflection cubemaps, generated by the dev menu *3D Icons / Dev: Rebuild Library Assets*). Tests in
`Tests/PlayMode/TestUiIcon3D.cs`. Editor: icons re-render whenever prefabs, materials, meshes or textures are
reimported (`Icon3DAssetWatcher`). Lesson: never compare a texture that may have been released with Unity's `==` -
a destroyed object equals null, the dead reference stays in the RawImage (`ReferenceEquals`, detach before release).

### Phase 3 - Dynamic loading and scale

1. Object source `CanonicalAssetKey` via `AssetManager`; one load per key, reference counted across icons.
2. Placeholder while loading (sprite or nothing); cancellation when the icon is disabled, pooled or reassigned
   before the load finishes - the late result must never end up in the wrong icon.
3. `IPoolable` support.
4. Request queue: frame budget (count and/or milliseconds), visible icons first, de-duplication.
5. Texture loss: `RenderTexture.IsCreated()` false (device reset, app resumed on mobile) -> re-render.

**Done when** a scroll list with 100 pooled items, 30 distinct objects loaded via Addressables, scrolls without
hitches, shows each distinct icon rendered once, and survives rapid scrolling (reassignment during loads).

**Status:** implemented and verified (`Icon3DAssetCache`, `UiIcon3D.PrefabId` / `CanonicalAssetRef`, loading texture,
visible-first render order). Tests in `Tests/PlayMode/TestIcon3DLoading.cs` with a fake provider that completes loads
on demand - the reassignment race is covered there. The scroll stress demo (100 icons, 30 objects, shuffle while
loading) runs stable. Results are polled, never delivered by callback: an icon only looks at its current lease, so a
late load can not reach the wrong icon by construction. The frame budget counts renders AND CPU milliseconds
(`RenderMilliseconds`, default 4, at least one render per tick; done 2026-10-05, `TestIcon3DBudget`). Open: the demo
loads through Resources - Addressables use the same abstraction but were not exercised separately.

### Phase 4 - Animated mode

1. Persistent instance, rendered every frame after the static batch; optional frame divider.
2. `forceRenderingOff` while other icons render.
3. `Animator.cullingMode` handling (forced to `AlwaysAnimate` or warning), particle systems, scripts that look for
   `Camera.main` (documented pitfall).
4. Switching an icon between static and animated (e.g. animate the selected/hovered item only).

**Done when** one animated icon runs among 99 static ones, neither affects the other, and switching the
animated one to static freezes it on its current frame without a flash.

**Status:** implemented (`UiIcon3D.EMode.Animated`, `UiIcon3DRenderer.RenderAnimated`, `Icon3DHandle.IsPlaying`).
The persistent instance gets `forceRenderingOff` and its lights off outside its own render, Animators `AlwaysAnimate`
without root motion, skinned meshes `updateWhenOffscreen`, particles `AlwaysSimulate`; physics off through
`versionDefines` (`UITK_PHYSICS`, `UITK_PHYSICS2D`). Framing once from the entry pose. Invisible animated icons keep
animating but are not rendered. Static -> animated keeps the static image until the first frame; animated -> static
freezes the current frame and frees the instance; resuming starts the animation anew. Edit mode advances Animators and
particles manually at 30 fps (scripts do not run there). Tests in `Tests/PlayMode/TestIcon3DAnimated.cs`; demo scene
shows an always animated and a hover-to-animate icon. Lesson: a test of motion must not depend on time - test runner
frames are milliseconds apart.

### Phase 5 - Tooling and documentation

1. Preset studio: editor window rendering a grid of sample objects x presets, for authoring presets.
   **Done** (2026-10-05): `Icon3DPresetStudio` (menu *3D Icons > Preset Studio...*), tests in `TestIcon3DPresetStudio`
   (EditMode: one request per cell, all released on close). The drawing was not looked at in the window itself, only
   the renders it shows.
2. Debug view: all keywords of `RenderTextureManager`, sizes, memory, users; render count per frame.
3. Documentation page + BEST-PRACTICES entry (layer reservation, preset variants, pipeline variants).

**Status:** item 3 done - guide `Documentation~/3D-Icons.md` (linked from the README), BEST-PRACTICES §6, architecture
section in CLAUDE.md / AGENTS.md. Items 1 (preset studio) and 2 (debug view) are open; worth building once real
assets show where authoring hurts.

### Phase 6 - Optional

- **Atlas** for static icons: storage pages instead of one texture per icon; `RawImage.uvRect`; fewer draw calls.
- **Alpha fix** for transparent object materials (separate alpha pass or alpha-preserving blend override).
  **Done** (2026-10-05): not a blend override (arbitrary shaders have fixed blend states) but a second render over
  white; the alpha is `1 - mean(white - black)` per pixel (`Icon3DAlphaCombine` shader, preset `AlphaMode` Auto /
  Fast / Exact). Pipeline independent, so it is also the plan for URP and HDRP. Measured: 50% glass leaves alpha 0.247
  in Fast, 0.498 in Exact. Tests: `TestIcon3DAlpha`. Cost: two renders for icons with a transparent material.
- **Shadow catcher** in presets: invisible ground that only receives shadows, written into alpha.
  **Done** (2026-10-05, Built-in): a quad under the object (`Icon3DShadowCatcher` shader) that outputs the main
  directional light's shadow as premultiplied alpha. Two findings: it must live in the OPAQUE queue and write depth,
  because directional shadows are collected in screen space from the camera depth texture (a transparent ground has no
  depth there and gets no shadow); and the shadow map needs resolution High, otherwise a small object's shadow blurs
  to a faint plateau (measured: peak alpha 0.19 of 0.6 at Low, 0.60 at High). The scene's quality level is overridden for
  the render and restored. Tests: `TestIcon3DShadowCatcher`.
- **URP backend**, then **HDRP backend**.

---

## Test strategy

Rendering is testable: render to a texture, read pixels back, compare colours. Every isolation rule gets a test
of that shape (Built-in). Where possible the expectation is not a hand-picked colour but the **oracle image**:
the same render with a clean scene made active (see the spike), which has the complete environment swapped.

| Test | Setup | Expectation |
|---|---|---|
| Preset light only | White sphere, preset with one red light | Centre pixel red, no blue |
| Two presets | Same sphere, red preset then blue preset | First result stays red after the second render |
| Scene light | Scene directional light, bright green, culling mask *Everything* | Icon has no green |
| Scene ambient / fog | Scene ambient magenta, fog on | Icon unaffected; scene values unchanged afterwards |
| Light probes | Scene light probe group baked bright at the stage position | Icon unaffected |
| Transparency | Sphere on cleared background | Corner pixel alpha 0, centre alpha 1, edge colour <= alpha (premultiplied) |
| Framing | Objects of very different size | Coverage of the texture within a tolerance band |
| Object light | Object prefab with its own blue light, icon next to an animated icon | Blue only in its own icon, not in the animated one, not in the scene |
| Periodic | Periodic icon with changing preset value | Texture changes after the interval, not before |
| Shared result | Two icons, same content | Same keyword, one render |
| Async race | Reassign object while the load is pending | Final image shows the second object |

Visual checks that stay manual: dev scene with presets side by side; preset studio.

---

## Risks

| Risk | Why it is likely | Mitigation |
|---|---|---|
| A new isolation leak shows up late | Built-in has many global inputs (SH ambient, probes, reflection, shadows, fog keywords); confirmed as the main cost of the previous implementation | Spike first; pixel tests per leak; every new leak gets a test first |
| Ambient override not visible per camera in Built-in | Ambient is uploaded as SH; when exactly Unity reads it is undocumented | Verify first thing in phase 1; fallback: set `RenderSettings.ambientProbe` directly |
| Edit-mode lifecycle | Domain reload, Prefab Stage, scene save, hidden objects | `HideAndDontSave` stage, release on `beforeAssemblyReload`, tests in edit mode |
| Async + pooling races | Loads finish after the icon was reused | Request tokens; result applied only if the token still matches |
| Texture content loss | Render textures lose content on device reset | `IsCreated()` check per frame for static icons |
| Animator culling | `BasedOnRenderers` may consider a manually rendered camera as not visible | Force or warn (phase 4) |
| Memory with 100 icons | Depth + MSAA per texture | Scratch target + lean storage, atlas later |
| SRP differences | Alpha, exposure, light limits | Backend interface from phase 1; pipeline-specific presets |

---

## Decisions

| Question | Decision |
|---|---|
| Where did the previous implementation lose its time? | Environment handling / isolation, as assumed in *Risks*. It gets the first slot (spike) and a pixel test per leak. |
| May objects bring their own lights? | Yes (R9). |
| Static icons re-rendering periodically? | Yes, on request (periodic mode). |
| Consistent scale across icons? | No, fit into the rect (R10). |
