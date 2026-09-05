# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Added
- **Style config inheritance** — a `UiStyleConfig` can name a **parent** it builds on, so a project stores
  only what it actually overrides instead of a full copy of everything. Until now the only way to theme a
  project was to clone the config that ships with the package, which stops following the library at the
  moment it is made; worse, nothing says afterwards which of its 140 values were ever decided on purpose.

  A lookup resolves through the chain, skin by skin, own winning. Skins are matched by name, or by
  `Inherits skin from` where one is set - and that may point at a skin of the **same** config, because a
  variant skin is usually a variant of the skin next to it rather than of anything in the library. The
  editor offers the candidates and refuses the ones that would close a circle.

  In a config inspector an inherited style is listed read-only and tinted blue, **Overr.** copies it into
  the config so it can be changed (yellow), **Revert** drops the copy again; an override is per skin. The
  same three states appear on a style applier. Read-only is not cosmetics: a resolved inherited style IS
  the parent's instance, so writing to it would edit the parent - and for the package copy that save is
  discarded without a word. Every write path materialises first.

  `Gui Toolkit → Style Config Drift Report...` compares two configs, or two single skins, style by style
  and value by value, and writes nothing. It answers the question an existing clone cannot answer about
  itself: how much of it carries no information. The same window converts a clone into a child, listing
  every droppable copy by name so any one of them can be kept as a pinned override - and no style is ever
  removed unless something else still provides it.

  `Gui Toolkit → Configuration` now offers **Inherit** next to **Clone**, and the parent can be set there
  as well as in the config inspector.

- **Gaps on `UiRoundedImage`** — a shape can be interrupted, and what an interruption *is* depends on what
  the shape is:
  - **With a frame** there is an outline, and a gap interrupts one of its four **sides**. Four at once give
    corner brackets with no bitmap involved.
  - **Filled** there is no outline to interrupt, so a gap cuts a **band** right through instead: one
    horizontal, one vertical. Both at maximum leave exactly the four rounded corners standing.

  Each gap is `Active`, `Size` and `Offset`, with `Offset` measured from the centre so a centred gap needs
  no offset at all. `Gap Unit` decides how the two numbers are read — **`Normalized`** as a fraction of the
  side, so the gap keeps its proportion when the rect resizes, or **`Pixels`**, so it keeps its
  measurement. Readable and writable from code as well (`GapTop`, `GapHorizontal`, `GetGap(ESide2D)`,
  `SetGap`, `GapUnit`).

  The inspector shows only the gaps that can do something in the current mode, behind a `Gaps` foldout
  whose collapsed header names what is switched on. Each gap is one line until it is switched on, and only
  then costs the two measurements — a slider each when normalized, a plain field when pixels, because no
  pixel range would mean anything.

  Two things in the geometry are worth knowing. A gap is resolved to absolute coordinates ONCE from the
  outer rect and then handed to every ring: a frame with fade is three concentric rings of different size,
  and normalising per ring would cut each at a slightly different place and leave the fade edges bridging
  the gap. And every gap is clamped to the straight run of its axis, which is what keeps the corners intact
  in the square and the rounded case alike, with no corner handling at all — and what makes the maximum
  useful rather than destructive.

  The filled mesh had to be re-triangulated for this. It was one fan from the rect's centre, in which every
  corner triangle reaches across the whole shape, so a band through the middle would have had to split all
  of them. A filled rounded rect tiles exactly as three rectangles plus four quarter discs, and rectangles
  split against a band trivially. Same silhouette, different triangles
- **`mirror_variant_graph`** (`UiVariantGraph`) — copies the library's prefabs into the project **with the
  inheritance between them**. 22 of the 66 library prefabs are themselves variants (`OkButton` and three
  siblings all descend from `StandardButton`); a plain one-variant-per-prefab run owns them all but relates
  them to the package rather than to each other, so a change to the project's `StandardButton` reaches none
  of them. This creates roots as variants of the library prefab and each dependent as a variant of the
  PROJECT copy of its base, transplanting the library variant's overrides — property values, added objects,
  added and removed components, and the internal references re-aimed at the copy. Dry-runs by default, and
  verifies every rebuilt dependent against its original property for property. `replaceExisting` is
  `none`/`dependents`/`all` so hand-edited roots survive a rebuild of what sits below them. Replacements
  are deleted bottom-up before anything is rebuilt: removing a base while its dependents are still on
  disk makes Unity re-import them parentless and fill the console with "Missing Prefab Variant parent",
  which reads like data loss and is only a half-demolished chain
  Mirrors the library's folder structure under the target by default, and MOVES an existing copy that sits
  in the wrong folder instead of rebuilding it — a move keeps its GUID, its place in the chain and any hand
  edits, so a flat set can be reorganised without losing them
  Works around a Unity quirk found while doing it: `AssetDatabase.CreateFolder(parent, "Dialogs")` returns
  `parent/DialogS` when the parent already holds a file starting with that prefix in another case
  (`DialogStub Variant.prefab`), so the requested name cannot be trusted — the folder is read back and
  renamed, and all folders are created before any asset moves into them
- **`variantOf` in the screen description** — a root node can declare `"variantOf": "StandardButton"` and
  the bake produces a prefab **variant** of it instead of a standalone asset, so the result follows its
  base. `overrides` changes inherited parts, `children` adds new ones. A root `template` node always did
  this silently; it now warns and says so. The bake result reports `variantOf` when the saved asset
  inherits — the one property of a baked prefab that cannot be seen by looking at it
- **`BEST-PRACTICES.md`** — the three things a project should take ownership of on day one, because the
  default in each case is the library's own copy and the package is read-only: prefab variants of the
  standard elements (created in **one bulk run**, so the registry's client-over-library ranking makes
  every existing reference resolve to them with nothing rewired), the style config, and what
  `IsApplicable` decides. Linked from `README.md`, `CLAUDE.md` and `mcp~/README.md`
- **Theming over the MCP bridge** (`UiStyleWriter`) — the styling system was readable from outside but not
  writable, so a project's look could only be changed in the Inspector:
  - `clone_style_config` — copy the package's style config into the project and repoint the
    `UiToolkitConfiguration` at it. Idempotent; `write_skin` refuses package-owned configs, so the order
    cannot be got wrong by accident
  - `read_skin` — the values behind the style names (colours, fonts, sizes, sprites), applicable ones by
    default, in the same notation the screen-authoring JSON uses for props
  - `write_skin` — write them, addressed by style name + target component type, with before/after per
    value and a `dryRun` plan. All-or-nothing: everything is resolved and converted before anything is
    written, so a rejected call leaves the config exactly as it found it
  - TMP `VertexGradient` is now a first-class value in the screen/style JSON (`{topLeft…}`, `[top, bottom]`
    or a single colour) — it is how a two-tone headline is made, and without it the only route was a
    hand-tinted material per prefab
- **`execute_code`** (`UiCodeRunner`) — compile and run a C# snippet inside the editor over the bridge, so
  the toolkit stays fully drivable in projects that have no separate code-execution bridge. Bare statements
  are wrapped (common namespaces pre-imported); compile diagnostics carry line numbers in the caller's own
  source. Not a sandbox — editor rights, main thread, real writes
- **Gettext ↔ Google Sheets sync** (`LocaGettextSheetsSyncer`):
  - `[Create by PO]` inspector button — auto-generates column configuration from PO files on disk (language columns + plural forms)
  - `[Push new keys]` inspector button — appends keys from PO files that are missing in the Google Sheet; never overwrites existing cells
  - `[Pull from Sheets]` inspector button — merges translations from the linked Google Sheet into local PO files conservatively (only fills empty translations)
  - `AutoSyncAfterMerge` setting in `UiToolkitConfiguration` — auto-pushes new keys to Google Sheets after every POT→PO merge
- **POT→PO merge pipeline** (`PoMergeEngine`, `LocaPoMerger`):
  - Merge POT template changes into PO files while preserving existing translations
  - Marks removed keys as obsolete (`#~`) rather than deleting them
  - SSoT (Single Source of Truth) header protection — spreadsheet-managed PO files are not overwritten by merge
  - `AutoMergePotToPo` setting in `UiToolkitConfiguration`
  - Manual merge menu: `Gui Toolkit > Localization > Merge POT into PO Files`
- **PO file backup management** (`PoBackupManager`) — keeps up to 10 backup revisions per file; accessible via editor window
- **Google Sheets push** (`LocaExcelBridgePusher`) — write in-memory translations back to the linked Google Sheet
- **CSV export** (`LocaCsvExporter`) — export all PO translations to CSV for offline review
- **UiLocalizedTextMeshProUGUI** — `LocaManager` bootstrap race-condition fix via coroutine retry

- **`UiChip`** — a compact, colour-coded label with an optional leading icon: a division name, a tag, a
  state word. It owns no look of its own, so the same prefab is a list-row chip or an inline one depending
  on which style it carries (`Chip/Default`, `Chip/Small`). Display only unless it is made clickable, and
  then it says so: a chip that takes no clicks does not raycast either, so the tap reaches the card or row
  underneath instead of dying on the chip. `AddClickListener()` switches that on by itself, because a
  listener on a chip that cannot be clicked would never fire and the cause would be invisible.
  Ships as the standard element `StandardChip`.

- **`UiCountIndicator`** — an "x / y" counter that also says whether the number is acceptable: too few,
  right, too many. It holds no rule. Whether five athletes are enough is a question about a division, not
  about a label, so the caller sets the numbers and may set the verdict; deriving it from the two numbers
  is only the default, and setting `State` turns the derivation off until `ClearStateOverride()` hands it
  back. The three verdicts wear the styles `CountIndicator/Below`, `/Ok` and `/Above`, so the colours
  belong to the skin and the component has no state machine of its own. An optional second counter covers
  a side condition ("of which male: 1 / 2") and ships wired but switched off, so the plain case needs no
  setup. Ships as the standard element `StandardCountIndicator`.

  Deliberately **not** `[ExecuteAlways]`: the verdict is applied by writing a style name and a text into
  the object, and running that on every prefab open would edit assets nobody touched. An author still gets
  a live update, because `OnValidate` refreshes - through `AssetReadyGate`, so it lands after the import
  rather than during it.

- **Screen authoring: a node can ship switched off.** `"active": false` on a node, and now also inside a
  template's `"overrides"`, for an icon slot with no icon yet, an empty-state line, a part only one of
  several states shows. Applied last of everything done to the node, because style appliers resolve when
  they are enabled and deactivating any earlier would bake an unstyled object. `read_screen` emits it
  again when a node is off - a value the writer can set but the reader cannot see is a value that a
  read-and-re-bake silently throws away.


### Fixed
- **The package did not compile at all on Unity 2022.3 — the version its own `package.json` names.** Two
  places reach for Roslyn, and both are only compiled BELOW Unity 6, which is why nothing in the
  development app ever saw them. `LocaExcelBridge` throws `RoslynUnavailableException` in the `#else`
  branch of its `UITK_USE_ROSLYN` switch without a `using GuiToolkit.Exceptions;`, and `UiCodeRunner`
  uses `Microsoft.CodeAnalysis.*` with no guard where its neighbour `RoslynComponentReplacer` carries
  `#if UITK_USE_ROSLYN || UNITY_6000_0_OR_NEWER`. The second one closes a trap around the first: the
  menu item that installs the Roslyn DLLs on older Unity versions lives in the editor assembly that
  fails to compile, so the documented way out was unreachable from inside the Editor. Guarded now, with
  the `executeCode` bridge case answering `RoslynUnavailableException` rather than vanishing silently.

  Installing the DLL hack is not an alternative fix, which is worth writing down: the global
  `UITK_USE_ROSLYN` define also switches `LocaExcelBridge` — a RUNTIME file — onto `ExcelDataReader`,
  and a runtime assembly cannot reference the editor-only asmdef the hack creates. Below Unity 6 that
  file has to keep taking its `#else` branch

- **The bake-time check for `@loca:` keys trusted the POT alone, and a POT is a harvest.** It is only as
  current as the last loca processing pass, while the PO files are what translators and the runtime work
  with — so a project that has not re-run the pass since its last translation round got a warning for every
  key that resolves perfectly well. Measured in a live project, whose POT held 69 of 500 keys: the warning
  would have fired on more texts than it spared. `LocaManager.EdHasKey` now falls back to the PO catalogs
  (`.po` and `.po.txt`, their union, read once per domain and kept strictly apart from the harvest so a key
  merely READ from a PO can never be written back into a POT), and `EdHasAnyKeys` counts them as evidence
  that a group has a catalog worth checking against. A misspelled key is still reported, which is the case
  the check exists for.

  Found alongside it: a key that appears only as a **plural** entry was reported missing. `EdAddKey` puts
  the singular of a plural into a separate dictionary and returns, deliberately — the POT writer emits the
  two sets as separate blocks and would otherwise duplicate the entry — but the lookup only ever asked the
  plain keys. It asks both now

- **A style applier in `FullScreenTabDialog` never applied anything.** It asked for
  `FullScreenTabDialog/TabBackground` as a `UiGradientSimple`, and that style exists only as an `Image` -
  which the applier next to it on the same object already resolves. Removed, which changes nothing on
  screen: the gradient keeps the colors it has in the prefab. `FullScreenSettingsDialog` is a variant of
  that prefab and loses it along with it.

- **`Backgrounds/PanelHeadline` cleared the sprite of every Image it styled.** Its `Sprite` value was
  switched on with nothing assigned, so the style applied null. Switched off instead, in both skins, which
  is the truthful statement: this style does not decide the sprite. Verified to change nothing visually -
  every prefab using it has no sprite of its own either.

- **`UiRoundedImage` folded through its own corners once the frame or fade exceeded the radius.** The
  corner piece was drawn between an outer arc of `radius` and an inner arc of `radius - frameSize`, and
  past the radius that inner radius goes NEGATIVE: the inner arc flips to the far side of the corner's
  centre and the triangles cross, which showed as a fan of overlapping slivers in each corner. A frame
  thicker than the radius simply has no rounded inner boundary — it is a plain rectangle inset by the
  frame — so the corner piece is now a triangle fan onto that rectangle's corner, carried over the arc
  AND over the straight run of each side between the tangent point and the inset rectangle (without those
  two extra points the piece stops at the tangent and leaves a notch). A ring that used the radius up
  leaves the next one square, and the radius no longer goes negative between rings; a frame thicker than
  half the shape is clamped so the two inner corners cannot cross. Checked by winding: across nine
  configurations spanning the switchover, every triangle now turns the same way and none has zero area —
  at exactly frame == radius the apex coincides with the arc's centre, which used to add slivers whose
  orientation was decided by rounding error
- **`UiRoundedImage`'s fade ring recoloured a fixed 32 vertices** — "the frame is 8 quads, 16 tris, 32
  verts" — to find the ring it had just emitted. That held only as long as every side was exactly one quad.
  It now takes the ring's first vertex index. Invisible before edge gaps existed, and immediately visible
  with one: a single gap left 3 boundary vertices opaque, three gaps left 9, which reads as a hard edge
  where the antialiasing should be
- **A localized label could not be cleared.** `UiLocalizedTextMeshProUGUI` overrides TMP's `text` setter and
  routes the value into the loca KEY; `ApplyTranslation()` then returns early on an empty key, deliberately,
  so a prefab's design-time placeholder survives when nothing has been assigned yet. Both halves are
  reasonable, but together they made `text = ""` a no-op: the placeholder was promoted to real content
  instead of being replaced. `UiPlayerSettingBase.SetData` assigns `Text = _playerSetting.Title`
  unconditionally, so every setting WITHOUT a title displayed whatever its prefab happened to carry — the
  language row in a settings dialog printed the word "Button". An explicit empty assignment now clears the
  displayed text, while the passive path keeps the placeholder as before, so the Editor still shows it
- **`mirror_variant_graph` produced variants that carried what their originals had REMOVED,** and the
  verification pass reported them as identical. Three faults, each hiding the next: removed components were
  looked up in the wrong tree (`assetComponent` belongs to the base, not to the source) so every removal was
  skipped in silence; removed GameObjects were not handled at all; and the check only asked whether
  everything in the original was present in the copy, never whether the copy had more. The visible result
  was a radio row that still carried the plain toggle underneath it, writing a bool into a string setting the
  moment the row was used. The comparison now runs both ways and counts EXACT types — `GetComponents(type)`
  matches subclasses, which is precisely how a base component that should have been replaced by a derived
  one passed as equal
- **`screenshot_view` returned a blurred, colour-fringed preview** in projects whose render pipeline
  has post-processing in its DEFAULT volume profile — no Volume component anywhere is needed for that,
  and one client's default had depth of field, chromatic aberration, lens distortion and film grain all
  on. The preview camera now excludes every volume layer, so a preview shows the UI and nothing else.
  Same cause as the runtime UI camera's HDRP blur, one camera further along
- **`Bootstrap` never initialised in an unfocused editor.** Its editor-side start hung off
  `EditorApplication.delayCall`, which promises only "some later tick" — measured in a background editor,
  a delayCall had still not run after 20 seconds while `EditorApplication.update` ticked normally
  throughout. Everything then reported "GuiToolkit is not initialized" until a human clicked into the
  window. Now scheduled as a one-shot on `EditorApplication.update`, which fires regardless of focus
- **`AssetReadyGate.WhenReady` stopped waiting while the editor was unfocused.** The focus check was meant
  to keep a slower background editor from running out of its frame budget, but it skipped the whole tick —
  so nothing was re-checked until a human clicked into the window. Everything gated on it waited for that
  click, `Bootstrap` included, which left the toolkit reporting itself uninitialised to anything driving
  the editor from outside. It now keeps checking while unfocused and only spends the frame budget while
  focused
- **Cloning a style config left its internal back-references pointing at the original.** Every skin
  and every style holds a reference to the config it belongs to, and `Instantiate` copies those
  verbatim — 128 of them in the default config — so a cloned config's styles believed they still
  lived in the package asset and the editor's cross-style synchronisation reacted to the wrong
  document. Repaired in both clone paths (the Configuration window's button and `clone_style_config`)