# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Added
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


### Fixed
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