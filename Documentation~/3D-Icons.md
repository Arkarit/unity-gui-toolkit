# 3D Icons

`UiIcon3D` shows a 3D object as a UI icon. A camera renders the object into a texture, and a `RawImage` shows
that texture. Every icon has its own lighting and is **isolated**: the scene's lights, ambient, fog and reflections
do not reach it, and neither do the other icons. The texture follows the icon's size. Identical icons share one
render. The object can be referenced directly or loaded by id. It can also play its own animation.

This page is the user guide. For the reasoning, the measurements and the history, see
[.Dev-App/Planning/3D-Icons.md](../.Dev-App/Planning/3D-Icons.md). For the project setup decisions, see
[BEST-PRACTICES.md](../BEST-PRACTICES.md#6-3d-icons-reserve-the-layer-and-own-the-presets-at-setup).

**Render pipeline:** built-in only for now. The pipeline specific part sits behind `IIcon3DRenderBackend`;
URP and HDRP backends are planned.

---

## `UiIcon3D` or `Ui3DObject`?

| | `UiIcon3D` | `Ui3DObject` |
|---|---|---|
| What it does | renders the object on a hidden stage into a texture | puts the mesh into the UI hierarchy itself, fitted to the rect by a vertex shader |
| Lighting | its own preset, isolated from the scene | the scene's lights and environment |
| Good for | inventories, shops, item tooltips, character portraits | a weapon in a shooter's HUD that should catch the level's light |
| Object | any prefab: several meshes, skinned meshes, particles, animation | one mesh with the `UI_3D` material |

Neither replaces the other.

---

## Setup (once per project)

1. **Reserve a layer.** Open `Gui Toolkit → Configuration`, section *3D Icons*, and press *Create layer 'Icon3D'*.
   The icon camera renders only this layer, so **no scene object may use it**. *Check scenes for objects on this
   layer* (also in `Gui Toolkit → 3D Icons`) finds offenders. Scene cameras do not need to exclude the layer,
   because icon objects only exist during their own render.
2. **Own the presets.** The library presets live in the package, and the package is read-only. Create variants of
   them in the project, and set *Default Preset* in the configuration to your variant of *Neutral*. See
   [BEST-PRACTICES §6](../BEST-PRACTICES.md#6-3d-icons-reserve-the-layer-and-own-the-presets-at-setup).
3. Optional settings in the same section:
   - *MSAA*: anti-aliasing of the render. Stored icons never carry MSAA.
   - *Renders per Frame*: how many static icons may render in one frame.

Without the layer, icons fall back to layer 31 and log a warning once.

---

## Quick start

1. Drop `Prefabs/StandardElements/StandardIcon3D` into a canvas. Better: drop your project's variant of it.
2. Set **Prefab** to the object to show.
3. Optionally set **Preset**. Empty means the configured default preset.

The icon appears in edit mode, in the Prefab Stage and in play mode. The texture size is the rect size in screen
pixels, times *Resolution Scale*. Change the object, the preset or the size, and the icon renders again. Edit a
preset or object prefab and save it, and every icon re-renders.

The `RawImage` must use the **`UI_Icon3D` material**. Icon textures carry premultiplied alpha, and the standard
UI material gives them dark fringes. The library prefab already has the material. The inspector warns if it is
missing.

From code:

```csharp
icon.Prefab = swordPrefab;               // direct reference
icon.PrefabId = "res:Items/Sword";       // or load by canonical id (used while Prefab is null)
icon.Preset = warmPreset;
icon.Mode = UiIcon3D.EMode.Animated;     // Static (default), Periodic, Animated
```

---

## Presets

A preset is a **prefab** with a `UiIcon3DPreset` on its root. It describes a lighting mood and the framing:

- **Lights** are child `Light`s. They are **camera relative**: author them as if the icon camera looked along the
  preset's +Z axis. The preset turns with the view, so a key light stays top left whatever direction the object
  is seen from.
- **Environment** comes from an optional `UiCameraRenderSettings` on the same GameObject: ambient, fog, reflection
  and quality settings. **Whatever the preset leaves open is neutral, never the scene's.** Before the preset, the
  renderer applies a baseline that matches an empty scene: black ambient, no fog, no skybox, black reflection.
- **Give every preset a reflection cubemap.** Metal shows almost nothing but its reflection. With the black
  baseline reflection, metal renders black. `Icon3DEnvironmentUtility.CreateGradientCubemap(sky, horizon, ground)`
  makes a simple studio reflection. The library presets ship one each.
- **Framing:**
  - view rotation, perspective or orthographic projection, field of view
  - fit mode:
    - *Box*: tight, the default.
    - *Sphere*: stable when the object rotates, but up to √3 looser for round objects.
  - padding
  - background colour (alpha 0 = transparent)
  - alpha mode (see *Limits and pitfalls*): how the alpha of transparent materials is made right

The library ships three presets:

| Preset | Location | Mood |
|---|---|---|
| *Neutral* | `Resources/Icon3D/` | soft studio light; used when neither the icon nor the configuration names a preset |
| *Warm* | `Prefabs/Icon3D/` | golden key light, violet rim, warm ambient |
| *Dramatic* | `Prefabs/Icon3D/` | hard top light, strong cold rim, almost no ambient |

The library assets are generated by a dev-only menu (*3D Icons → Dev: Rebuild Library Assets*). Re-running it
keeps their GUIDs.

---

## Objects

Any prefab works. The renderer copies it for every render and never modifies it. On the stage the copy:

- moves to the icon layer and is hidden and never saved
- ignores the scene's **light probes and reflection probes**; otherwise the probes around the stage would light it
- keeps its own lights, restricted to the icon layer and on only during its own render; a glowing crystal is fine
- has its audio sources, audio listeners and cameras switched off
- has its physics switched off: colliders off, rigidbodies kinematic. This needs the physics modules, picked up
  through `versionDefines`.
- shows Animator characters in their entry pose instead of the bind pose

The copy's scripts **do run** while it is on the stage (`Awake`, `OnEnable`, and `Update` in animated mode). A
script that looks for `Camera.main` or registers with game systems may misbehave. Keep the icon object free of
gameplay scripts, or let those scripts ignore objects on the icon layer.

**Framing** comes from the bounds of all renderers. Particle, trail and line renderers are left out. Where that
fails (skinned meshes whose bounds cover the whole bind pose, odd pivots, or one detail that should fill the
icon), add a **`UiIcon3DBoundsHint`** to the object. Its *Fit to Renderers* button is a starting point. It can also
set the view the object prefers, for example a sword seen from the side. The view rotation is decided in this
order: the icon's own override, then the object's hint, then the preset.

---

## Modes

| Mode | Renders | Notes |
|---|---|---|
| **Static** (default) | once, again only when something changes | the instance is destroyed after the render; only the texture stays |
| **Periodic** | every *Refresh Interval* seconds | for time dependent presets or slowly changing shaders |
| **Animated** | every frame, or every *Frame Divider*-th frame | the object's own animation plays: Animator, particles, scripts |

How animated icons behave:

- **Persistent instance.** The instance stays alive on the stage. Outside its own render it is invisible to every
  camera (`forceRenderingOff`, which covers its shadows too), and its lights are off.
- **Nothing falls asleep or wanders off.** Animators run with `AlwaysAnimate` and without root motion. Skinned
  meshes and particles keep updating although no real camera looks at them.
- **Framing is computed once,** from the entry pose. Per frame, the image would pump with the animation.
- **Off-screen icons are not rendered.** An animated icon that is off screen or culled by a `RectMask2D` keeps
  animating, but costs no render.
- **Switching to Static freezes the current frame.** No new render happens, because it would show the entry pose,
  and the instance is freed. **Switching to Animated** keeps the static image until the first animated frame.
  Resuming starts the animation from the beginning.
- **Edit mode:** Animators and particles are advanced at 30 fps, so the animation is visible without play mode.
  Scripts do not run in edit mode.

"Animate only the hovered item" is two lines; the dev project's demo has `Icon3DHoverAnimate`:

```csharp
public void OnPointerEnter( PointerEventData _ ) => icon.Mode = UiIcon3D.EMode.Animated;
public void OnPointerExit( PointerEventData _ )  => icon.Mode = UiIcon3D.EMode.Static;
```

---

## Loading objects

Instead of a direct reference, set **Prefab Id**, a canonical asset id that `AssetManager` understands, such as
`res:Items/Sword` for Resources or the Addressables provider's prefix and key. The inspector offers the usual
`CanonicalAssetRef` drawer, so you can drag and drop.

- **One load per id**, shared by every icon that shows it. The asset is released when the last icon lets go.
- **No wrong icons in reused list items.** When an icon gets a different object, its old image is dropped at once,
  so a reused list item never briefly shows the previous entry. A load that completes after the icon moved on
  never reaches it, because icons poll their current request and never take callbacks.
- **Loading Texture** is shown while the object loads. Empty means transparent. Use an opaque or premultiplied
  image, because the icon material expects premultiplied alpha.
- A failed load logs one error and leaves the icon empty. The inspector shows the state: *loading*,
  *load failed*, *pending* or *rendered*.

---

## Performance and memory

- **Identical icons render once.** Icons with the same object, preset, view and size share one render and one
  texture, so a list of 100 items with 20 distinct objects renders 20 times.
- **Stored icons are lean:** colour only, no depth buffer, no MSAA. The render itself goes into one shared
  temporary target with depth and MSAA, which is then resolved.
- **Static renders are budgeted per frame** (*Renders per Frame*, default 8). **Visible icons go first**: on
  screen and not culled by a `RectMask2D`. A list that just opened fills in from what the user is looking at.
  Animated icons are not budgeted.
- **The old image stays until the new one is ready.** A resize or preset change never flickers. Only a different
  object drops the old image immediately (see above).
- **Draw calls:** every distinct texture is its own draw call. An atlas for static icons is planned for large lists.

The scroll stress demo in the dev project (*3D Icons → Create Scroll Stress Demo*) shows 100 icons and 30 loaded
objects. Its *shuffle* option reassigns icons while their loads are pending, and an overlay shows renders, loads
and textures.

---

## Code without the component

```csharp
// Static: shared by key, rendered once
Icon3DHandle handle = UiIcon3DRenderer.RenderStatic(prefab, preset, new Vector2Int(256, 256));
rawImage.texture = handle.Texture;    // content valid once handle.IsRendered
...
handle.Release();                     // when no longer needed

// Animated: never shared; IsPlaying = false freezes, FrameDivider throttles
Icon3DHandle animated = UiIcon3DRenderer.RenderAnimated(prefab, preset, new Vector2Int(256, 256));
animated.IsVisible = isOnScreen;      // invisible animated icons are not rendered
```

- The texture lives in `RenderTextureManager` under `handle.Key`. A `UiRawImageRenderTextureConsumer` with that key
  shows it without code.
- `UiIcon3DRenderer.Flush()` renders everything pending now, regardless of the budget; useful in tests and tools.
- `UiIcon3DRenderer.Invalidate()` re-renders everything. The editor calls it on asset reimport.
- `EvBeforeRender` / `EvAfterRender` mark one renderer tick. It runs once per frame after the canvas layout, and
  on every editor update in edit mode.
- `Layer`, `MsaaSamples`, `RendersPerFrame` and `Backend` can be overridden for tests.

---

## Limits and pitfalls

- **Transparent materials on the object** (glass, alpha blended particles) would write a wrong alpha into the icon,
  because their blend mode applies to the alpha channel too (a 50% layer ends up at 25%). The preset's **Alpha Mode**
  handles it: *Auto* (default) renders such objects twice, over black and over white, and derives the alpha from
  the difference - right for any material, but twice the render cost for these icons. *Fast* always renders once
  (use it when the object's transparency does not matter), *Exact* always renders twice. It only applies to a
  transparent background; with an opaque background colour nothing is derived. Auto recognises an object by its
  materials' render queue (3000 and up), so a material that switches to a transparent queue at runtime needs *Exact*.
  **Additive** particles were always fine: they add light, which is what premultiplied alpha expects.
- **Stencil `Mask`s** are not considered for "visible first"; only `RectMask2D` and the screen bounds are. Masked
  icons count as visible.
- **Sphere fit** is conservative by design (see *Presets*). Static icons should use *Box*.
- **Never compare a texture that may have been released with Unity's `==`.** A destroyed texture compares equal
  to `null`, and the dead reference stays where it was. `UiIcon3D` detaches before it releases and compares with
  `ReferenceEquals`; code that handles icon textures itself should do the same.

---

## How isolation is verified

Isolation is measured, not judged by eye. The PlayMode tests render into textures and read the pixels back.
- **A test per rule:** a green scene light must not appear in a red preset; two presets must not influence each
  other; a scene object right in front of the stage camera must stay invisible.
- **The oracle test** (`TestIcon3DRenderer.Scene_Environment_Does_Not_Reach_The_Icon_Oracle`) covers the
  environment. Switching the active scene swaps Unity's *complete* environment, including inputs nobody thought of.
  The test renders an icon in a hostile scene with the overrides, and again with a clean scene active and no
  overrides. The two images must be identical.

That test found the leak of the scene's environment reflection on its first run. The Unity behaviour it relies on
is pinned down in `TestIcon3DEnvironmentFindings`, so a Unity update that changes it fails a test.
