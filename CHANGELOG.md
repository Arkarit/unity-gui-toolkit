# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Added
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