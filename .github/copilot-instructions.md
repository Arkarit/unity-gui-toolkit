# Copilot Instructions

## Project Overview

**unity-gui-toolkit** is a Unity UI package (`de.phoenixgrafik.ui-toolkit`) targeting Unity 2022.3. It is a runtime/editor C# library — there are no standalone build, test, or lint commands. All testing and development happens inside the Unity Editor.

## Development Setup

To work on the package itself, set up the dev app first:

1. Run `.Dev-App/Install.bat` as administrator (creates symlinks linking `Runtime/` and `Editor/` into the Unity project's Assets folder)
2. Open `.Dev-App/Unity` in Unity Hub

On macOS/Linux use `.Dev-App/install.sh` instead.

## Architecture

### Component Hierarchy

The core inheritance chain for all UI elements is:

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

### State System (`GuiToolkit.UiStateSystem`)

`UiStateMachine` records GameObject property snapshots per named state and animates between them via `UiTransition`. States are set via `stateMachine.State = "stateName"` or `SetState("stateName", useTransition)`. Supports nested sub-state-machines and can preview transitions in the editor.

### Localization (Loca)

- `LocaManager` manages the active `ILocaProvider`.
- Components needing localization override `NeedsLanguageChangeCallback` → `true` and implement `OnLanguageChanged(string languageId)`.
- `LocaExcelBridge` provides tooling to sync localization keys with Excel.

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
