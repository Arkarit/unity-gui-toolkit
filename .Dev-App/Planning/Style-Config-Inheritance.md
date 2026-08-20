# Style Config Inheritance

## Overview

This document assesses making `UiStyleConfig` **inheritable**: a project's config declares the package config as its parent and only stores what it overrides, everything else resolves through the parent. It is intended as a planning reference, not a complete specification.

The goal is to remove a class of problem rather than a task: today a project's style config is a **full one-time copy**, so styles added to the library never reach the project, and the copy drifts silently from the original.

---

## Problem

`clone_style_config` copies the package config into the project once. There is no merge, sync or diff afterwards. Consequences:

- Every new library component needs its styles authored **twice**, once per config.
- A style fix or addition in the library **never arrives** in a consuming project.
- The copy diverges over time and nobody can say by how much.
- The divergence is invisible in review, because a real change is buried among the copies.

Measured on the current assets:

| | Package config | botw-client clone |
|---|---|---|
| Style instances | 1,776 | 2,100 |
| File size | 524 KB | 609 KB |

So roughly **85% of the project config is a duplicate** of the package config; about 324 style instances are genuinely the project's own.

A second consumer exists: `notr-game-client` uses the package (currently pinned one tag behind) but has **not** taken ownership of a style config yet. The cost of the status quo multiplies with each project that does.

---

## Feasibility

Favourable, because of how a style is resolved. `UiAbstractApplyStyleBase` stores **no object reference** to its style — only a name and a key:

```csharp
[SerializeField][HideInInspector] private string m_name;
public abstract int Key { get; }            // hash of component type + style name

public UiAbstractStyleBase FindStyle()
{
    var styleConfig = StyleConfig;
    UiSkin currentSkin = SkinIsFixed ? styleConfig.GetSkinByName(m_fixedSkinName)
                                     : styleConfig.CurrentSkin;
    return currentSkin.StyleByKey(Key);
}
```

Resolution runs through **one choke point**, `UiSkin.StyleByKey(int)`, which returns `null` when the key is unknown. That is where the parent fallback belongs. No serialized data has to be migrated for the lookup to change.

Two constraints that shape the design:

- **Styles are `[SerializeReference]` objects inline in the ScriptableObject**, not sub-assets. A child config therefore cannot "reference" individual parent styles; it can only ask the parent to resolve a key.
- **`UiSkin` carries a back-reference `m_config`** and `CurrentSkin` is index-based. The fallback must match skins **by name**, not by index — two configs are not guaranteed to list their skins in the same order.
- **Every skin holds the full style set today, and two places rely on it.** `UiStyleConfig` reads the style *vocabulary* off `m_skins[0]` alone (`GetStyleNamesByMonoBehaviourType`, `StyleExists`), and `UiStyleManager.SetSkin` pairs the outgoing and incoming skin **by index** with `Debug.Assert(previousStylesCount == stylesCount)` before tweening. Once a child config stores only overrides, that invariant is gone: a skin switch with a tween duration would bail out on mismatched counts, and inherited styles — the majority — would never be tweened at all, because `UpdateTween` iterates `CurrentSkin.Styles`. The tween path is play-mode only (`_tweenDuration <= 0 || !Application.isPlaying` takes the immediate path), so this half breaks at runtime while the vocabulary half breaks in the editor. Both need to run against the **effective** style set (own plus inherited), and the tween pairing has to become key-based instead of index-based. This is the only runtime path outside the choke point that inheritance actually breaks.

---

## Two Levels of Inheritance

### Level A — style-level fallback (recommended)

A child config stores only the styles it overrides. On a lookup miss, the request is forwarded to the parent's skin of the same name.

- No change to existing semantics.
- Fully backwards compatible: a config without a parent behaves exactly as today.
- Solves the stated problem completely — a new library style is immediately present in every consuming project.

### Level B — value-level inheritance (not recommended for now)

Each individual value could inherit instead of only being on or off. This changes the meaning of `IsApplicable`, which is the central concept of the styling system: two states would become three. It touches the 32 generated `UiStyleX` / `UiApplyStyleX` pairs, the drawers, and every existing config.

The benefit over Level A is real but narrow: it would allow a project to override a single colour of a style without copying the whole style. Worth revisiting **after** Level A has run in production for a while.

---

## Phases

### Phase 1 — Resolution  ✅ done

Implemented with 17 tests. `m_parent` plus a name-matched fallback in `UiSkin.StyleByKey`, cycle and
depth guard (`MaxInheritanceDepth`, self included), `EffectiveStyles` per skin, the vocabulary renamed
to `EffectiveStyleNames`/`EffectiveStyleAliases` and routed through it, and the skin tween paired by
key via the pure `UiStyleManager.PairStylesByKey`. Resolution cost is unchanged (`FindStyle` 0.9 us).

- Add `[SerializeField] UiStyleConfig m_parent` to `UiStyleConfig`.
- Resolve a lookup miss through the parent, matching skins **by name**.
- Guard against cycles and limit chain depth.
- Applies to `UiAspectRatioDependentStyleConfig` as well, since it derives from the same base.
- Expose an **effective style set** per skin (own plus inherited, own wins) and route the vocabulary lookups and `UiStyleManager` skin tweening through it; pair skins by key, not by index.

### Phase 2 — Copy-on-write  ✅ done (except the drawer, which phase 3 creates)

Writing to an inherited style must materialise it in the child config first, then write. Without this the write silently disappears — see Risk Summary.

Implemented with 8 tests: `UiStyleUtility.CloneStyle` (an independent copy carrying values and applicableness, asset references shared rather than duplicated), `UiSkin.MaterializeStyle` / `OwnsStyle` as the single copy-on-write entry point, `UiStyleConfig.IsPackageOwned` as the refusal (verified against the dev app's symlinked package copy, where the `Packages/` test alone says nothing), and `UiAbstractApplyStyleBase.MaterializeStyleForOverride` — public, because phase 3's override action wants exactly that. `Record()` goes through it. An override is per skin: materialising in one skin leaves the others inheriting.

`UiStyleWriter` now reads the effective set (reporting `inherited` per style) and materialises before writing, unless it is a dry run.

Still open: the skin drawer's value edits. An inherited style is not part of the child's serialized data, so the drawer cannot show or edit one yet — that path comes into existence with phase 3 and is wired there.

### Phase 3 — Editor

- Inherited styles listed read-only, visually distinct from own ones.
- **Override** on an inherited entry (materialise into the child), **Revert to inherited** on an own entry (delete from the child).
- The parent field surfaced in `UiStyleConfigEditor` and in the configuration window.

### Phase 4 — Conversion tool

See below.

### Phase 5 — Verification and documentation

- Dev-App scene exercising: inherited style, overridden style, project-only style, skin present in parent but not in child, and the reverse.
- `BEST-PRACTICES.md` §2 rewritten: cloning is no longer the recommended path, inheriting is.
- `CHANGELOG.md`, and a note in the AI documentation that a style may now live in the parent.

---

## Conversion Tool

The tool that makes the switch safe for an existing clone.

- **Diff** child against parent, style by style and value by value.
- **Report first**: what is identical (can be inherited), what differs (stays an override), what exists only in the child (stays regardless).
- **Dry run by default**, applying only on request.
- **Not all-or-nothing**: an identical style can deliberately be *pinned* as an override where following the parent is unwanted.

The report is useful on its own, before any conversion: it answers a question nobody can answer today — **how far has this clone actually drifted?**

---

## Key Decisions (resolve before starting Phase 1)

1. ~~**Does a child skin inherit styles from a parent skin of the same name only?**~~ **Answered by the projects rather than by this plan.** Name matching alone is not enough: the client config has the skins Default and BOTW, the package config Default and Light - so BOTW would have found no counterpart and inherited nothing, leaving half the config a full copy. A skin therefore carries an optional `m_inheritFromSkinName` (empty = same name), offered in the editor as a popup over the parent's skins. Each level of a chain maps on its own, which is why resolution walks skin by skin rather than config by config.
2. **Chain depth.** One level (project → package) covers every known case. Allowing longer chains costs nothing in the lookup but widens the failure surface.
3. **Conversion policy for the existing clone**: convert everything identical to inherited, or pin selected areas? This is a look-and-feel decision, not a technical one.
4. ~~**The unexplained skin identity issue.**~~ **Resolved — see "Skin identity" below.** No longer blocks Phase 1.

---

## Skin Identity (former FIXME, resolved)

`UiStyleConfig.OnSetSkinAlias` used to carry `//FIXME: The _skin instance is different than the skins in style config - why??!`. The cause is `SerializedProperty.boxedValue`:

- `UiSkin` is a plain `[Serializable]` class inside `List<UiSkin> m_skins`, so the element's property type is **Generic** — and for Generic properties `boxedValue` builds a **fresh managed copy on every single access**. `UiSkinDrawer.OnEnable` read exactly that, and `OnEnable` runs from both `OnGUI` and `GetPropertyHeight`, so the drawer never once held the skin that lives in the config.
- Styles behave differently because `m_styles` is `[SerializeReference]`: those elements are **ManagedReference** properties, and `boxedValue` returns the **real** instance. That is why `OnSetStyleAlias`, `DeleteStyle`, HSV and Paste all work on the actual objects and never needed a workaround, while the skin path did.
- The copy's `m_config` is a UnityEngine.Object reference and survives the copy, which is why `skin.StyleConfig` still points at the real asset and the `_styleConfig != this` guard passes.

Verified empirically in both editors (2022.3.62f2 and 6000.0.64f1) across five configs: the boxed skin is never reference-equal to `Skins[i]`, a second access yields yet another instance, the style list is a new `List` holding the **same** style instances — and writing `Alias` on the copy leaves the asset untouched and not even dirty.

Fixed by having `UiSkinDrawer` resolve the real skin by array index (`SerializedProperty.GetArrayIndex()`) instead of taking the copy. The by-name matching in `OnSetSkinAlias` stays: skin names are unique per config, and a caller may still legitimately pass a detached copy.

**Two consequences for this plan**, both good to know before Phase 2:

- Inherited styles resolve to instances **owned by the parent asset**, and being `[SerializeReference]` they come back as real, writable objects. An editor that let the user type into an inherited style would silently mutate the *package* config in memory — and `SkipSavingInPackageFolder` would then discard the save without a word. Phase 3's read-only display of inherited entries is therefore a **guard against data loss**, not cosmetics.
- The trap is generic to `AbstractPropertyDrawer<T>` whenever `T` is a plain serializable class. `UiSkinDrawer` was the only site that wrote through it (`UiSoundDefDrawer` only reads for preview, `UiAbstractStyleBaseDrawer`'s `T` is a SerializeReference type); the base class now documents it.

---

## Effort Estimate (single developer)

| Item | PD |
|---|---|
| Phase 1 — resolution, name-based skin matching, cycle guard | 0.5 |
| Phase 1b — effective style set: vocabulary lookups and key-based skin tweening | 0.4 |
| Phase 2 — copy-on-write on all write paths | 0.5 |
| Phase 3 — editor: inherited vs. own, override / revert | 1.0 |
| Phase 4 — conversion tool with report and dry run | 0.8 |
| Phase 5 — tests, Dev-App verification, documentation | 0.7 |
| **Total (Level A)** | **~3.9** |

Level B would add an estimated **3 to 5 PD** on top, and is not part of this plan.

---

## Risk Summary

**Library updates can change a project's look.** Today a clone is frozen; a restyle in the library cannot reach the project. With inheritance it can — which is the point, but it must be a deliberate decision rather than a surprise after a package bump. Mitigation: overrides stay pinned, only unset styles follow the parent, and the conversion tool shows exactly which styles would become inherited.

**Silent write loss.** `SkipSavingInPackageFolder` drops any save whose path starts with `Packages`, without an error. A write to an inherited style that is not materialised first therefore *appears* to succeed and is gone on the next reload. This footgun exists today but is rare; inheritance would make it the normal case. Phase 2 exists solely to remove it.

**Low risks.** Lookup cost is one additional dictionary miss, and appliers cache their resolved style. Merge conflicts become *less* likely, because a converted child config is a fraction of its current size.
