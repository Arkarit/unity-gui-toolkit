# Localization System Analysis — Unity GUI Toolkit

## Architecture Overview

The localization system is a production-grade implementation built around the **gettext standard** using **PO (Portable Object) files**, with additional support for Excel/Google Sheets sources.

### Core Components

| Component | Role |
|---|---|
| `LocaManager` (abstract singleton) | Central translation API; supports singular/plural; fallback chain |
| `LocaManagerDefaultImpl` | Default implementation; manages in-memory translation dictionaries |
| `LocaExcelBridge` | ScriptableObject linking an XLSX file or Google Sheet to the system |
| `LocaProviderList` | Registry of all `LocaExcelBridge` instances (loaded from Resources) |
| `AdditionalLocaKeys` | Editor-only ScriptableObject for bulk key definitions |
| `UiAutoLocalize` | Component that auto-translates a `TMP_Text` based on a localization key |
| `UiLanguageToggle` | UI control for runtime language switching |
| `LocaPlurals` (partial, generated) | Auto-generated plural rules per language (CLDR standard) |
| `UiLocalizedTextMeshProUGUI` | TMP subclass with integrated localization; supersedes `UiAutoLocalize` |
| `UiRefreshOnLocaChange` | Lightweight component triggering `OnLanguageChanged` without `UiThing` inheritance |
| `UiAbstractLocalizedTextByPlayerLevel` | Abstract base class; shows different localized text per player level |
| `LocaJsonKeyProvider` | Editor-only ScriptableObject harvesting translation keys from JSON data files by field name |
| `UiForceUnlocalizedText` / `UiForceLegacyText` | Migration helper components for intentionally unlocalized or legacy text |
| `LocaGettextSheetsSyncer` | Editor tool for the gettext push/pull workflow with Google Sheets |

### Data Flow

```
Editor: "Process Loca" menu
  → LocaProcessor scans scenes / prefabs / ScriptableObjects / .cs files
  → Extracts keys: _("key"), _n("s","p",n), __("key"), gettext(), ngettext()
  → Writes .pot files

Translator fills .po files / Excel sheet

Runtime:
  LocaManager.ChangeLanguage(id)
  → Load .po files + LocaExcelBridge data for that language
  → Commit: set Language, update CultureInfo, save PlayerPrefs, fire EvLanguageChanged
  → All UiThing subclasses with NeedsLanguageChangeCallback=true call OnLanguageChanged()
```

---

## Strengths

### S1 — Industry-Standard Gettext Workflow
- Full support for `_()`, `_n()`, `__()`, `gettext()`, `ngettext()` patterns
- PO/POT files are universally understood by translation platforms (Crowdin, Weblate, Lokalise)
- Language-specific plural rules generated from PO file headers (CLDR)

### S2 — Two Data Sources, One API
- PO files (primary) and Excel/Google Sheets coexist via `ILocaProvider`
- Non-technical translators can use familiar spreadsheet tools
- Google Sheets integration supports cloud-based collaborative translation

### S3 — Automatic Key Extraction
- Regex-based code scan extracts gettext patterns from `.cs` files
- `ILocaKeyProvider` interface auto-discovers keys from components at edit time
- Duplicate and conflict detection with warnings

### S4 — Performance
- O(1) dictionary lookups per translation
- Plural rules are switch-based (no list iteration)
- Lazy loading of PO files; no per-frame overhead after init

### S5 — Extensible Architecture
- Singleton setter enables dependency injection (useful for tests)
- `ILocaProvider` allows additional custom sources
- Groups create logical translation namespaces

### S6 — Defensive Fallback Chain
```
Requested language → "dev" language → key itself (configurable)
```
- Two-phase language switch prevents partial state: attempt first, commit only on success
- `CultureInfo` kept in sync for date/number formatting

### S7 — Editor Tooling
- Menu-driven key extraction, POT generation, plural rule setup
- `LocaExcelBridgeEditor` for spreadsheet configuration
- `SetAllUiLocaGroups` for batch group assignment
- Progress bars for long operations; full undo support

### S8 — Gettext-Sheets Sync Workflow
- `LocaGettextSheetsSyncer` provides a dedicated push/pull workflow: push new keys to a Google Sheet, pull translator corrections back into PO files
- Push is additive: only rows not yet present in the sheet are appended; existing translator work is never overwritten on push
- Pull always overwrites local PO entries so translator corrections win; uses `UNFORMATTED_VALUE` to avoid Google Sheets formatting artifacts
- Timestamped backups are created before any pull overwrites local PO files
- `AutoSyncAfterMerge` flag in `UiToolkitConfiguration` enables automatic sync after a merge operation

### S9 — Rich POT Source References
- `LocaProcessor` now emits full GameObject paths within prefabs and scenes (e.g. `Canvas/Header/TitleLabel`) as `#:` source references
- For C# scripts, the first 30 characters of the source line (stripped of leading whitespace) are included as context
- Translators and reviewers can locate the exact usage site of every key directly from the POT file

### S10 — JSON Key Harvesting
- `LocaJsonKeyProvider` is an editor-only ScriptableObject that extracts translation keys from JSON data files by configurable field name
- Enables data-driven content (item names, descriptions, quest texts) to participate in the standard POT extraction workflow without manual key lists

### S11 — Level-Based Localized Text
- `UiAbstractLocalizedTextByPlayerLevel` provides an abstract base class for UI components that display different localized strings depending on the player's current level
- Implements `ILocaKeyProvider` so all level-variant keys are automatically included in POT extraction
- Concrete implementations wire to the game's level-change event (e.g. `EventsManager.UpdateLevel`)

---

## Weaknesses

### W1 — No Thread Safety ⚠️
`m_translationDict` is accessed without locks. If a background coroutine calls `Translate()` during `ChangeLanguage()`, a race condition is possible. `Thread.CurrentThread` culture updates also affect all code on that thread globally.

### W2 — No Context-Aware Translations ✅ RESOLVED

`msgctxt` parsing is now fully implemented in `LocaManagerDefaultImpl`. Context-aware lookups use the GNU gettext `"context\u0004key"` convention (`ComposeContextKey()`), and a `LocaManager.Translate(key, context, group)` overload is available. See MI2.

### W3 — Silent Missing-Key Fallback in Production
`DebugLoca` mode must be enabled manually. Missing translations silently return the key name. There is no built-in collection or reporting of untranslated strings, making completeness impossible to measure at a glance.

### W4 — Language Code Inconsistency ✅
Language IDs are case-sensitive in lookups but normalized to lowercase at runtime. A mismatch between Excel column headers and PO file names (e.g. `en-US` vs `en_us`) causes silent load failures.
**Fixed**: `LocaManager.NormalizeLanguageId()` converts any ID to canonical BCP 47 lowercase-hyphen form (`zh-tw`, `pt-br`) and logs a warning when the input needed normalizing, so mismatches are visible in the console without breaking the game.

### W5 — No System Locale Auto-Detection
On first launch the system always defaults to `"dev"`. The OS language (`CultureInfo.CurrentCulture`) is never consulted, requiring every integrating project to implement its own first-run logic.

### W6 — Group Fallback Missing
If a key is not found in the requested group, the system does **not** automatically fall back to the default group. Applications must either duplicate keys or implement the fallback themselves.

### W7 — Language Switching is Synchronous
All PO files for a language are loaded synchronously inside `ChangeLanguageImpl()`. Large catalogs will cause a noticeable frame drop on language switch.

### W8 — Format-String Placeholder Blindness
The system does not track `{0}`, `{1}` placeholders in keys. Translators cannot see what the placeholders represent, and a translator accidentally reordering them causes silent runtime errors.

### W9 — ~~TextAsset Memory Not Released~~ ✅ NOT AN ISSUE
`TryLoadPoText()` loads the PO file as a `TextAsset` only to read its `.text` string into a local `string[]`. The `TextAsset` reference is a local variable and is not stored as a member field, so it can be garbage-collected immediately after loading. There is no memory accumulation across language switches.

### W10 — Rigid Excel Column Configuration
Plural form columns must be manually designated. There is no auto-detection of language columns from header names, and no CSV/TSV support.

### W11 — AssetReadyGate Single Point of Failure
If `AssetReadyGate` never fires, `LocaManager` is never initialized. Public `Translate()` calls do not guard against an uninitialized `Language`, returning the key silently.

### W12 — Fragile Key-Extraction Regex
The extraction regex does not handle escaped quotes, multi-line strings, or string concatenation. Complex patterns are silently skipped.

---

## Suggested Improvements

### Priority: High

| ID | Improvement | Effort | Risk |
|---|---|---|---|
| H1 | **Thread safety** — add `lock(m_lockObject)` around `Translate()` and `ChangeLanguageImpl()` | Low | Low |
| H2 | **Language code normalization** — central `NormalizeLanguageId()`, canonical form: lowercase + hyphens (BCP 47), warns on deviation ✅ | Low | Low |
| H3 | **Missing-key reporting** — collect missing keys in a `HashSet`; expose an editor dump method | Medium | Low |
| H4 | **Group fallback chain** — if key not in requested group, fall through to default group automatically | Low | Low |
| H5 | **Async language loading** — `Task.Run()` wrapper for `ChangeLanguageImpl()` to prevent frame drops | Medium | Medium |

### Priority: Medium

| ID | Improvement | Effort | Risk |
|---|---|---|---|
| M1 | **System locale auto-detection** on first launch (consult `CultureInfo.CurrentCulture`) | Low | Medium |
| M2 | **Format-string parameter tracking** — extract `{N}` placeholders into POT comments; validate at runtime | Medium | Low |
| M3 | **Excel column auto-detection** — recognize language codes in column headers without manual config | Medium | Low |
| M4 | **Plural form validation** — warn when a language has fewer plural forms than expected | Low | Low |
| M5 | **`EvLanguageChanging` event** — notify UI before the switch so loading indicators can appear | Low | Low |
| M6 | ~~**Language cache eviction** — unload previous language's `TextAsset`s to reduce memory~~ ✅ Not needed — `TextAsset` is not retained | — | — |
| M7 | **Improved key-extraction regex** — handle escaped quotes, trailing comma, and `__()` lookbehind correctly | Medium | Medium |

### Priority: Low

| ID | Improvement | Effort | Risk |
|---|---|---|---|
| L1 | **Translation coverage dashboard** (Editor window) — keys per language, completeness % | Medium | Low |
| L2 | **CSV/TSV import support** | Medium | Low |
| L3 | **`LocaExcelBridge` metadata** — translator name, last updated, completion percentage | Low | Low |
| L4 | **POT file hash caching** — skip re-extraction if source files unchanged | Low | Low |
| L5 | **Configurable missing-key debug log path** (replace hardcoded `C:\temp`) | Low | Low |

---

## Critical Observation

`AssetReadyGate` is the single point of failure for the entire initialization chain.  
**Recommended guard** in `Translate()`:

```csharp
if (Language == null)
{
    UiLog.LogError("Localization not initialized — AssetReadyGate may not have fired.");
    return _key;
}
```

---

## Missing Implementations

These are features that are either architecturally planned, partially scaffolded, or implied by the standard — but not actually implemented.

### MI1 — Google Sheets Authentication ✅ IMPLEMENTED

**Location:** `Runtime/Code/Loca/GoogleServiceAccountAuth.cs`, `Runtime/Code/Loca/LocaGettextSheetsSyncer.cs`

OAuth2 authentication using a Google service account JSON credential is fully implemented via `GoogleServiceAccountAuth.cs`.

**What was done:**
- `[Push new keys]` button appends only rows not yet present in the sheet, leaving existing translator work untouched
- `[Pull from Sheets]` button overwrites local PO files with sheet values (translator corrections win); uses `UNFORMATTED_VALUE` for raw cell content; creates timestamped backups before overwriting
- `AutoSyncAfterMerge` flag in `UiToolkitConfiguration` enables automatic sync after a merge operation

---

### MI2 — PO Context (`msgctxt`) Not Parsed ✅ IMPLEMENTED

**Location:** `Runtime/Code/Loca/LocaManagerDefaultImpl.cs`

`msgctxt` lines are now fully parsed by the PO parser. Context is incorporated into dictionary keys using the GNU gettext convention `"context\u0004key"` via `ComposeContextKey()`. A `LocaManager.Translate(key, context, group)` overload is available for context-aware lookups. This resolves W2.

---

### MI4— `LocaPlurals` Has No Default / Fallback Rule ❌ High

**Location:** `Assets/Generated/LocaPlurals.cs` (generated file)

The generated `switch` statement covers only the languages that were present when "Process Loca" was last run (`dev`, `de`, `en_us`, `lol`, `ru` in the demo app). Any language not listed causes `nplurals` and `pluralIdx` to remain at their default value of `0`, meaning every plural query silently returns the singular form — with no warning.

**Consequences:**
- Adding a new language to an Excel sheet does not automatically add its plural rules
- Silent data loss: plural translations are loaded but never selected
- Developer must remember to re-run "Process Loca" after adding a language

**What is needed:** A `default` case returning English-style rules (`nplurals=2; plural=(n!=1)`) as a safe fallback, plus a `LogWarning` when an unregistered language is encountered.

---

### MI5 — `ILocaProvider` Runtime Loading is Hardcoded to `LocaExcelBridge` ❌ Medium

**Location:** `Runtime/Code/Loca/LocaManagerDefaultImpl.cs`

`ReadLocaProviders()` calls `Resources.Load<LocaExcelBridge>(path)` with the concrete type hardcoded. The `ILocaProvider` interface exists and is designed for extensibility, but any new provider type (JSON, REST API, database) cannot be registered without modifying `LocaManagerDefaultImpl`. The JSON output path already exists (`WriteJson()`), but JSON is never read back as a provider at runtime.

**What is needed:** Either store the provider type alongside the path in `LocaProviderList`, or use a factory/registry pattern so new `ILocaProvider` implementations can be added without touching core code.

---

### MI6 — `LocaPreBuildProcessor` Has No Error Handling or Validation ❌ Medium

**Location:** `Assets/Demo/Editor/LocaPreBuildProcessor.cs`

The pre-build processor consists of two log lines around a single `LocaProcessor.ProcessLocaProviders()` call. It does not:
- Handle or surface download failures from Google Sheets
- Validate that all expected languages are present after sync
- Check whether any keys are untranslated
- Detect stale or outdated translation data
- Offer a rollback mechanism if sync fails mid-build

A build can therefore succeed silently with missing or empty translations.

**What is needed:** Return/throw on failure, a language coverage check, and an option to treat untranslated keys as build warnings or errors.

---

### MI7 — No Runtime Provider Switching ❌ Low

The editor supports multiple `LocaExcelBridge` assets and a `LocaProviderList` registry. At runtime, however, all providers are loaded once during `ChangeLanguageImpl()` and there is no API to add, remove, or swap providers dynamically (e.g. for DLC language packs or live-update scenarios). The `ILocaProvider` interface has no runtime `Load` / `Unload` lifecycle.

---

### MI8 — PO Translator Comments Not Forwarded to Editor ❌ Low

PO files support `#.` (translator comment), `#:` (source reference), and `#,` (flags like `fuzzy`). The PO parser discards all comment lines. In particular, `fuzzy` entries — which translators use to mark strings needing review after a source change — are silently accepted as valid translations. There is no editor indicator for fuzzy or unreviewed strings.

---

### MI9 — `UiLocalizedTextMeshProUGUI`: TMP Subclass with Integrated Localization ✅ IMPLEMENTED

**Location:** `Runtime/Code/Loca/UiLocalizedTextMeshProUGUI.cs`

`UiLocalizedTextMeshProUGUI` is implemented as a `TextMeshProUGUI` subclass with fully integrated localization.

**What was done:**
- `UiAutoLocalize.cs` is kept for legacy compatibility but is superseded by `UiLocalizedTextMeshProUGUI`
- `ReplaceComponentsWindow` editor tool handles component swaps and YAML reference updates across all scenes and prefabs

---



The system is **well-structured and production-capable**, successfully combining gettext tooling, Excel-based translation, plural rules, and dynamic runtime switching. The most impactful improvements are:

1. **Thread safety** (correctness)
2. **Missing-key reporting** (quality assurance)
3. **Language code normalization** (reliability)
4. **Group fallback chain** (usability)
5. **Async language loading** (performance)

**Most urgent missing implementations:**

| ID | Feature | Severity |
|---|---|---|
| MI1 | Google Sheets authentication (OAuth2 / service account) | ✅ Implemented |
| MI2 | PO `msgctxt` parsing and context-aware translation | ✅ Implemented |
| MI4 | `LocaPlurals` default/fallback rule for unknown languages | 🟠 High |
| MI5 | `ILocaProvider` runtime extensibility (type not hardcoded) | 🟡 Medium |
| MI6 | `LocaPreBuildProcessor` error handling and coverage validation | 🟡 Medium |
| MI7 | Runtime provider switching (DLC / live update) | 🔵 Low |
| MI8 | PO `fuzzy` flag / translator comments forwarded to editor | 🔵 Low |
| MI9 | `UiLocalizedTextMeshProUGUI` TMP subclass + migration tool | ✅ Implemented |

---

## Comparison with Unity Localization Package

### Unity Built-in Advantages over GUI Toolkit

| Feature | Unity Built-in | GUI Toolkit |
|---|---|---|
| Asset localization (Sprites, AudioClips, Prefabs) | ✅ Full | ❌ Text only |
| Smart Strings (ICU-based variable substitution) | ✅ | ❌ |
| Locale auto-detection (OS language) | ✅ | ❌ (W5, requires project code) |
| Async locale switching | ✅ Fully async | ❌ Synchronous (W7) |
| Preloading groups | ✅ | ❌ |
| Pseudo-localization | ✅ Built-in | Partial (`dev` language) |
| Visual table editor | ✅ StringTable editor | ❌ External PO editor |
| Metadata/comments per entry | ✅ | ❌ Discarded (MI8) |
| XLIFF import/export | ✅ | ❌ |
| Play mode preview without build | ✅ | ✅ (works in edit mode too) |
| Thread safety | ✅ | ❌ (W1) |
| Fuzzy entry support | ✅ | ❌ (MI8) |
| Variable tracking in translations | ✅ (Smart Strings) | ❌ |
| Locale fallback chain | ✅ Configurable | Partial (dev → key fallback only) |
| Per-entry comment forwarded to translators | ✅ | ❌ |

### GUI Toolkit Advantages over Unity Built-in

| Feature | Unity Built-in | GUI Toolkit |
|---|---|---|
| Gettext PO/POT standard | ❌ No PO support | ✅ Full PO/POT |
| Crowdin / Weblate / Lokalise integration | Limited (CSV/XLIFF) | ✅ Direct via PO |
| Google Sheets push/pull workflow | Via extension package | ✅ Built-in, code-driven |
| Excel XLSX import | Via extension package | ✅ Built-in |
| Developer-friendly API | `GetLocalizedString(table, key)` verbose | ✅ `_("key")` clean |
| Version control friendly | ❌ Binary `.asset` tables | ✅ Text PO files, diffable |
| Automatic key extraction from C# | ❌ Manual only | ✅ Regex scan + `ILocaKeyProvider` |
| Source references in templates | ❌ | ✅ GO path + line snippet |
| Groups / namespaces | Via tables | ✅ Group parameter |
| No Addressables dependency | Requires Addressables | ✅ Resources-based (optional) |
| JSON key harvesting | ❌ | ✅ `LocaJsonKeyProvider` |
| Level-based localized text | ❌ | ✅ `UiAbstractLocalizedTextByPlayerLevel` |
| Context-aware translations (msgctxt) | ❌ No PO context | ✅ `ComposeContextKey()` |

---

## Missing Features vs Unity Built-in

| Priority | Feature | Notes |
|---|---|---|
| 🔴 High | Asset localization (Sprites, Audio, Prefabs) | Unity supports any asset type per locale |
| 🔴 High | Async locale switching | Frame drop on large catalogs (W7) |
| 🟠 Medium | Smart Strings / ICU variable substitution | Unity's `{count, plural, …}` syntax |
| 🟠 Medium | Pseudo-localization tool | Stress-test layouts with expanded strings |
| 🟠 Medium | Locale auto-detection (OS language) | Requires project-side code currently (W5) |
| 🟡 Low | XLIFF import/export | Industry standard for CAT tools |
| 🟡 Low | Fuzzy entry support / translator comments in editor | PO metadata currently discarded (MI8) |
| 🟡 Low | Visual table editor | Unity has a rich StringTable editor |
| 🟡 Low | Per-entry metadata (notes, author, completion) | Unity supports rich per-entry metadata |
| 🔵 Minimal | Preloading groups | Unity can preload specific locale groups |
