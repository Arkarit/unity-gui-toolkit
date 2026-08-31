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

Create variants of **all** library prefabs in one go, at project setup — and keep the inheritance
between them, which is the part that is easy to lose.

Of the library's 66 prefabs, **22 are themselves variants**: `OkButton`, `CancelButton`, `CloseButton`
and `StandardButtonSmall` are all variants of `StandardButton`, `FullScreenSettingsDialog` is one of
`FullScreenTabDialog`. One variant per prefab, each hanging off its own original, gives the project
ownership but flattens that shape — the copies end up related to the package rather than to each other,
and a frame added to the project's `StandardButton` never reaches the project's `OkButton`.

**The tool that does it properly:** `mirror_variant_graph` over the MCP bridge. It creates the roots as
variants of the library prefab and each dependent as a variant of the *project copy* of its base,
transplanting the library variant's own overrides. It dry-runs by default, and afterwards verifies each
rebuilt dependent against its library original property for property.

**By hand,** if you have no bridge: select the prefab folders under the package, right-click →
**`Create Variant`** → *Select common Path*. That gives you the flat version — every copy owned by the
project, but the inheritance between them lost. Fine as a starting point, and `mirror_variant_graph`
with `replaceExisting: "dependents"` upgrades it later without touching roots you have edited.

Either way, finish with:

1. Point `Gui Toolkit → Configuration → Prefab Variants Path` at that folder.
2. Regenerate the screen-authoring catalog (`Gui Toolkit → AI → Regenerate Catalog`, or the
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

## 2. Inherit the style config, do not clone it

A fresh project's `UiToolkitConfiguration` points at the style config **inside the package** — which
is why a fresh project looks like the library's default skin rather than like nothing. Editing it has
the same problem as editing a package prefab: the write is refused, or silently lost at the next
version bump.

`Gui Toolkit → Configuration`, next to the style config field, offers two ways out. They are not
equally good.

**Inherit** creates a project config that *builds on* the package's: the same skins, no styles of its
own. Only what you override is stored in your project; everything else keeps following the library as
it changes. In the config inspector an inherited style is listed read-only and tinted blue, **Overr.**
copies it into your config so it can be changed (yellow), **Revert** drops the copy again. An override
is per skin.

**Clone** creates a full copy. It stops following the library the moment it is made — and that is not
the real cost. The real cost is that nothing says afterwards which of its 140 values were ever decided
on purpose, so nobody dares touch any of them.

Both repoint the configuration and repair the copy's internal back-references (every skin and every
style holds a reference to its config, and a plain duplicate leaves all of them naming the original —
128 of them in the default config). `clone_style_config` over the MCP bridge does the cloning variant.

**Already cloned?** `Gui Toolkit → Style Config Drift Report...` compares two configs style by style
and value by value, and writes nothing. It answers what nobody can answer by looking: how much of the
clone carries no information. Measured on one real client, **61 of 80** styles in its main skin were
copies of the package's, and its three "differences" turned out to be `IsApplicable` flags rather than
decisions — the package had moved on without it. The same window converts, listing every droppable
copy by name; any single one can be kept as a **pinned** override, and no style is ever removed unless
something else still provides it.

**Skins are matched by name**, so a project skin named something the parent does not have inherits
nothing until it is mapped. The skin's `Inherits skin from` says so and offers the candidates —
including the **other skins of the same config**, because a variant skin is usually a variant of the
skin next to it rather than of anything in the library.

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

---

## 5. Authoring a screen: reference the elements, do not rebuild them

A project ships a palette — headlines, panel backgrounds, buttons, tabs, close buttons, button bars —
and every one of them is already wired, already styled, and already following the skin. An authored
screen either references those or it does not, and the difference does not show up in a screenshot.

Measured in a consuming project: of five dialog parts authored through the MCP, exactly one referenced
an existing element (`variantOf: StandardPanelBackground`, **26** lines of description). The other four
were rebuilt from raw `Image` / `TextMeshProUGUI` / `UiTab` nodes — 129, 180, 207 and 337 lines — and
between them they carried **14 literal hex colours and not one `style`**. The one that inherited
carried neither, because it had nothing to carry. That dialog now sits outside the skin: change the
palette and it keeps its own colours.

So:

- **Reference first.** `"template": "<element>"` for a child, `"variantOf": "<element>"` for the root.
- **A variant may add what the original lacks.** If an element is close but missing a part, that is not
  a reason to start over — a variant inherits everything and adds children on top.
- **Before concluding an element does not fit,** read its palette entry's `parts`. It lists every child
  path an `"overrides"` entry may be keyed by, so the parts you want to adjust are addressable.
  `capture_prefab_values` on the element's own prefab gives the same paths with all their values.
- `read_screen` deliberately stops at a template's boundary. It answering "one node" does **not** mean
  the element is a single node.

### Styles, not literal values

A `style` is the whole reason a skin change reaches a screen. A literal colour on an `Image`, or a font
size on a TMP node, is a value nobody can find again and nothing will ever update — and it is invisible
in review, because it looks exactly like a styled one. Give text and panel nodes a style from the
project's vocabulary (`list_styles`); if the right style does not exist, add it to the style config
rather than typing the value into the screen. The same applies to a hand-built element: it is not
"a copy that looks the same", it is a copy that has left the styling system.

### Static labels: the key belongs in the prefab

Text that never changes at runtime is authored, not assigned. Give the node a
`UiLocalizedTextMeshProUGUI` and a `@loca:` key (`@text:` for a literal that is deliberately not
translated), and the prefab carries the key where the loca tooling can see it. A key handed in from
code — `SetText(label, "All")` — is invisible to every harvester and every review; it looks correct in
the authoring language and shows the raw key in the first foreign build. The baker warns about a
`@loca:` key that is not in the catalog, which is the other half of the same guarantee.

If the project also runs a second localization system next to the toolkit's, decide which one owns UI
strings before authoring against either. Two catalogs with an undrawn border is how a string ends up
declared in one and looked up in the other — and both sides fail the same quiet way, because each
returns the key when it cannot resolve it. In the authoring language that looks entirely correct.

**Bridge rather than migrate.** `LocaManager.RegisterProvider` takes an `ILocaProvider`: a
ScriptableObject that hands the manager a `ProcessedLoca` and is reloaded on every language change.
That makes the toolkit the API in front of an existing catalog without moving a single string. For
Unity's Localization package the toolkit ships one — `UnityLocalizationLocaProvider`, in its own
assembly behind a version define, so a project without that package compiles as if the file did not
exist. Create the asset under `Assets/Resources/LocaJson/` and run the loca processor once; the asset
does not register itself, and one outside `Resources` cannot be loaded at all.

Its `Contribute Keys To Pot` option is worth understanding rather than accepting: with it on, the
foreign catalog's KEYS (not its translations) are registered with the loca processor, which is what
lets the toolkit's own edit-time checks — the screen baker's warning about unresolved `@loca:` keys,
above all — know that those keys exist. With it off, every one of them is reported as missing, and an
author quickly learns to ignore a warning that is worth reading.

### Two component facts that are easy to get wrong

**Disabling a `UiButton` is `EnabledInHierarchy`, not `Button.interactable`.** The toolkit has its own
notion of "disabled" and it does more than block clicks: `EnableableInHierarchyUtility` passes the
state down to every child that opts in — `UiImage` with a disabled material, the `UiShapeImage`
family, `UiTextContainerDisableable` — so the whole element greys out. Reaching past it to
`button.Button.interactable` gives you a button that does not react and looks completely normal, and
the value does not even hold: `UiButton.OnEnabledInHierarchyChanged` writes `interactable` itself, so
the next hierarchy change overwrites it.

```csharp
uiButton.EnabledInHierarchy = false;   // not: uiButton.Button.interactable = false
```

**`UiView.autoDestroyOnHide` defaults to `true`.** Correct for a screen entered once, wrong for a
dialog: `Hide()` then destroys or pools the view, and it takes the view's own subscriptions with it —
so whatever was supposed to reopen it has nothing left to talk to. A dialog you open repeatedly turns
this off on its prefab.
