# AGENTS.md

This file provides guidance to Codex when working with code in this repository. It is the Codex-facing
copy of [CLAUDE.md](CLAUDE.md); keep the two in sync.

## Project Overview

**unity-gui-toolkit** is a Unity UI package (`de.phoenixgrafik.ui-toolkit`) targeting Unity 2022.3 LTS. It is a runtime/editor C# library — there are no standalone build, test, or lint commands. All testing and development happens inside the Unity Editor.

**Before advising anyone on project setup, read [BEST-PRACTICES.md](BEST-PRACTICES.md).** It holds the
decisions that are cheap on day one and expensive to retrofit — above all: create variants of **all**
library prefabs in one bulk run at setup, not one at a time when a need arises. The registry ranks
client prefabs above library ones, so once the variants exist nothing has to be rewired; retrofit that
later and you are doing the same work against a project full of references that already resolved
elsewhere. If a user asks how to change one library prefab, the answer starts with that bulk step and
the folder to put it in, not with a single variant.

**When a screen needs an asset that does not exist, you can have one made.** The agent-tools installer
puts `tools/invoke-codex.ps1` and `tools/codex-asset-result.schema.json` into a consuming project; a
brief plus that schema returns a written file (SVG authored as text, rasterised with ImageMagick) and a
typed answer naming its path. See **[mcp~/README.md](mcp~/README.md) - "Assets that do not exist yet"**
for the invocation, the sandbox traps, and the Unity-side import step. Say so before substituting the
nearest existing sprite or reporting the gap and stopping: those tools announce themselves only through
their file names, so nobody finds them by accident.

## Development Setup

1. Run `.Dev-App/Install.bat` **as a normal user** (do not run as administrator — the script requests elevation only when needed; running as admin causes the gh-pages docs repo to be created with incorrect ownership)
2. Open `.Dev-App/Unity` in Unity Hub (symlinks `Runtime/` and `Editor/` into the Unity project's Assets folder)

On macOS/Linux use `.Dev-App/install.sh` instead.

## Architecture

### Component Hierarchy

```
MonoBehaviour
  └─ LocaMonoBehaviour        (localization helpers)
       └─ UiThing              (base: lifecycle, events, RectTransform)
            └─ UiPanel         (show/hide lifecycle, animations, pooling)
                 └─ UiView     (top-level container: Canvas, layers, navigation)
```

- **`UiThing`** — base class for all UI components. Requires `RectTransform`. Manages the `AddEventListeners`/`RemoveEventListeners` lifecycle and an opt-in mechanism to receive events while disabled (`ReceiveEventsWhenDisabled`).
- **`UiPanel`** — adds Show/Hide API with optional `IShowHidePanelAnimation`, visibility events (`EvOnBeginShow`, `EvOnEndShow`, etc.), and pool/destroy-on-hide behavior.
- **`UiView`** — adds a `Canvas`/`CanvasScaler`/`GraphicRaycaster`, layer ordering (`EUiLayerDefinition`), fullscreen occlusion events, and navigation stack integration.

### UiMain (Singleton)

`UiMain` is the central singleton MonoBehaviour (lives on a persistent GameObject that also has a `Camera` and `UiPool`). It manages:
- **View lifecycle**: `CreateView<T>()`, `CreateAndShowView<T>()`, `LoadScene()`, `UnloadScene()`
- **Navigation stack**: `NavigationPush()`, `NavigationPop()`
- **Built-in dialogs**: `OkRequester`, `YesNoRequester`, `ShowToastMessageView`, `ShowSettingsDialog`, etc.
- **Layer sorting**: Sorts child `UiView`s by `EUiLayerDefinition` and plane distance every frame.

Always check `UiMain.IsAwake` before accessing `UiMain.Instance`. Use `UiMain.AfterAwake(action)` to defer initialization code until UiMain is ready.

### Styling System (`GuiToolkit.Style`)

- **`UiStyleConfig`** — ScriptableObject asset; root of the skin/style data tree.
- **`UiSkin`** — a named set of `UiAbstractStyleBase` entries. Skins can be aspect-ratio-dependent (`UiAspectRatioDependentStyleConfig`).
- **`UiStyleManager.SetSkin(name, tweenDuration)`** — switches the active skin at runtime, optionally tweening between values.
- **`UiAbstractApplyStyle`** / **`UiAbstractApplyStyleBase`** — components on GameObjects that subscribe to skin changes and apply style values to their target component (e.g., color, font size).

A style is identified by its name **and** the component type it targets, not by name alone:
`Buttons/Standard/Background` exists five times over — as an `Image`, a `UiGradientSimple`, a
`UiDistort`, a `Shadow` and a `RectTransform` — because those are five aspects of one button's
background. Any lookup that ignores the type is picking one of five at random.

Each style holds one `ApplicableValue<T>` per serialized field of its target component, exposed as a
public property named after the field (`Color`, `FontSize`, `Font`, `Radius`, …). `IsApplicable`
decides whether the style has an opinion at all; a value that is written but not applicable changes
nothing on screen, which is the most confusing possible outcome and therefore never the default.

**A project must not theme the config that ships in the package.** It lives in the immutable package
copy, so the edit is refused or silently lost at the next version bump. Clone it into the project first
— `Gui Toolkit → Configuration → Clone`, or `clone_style_config` over the MCP bridge. Both repair the
skins' and styles' back-references to their config, which `Instantiate` copies verbatim from the
original and which nothing else fixes afterwards.

### State System (`GuiToolkit.UiStateSystem`)

`UiStateMachine` records GameObject property snapshots per named state and animates between them via `UiTransition`. States are set via `stateMachine.State = "stateName"` or `SetState("stateName", useTransition)`. Supports nested sub-state-machines and can preview transitions in the editor.

### Localization (Loca)

- `LocaManager` manages the active `ILocaProvider`.
- Components needing localization override `NeedsLanguageChangeCallback` → `true` and implement `OnLanguageChanged(string languageId)`.
- `LocaExcelBridge` provides tooling to sync localization keys with Excel. Google Sheets sync is also supported.

### Pooling

`UiPool` (accessed via `UiPool.Instance` or `UiMain.Instance.UiPool`) manages reusable prefab instances. Use the `PoolInstantiate()` extension method on a component reference. Views created through `UiMain.CreateView<T>()` automatically use the pool. Implement `IPoolable` (`OnPoolCreated` / `OnPoolReleased`) to reset state on lease/return.

### Bootstrap

`Bootstrap` is a static class initialized via `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`. It initializes `UiToolkitConfiguration`, storage routing, and `PlayerSettings`. In the editor it also re-initializes on entering Edit Mode.

### Assembly Definitions

| Assembly | Namespace | Notes |
|---|---|---|
| `de.phoenixgrafik.ui-toolkit` | `GuiToolkit` | Runtime; `allowUnsafeCode: true` |
| `de.phoenixgrafik.ui-toolkit.Editor` | `GuiToolkit.Editor` | Editor-only; references TMPro and Roslyn |

## Key Conventions

### Naming

- **Parameters**: prefixed with `_` (e.g., `_instant`, `_newStateName`)
- **Private instance fields**: prefixed with `m_` (e.g., `m_canvas`, `m_isAwake`)
- **Private static fields**: prefixed with `s_` (e.g., `s_instance`, `s_layer`)
- **Component classes**: prefixed with `Ui` (e.g., `UiButton`, `UiView`, `UiPanel`)
- **Abstract base classes**: contain `Abstract` in the name (e.g., `UiAbstractStyle`)
- **Inspector-assigned prefab fields**: named with a `Prefab` suffix
- **Editor-only code**: wrapped in `#if UNITY_EDITOR`

### Lifecycle Rules

- **Always call `base`** when overriding `Awake`, `OnEnable`, `OnDisable`, `OnDestroy` in any `UiThing` subclass.
- Register `UiButton` click listeners via `AddOnEnableButtonListeners(...)` **before** calling `base.Awake()`.
- Override `AddEventListeners()` / `RemoveEventListeners()` (not `OnEnable`/`OnDisable` directly) to subscribe/unsubscribe from bus events. The base class controls when these are called based on `ReceiveEventsWhenDisabled`.
- Use `ExecuteFrameDelayed(action)` or `ExecuteTimeDelayed(action, seconds)` (from `UiThing`) instead of raw coroutines for deferred calls.

### Global Events

Use `UiEventDefinitions` for cross-component communication (e.g., `EvLanguageChanged`, `EvScreenResolutionChange`, `EvFullScreenView`, `EvSkinChanged`). Prefer `InvokeAlways` when the event must fire regardless of listener count.

### Views and Dialogs

- Instantiate views via `UiMain.Instance.CreateView<T>(prefab)` — this handles pooling and parenting.
- Use `ShowTopmost()` when the view should appear above siblings in its layer.
- Views with `m_autoDestroyOnHide = true` and `m_poolable = true` return to the pool automatically after `Hide()` completes.
- The `EvOnDestroyed` event fires before pool-return or destruction; remove listeners in its callback to avoid leaks.

## Editor code and secondary Unity processes

`[InitializeOnLoad]` does not only run in the editor a human is looking at. Unity spawns **asset
import workers** — full editor processes (`-batchMode -name AssetImportWorker0 …`) that load the same
editor assemblies, run the same static constructors, and can read `EditorPrefs`. They have no window
and no editor loop serving `EditorApplication.update`.

Any editor feature that claims a machine-wide resource must therefore check first. `UiScreenMcpBridge`
does, via its `IsSecondaryProcess` guard — without it every import worker started its own HTTP
listener and overwrote the project's discovery file and the machine-wide registry entry with its own
port and pid. The MCP proxy then connected to a worker: the port accepted, nothing ever answered, and
every request died in the handler timeout while the real editor sat there perfectly healthy.

That one cause produced a whole family of misleading symptoms — "the bridge dies on a domain reload"
(a reload spawns workers), "the port is open but nothing answers", "it comes back when a human clicks
into the window" (the real editor re-announces and takes the file back). If a bridge ever seems dead
again, **check the pid in `Library/UiToolkit/mcp-bridge.json` against the editor's own pid first**;
`AnnouncementIsOurs()` exists for exactly that reason and the Start menu item doubles as the repair.

**Do not schedule editor work with `EditorApplication.delayCall` if anything but a human's next click
depends on it.** It promises "some later tick" and in a background editor that tick may never arrive:
measured here, a delayCall scheduled in an unfocused editor had still not run after 20 seconds while
`EditorApplication.update` was ticking the whole time. That is how `Bootstrap` came to report the toolkit
uninitialised to everything until someone focused the window. Use a one-shot on
`EditorApplication.update` (fires regardless of focus, unsubscribe on the first tick), or
`AssemblyReloadEvents.afterAssemblyReload` when the trigger really is the reload. delayCall is fine for
Inspector code, where a human is by definition present and focused.

An earlier version of this file described a `WakeEditor()` nudge for unfocused editors and told you to
copy it to other platforms. There is no such code, and there should not be: it was written for the
theory that an idle unfocused editor stops ticking, and that theory was wrong. Measured after the real
fix, with the editor idle and `hasFocus:false`, `ping` answered in 82 ms. The `EditorApplication.update`
path keeps ticking; if the bridge ever seems not to, look for a secondary process holding the
announcement, not for a sleeping editor.
