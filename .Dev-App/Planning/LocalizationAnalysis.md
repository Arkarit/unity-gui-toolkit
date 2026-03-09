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

---

## Weaknesses

### W1 — No Thread Safety ⚠️
`m_translationDict` is accessed without locks. If a background coroutine calls `Translate()` during `ChangeLanguage()`, a race condition is possible. `Thread.CurrentThread` culture updates also affect all code on that thread globally.

### W2 — No Context-Aware Translations
The PO standard supports `msgctxt` for disambiguating identical source strings; the system ignores it. Workaround is manual key prefixing, which clutters the key namespace.

### W3 — Silent Missing-Key Fallback in Production
`DebugLoca` mode must be enabled manually. Missing translations silently return the key name. There is no built-in collection or reporting of untranslated strings, making completeness impossible to measure at a glance.

### W4 — Language Code Inconsistency
Language IDs are case-sensitive in lookups but normalized to lowercase at runtime. A mismatch between Excel column headers and PO file names (e.g. `en-US` vs `en_us`) causes silent load failures.

### W5 — No System Locale Auto-Detection
On first launch the system always defaults to `"dev"`. The OS language (`CultureInfo.CurrentCulture`) is never consulted, requiring every integrating project to implement its own first-run logic.

### W6 — Group Fallback Missing
If a key is not found in the requested group, the system does **not** automatically fall back to the default group. Applications must either duplicate keys or implement the fallback themselves.

### W7 — Language Switching is Synchronous
All PO files for a language are loaded synchronously inside `ChangeLanguageImpl()`. Large catalogs will cause a noticeable frame drop on language switch.

### W8 — Format-String Placeholder Blindness
The system does not track `{0}`, `{1}` placeholders in keys. Translators cannot see what the placeholders represent, and a translator accidentally reordering them causes silent runtime errors.

### W9 — TextAsset Memory Not Released
PO files loaded as `TextAsset` are never unloaded on language change. Applications supporting many languages simultaneously may accumulate significant memory.

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
| H2 | **Language code normalization** — central `NormalizeLanguageId()` replacing `-` with `_`, lowercasing everywhere | Low | Low |
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
| M6 | **Language cache eviction** — unload previous language's `TextAsset`s to reduce memory | Medium | Medium |
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

## Summary

The system is **well-structured and production-capable**, successfully combining gettext tooling, Excel-based translation, plural rules, and dynamic runtime switching. The most impactful improvements are:

1. **Thread safety** (correctness)
2. **Missing-key reporting** (quality assurance)
3. **Language code normalization** (reliability)
4. **Group fallback chain** (usability)
5. **Async language loading** (performance)
