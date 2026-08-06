# Best Practices

How this toolkit is meant to be used in a project. Short, and mostly about three decisions that are
cheap on day one and expensive to retrofit — because in every one of them the default is *the
library's* copy, and a project that never takes ownership discovers this at the worst moment: when it
wants to change something.

The rule behind all three: **the package is read-only.** It is delivered per version, replaced
wholesale on the next bump, and anything you change inside it is either refused or silently thrown
away. So the project needs its own copy of everything it intends to modify.

---

## 1. Take ownership of the prefabs — all of them, once, at the start

The toolkit's standard elements (buttons, panels, dialogs, settings rows, …) ship as prefabs inside
the package. Sooner or later a project needs a structural change to one of them — a frame object on
the button, an extra label, a different child order. That is not a value you can theme; it is an
object you have to add, and you cannot add it to a package prefab.

**Do not create variants one at a time, as each need arises.** That produces a project where some
elements are the library's and some are yours, and where "why does this button ignore my change" has
a different answer depending on which button you point at.

Create variants of **all** library prefabs in one go, at project setup:

1. Select the prefab folder(s) under `Packages/de.phoenixgrafik.ui-toolkit/Runtime/Prefabs/`
   (multi-select works, and folders are expanded for you).
2. Right-click → **`Create Variant`**, and pick a placement:
   - **Select common Path** — everything into one folder you choose. The usual choice.
   - **Mirror Package Hierarchy** — mirrored under `Assets/PackageVariants/`, if you prefer the
     package's own layout.
   - **Select each Path** / **Flat in Assets** — for one-offs, not for a bulk run.
3. Point `Gui Toolkit → Configuration → Prefab Variants Path` at that folder.
4. Regenerate the screen-authoring catalog (`Gui Toolkit → AI → Regenerate Catalog`, or the
   `regenerate_catalog` MCP tool).

### Why this is worth doing before you need it

**Nothing has to be rewired afterwards.** A variant inherits the base prefab's `UiStandardElement`
marker, and the registry ranks **client prefabs above library prefabs** for the same key. So once the
variants exist and the catalog is regenerated, every existing reference resolves to *your* variant:
`"template": "StandardButton"` in a screen description, the palette, the standard-element lookups.
No screen JSON changes, no call sites change. The day you add that frame object, every button in the
project has it.

Retrofitting later means doing the same thing while the project is full of references that already
resolved to something else.

### Two traps worth knowing before the bulk run

- **Do not run it twice into different folders.** Variant creation uses a unique asset path, so a
  second run produces `StandardButton Variant 1.prefab` — and then *two* client prefabs claim the same
  standard-element key. The catalog reports that as an ambiguity, picks the alphabetically first, and
  logs an error. `setup_status` and the catalog's `standardElementAmbiguities` are where you see it;
  the fix is to delete the duplicate.
- **A prefab named explicitly is honoured explicitly.** The registry re-resolution applies to
  standard-element *identities* (`"template": "StandardButton"`). A field or prop that names a
  specific prefab path is taken at face value and is *not* upgraded to a variant — deliberately, since
  several client prefabs can inherit the same key (an OK and a Cancel button are both
  `StandardButton`), and re-resolving would silently swap the one you named for the key's winner.
  So after a bulk run, check inspector fields and props that point at package prefab paths.

---

## 2. Clone the style config before theming

A fresh project's `UiToolkitConfiguration` points at the style config **inside the package** — which
is why a fresh project looks like the library's default skin rather than like nothing. Editing it has
the same problem as editing a package prefab.

`Gui Toolkit → Configuration`, next to the style config field: **Clone**. Or `clone_style_config` over
the MCP bridge. Both copy it into the project, repoint the configuration, and repair the copy's
internal back-references (every skin and every style holds a reference to its config, and a plain
duplicate leaves all of them naming the original — 128 of them in the default config).

The write path refuses a package-owned config, so this cannot be got wrong by accident.

---

## 3. `IsApplicable` is the whole story of the styling system

Every value in a style carries an on/off flag, and it decides who wins:

- **On** — the style wins, always. Whatever the component or prefab carries is overwritten when the
  style is applied.
- **Off** — the style has no opinion about that value, and the component decides.

Almost every surprise in the styling system is one of these two:

> *"I changed the colour in the style and nothing happened."* — the flag was off.
>
> *"I set it on the prefab and it keeps springing back."* — the flag was on. Either change it in the
> style, or switch the flag off there and then work on the prefab.

A style is identified by its name **and** the component type it targets, not by name alone.
`Buttons/Standard/Background` exists five times over — as an `Image`, a `UiGradientSimple`, a
`UiDistort`, a `Shadow` and a `RectTransform` — because those are five aspects of one button's
background. A lookup that ignores the type is picking one of five at random.

Style *values* apply live, in Edit Mode too, through the `ExecuteAlways` appliers: no re-bake, no
reimport. The side effect is that changing a skin marks any open scene dirty, because the appliers
write the new values into its instances. That dirt is not worth keeping.

### Your own components can join the styling system

A style is data; the component that reads it is the *applier* (`UiApplyStyleImage`,
`UiApplyStyleTMP_Text`, …), and those pairs are **generated** — not only for the toolkit's own types.

`Gui Toolkit → Styles → 'Ui Apply Style' Generator…` writes a `UiStyleX` + `UiApplyStyleX` pair for
any component type you point it at, including your project's own MonoBehaviours, into the project's
`Generated Assets Dir` (set in the configuration). After that your widget's properties are skinnable
like everything else: same skins, same switching, same tweening.

Worth knowing early, because the alternative is invisible: without it, a project's own components are
the one part of the UI a skin change does not reach, and that is usually discovered late — when the
second skin exists and half the screen fails to follow it.

---

## 4. Editor code: do not schedule real work on `delayCall`

`EditorApplication.delayCall` promises "some later tick", and in a background editor that tick may
never arrive — measured here: a delayCall scheduled in an unfocused editor had still not run after 20
seconds, while `EditorApplication.update` ticked throughout. Anything gated behind it then waits for
a human to click into the window, which is indistinguishable from a hang and impossible to debug from
outside.

Use a one-shot on `EditorApplication.update` (fires regardless of focus; unsubscribe on the first
tick), or `AssemblyReloadEvents.afterAssemblyReload` when the trigger really is the reload.
`delayCall` is fine for Inspector code, where a human is by definition present and focused.

The same applies to any wait loop: keep checking while unfocused, and if you have a frame budget,
spend it only while focused.
