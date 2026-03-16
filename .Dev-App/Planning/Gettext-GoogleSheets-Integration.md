# Gettext ↔ Google Sheets Integration

## Goal

Developers define localization keys directly in code (`_()`, `gettext()`, etc.).
Those keys automatically flow into Google Sheets so that translators can work in
their familiar environment. Translations then flow back into the project.

---

## Proposed Data Flow

```
Code (_(), gettext(), …)
        │
        ▼
  LocaProcessor          ← already implemented
        │ POT file(s)
        ▼
  PoMergeEngine          ← already implemented
        │ PO file(s) (new keys empty, existing translations preserved)
        ▼
  [PUSH] Google Sheets   ← NEW: append new rows only, never overwrite
        │
  Translators fill in Sheets
        │
        ▼
  [PULL] PO files        ← partially implemented (LocaExcelBridge already reads)
        │
        ▼
     Runtime
```

### Push direction (PO → Sheets)
- **Conservative**: only append rows for keys that do not yet exist in Sheets
- Existing translations are **never** overwritten
- Empty `msgstr` fields are written as empty cells (translators fill them in)
- If the sheet is empty: write the header row first, then append keys

### Pull direction (Sheets → PO)
- **Authoritative**: Sheets is the SSoT for translations
- Updates `msgstr` in PO files for every key that has a value in Sheets
- Keys present in PO but absent from Sheets → left untouched (merge, not replace)
- SSoT protection (`PoSsotProtector`) triggers if someone edits PO files manually

---

## Why PO as an Intermediate Step

A direct Code→Sheets path would be possible, but the PO intermediate pays off:
- **Local cache**: project works offline
- **Git history**: translation changes are traceable
- **Plural handling**: PO format manages plural forms cleanly; column mapping to Sheets is already implemented in `LocaExcelBridge`
- **Already implemented**: `LocaProcessor`, `PoMergeEngine`, and the PO parser are all in place — only the bridge toward Sheets is missing

---

## Open Design Decisions

### 1. msgctxt → Sheets columns
PO has `msgctxt`; Sheets uses `KeyPrefix`/`KeyPostfix` in the bridge configuration.

**Recommendation**: map msgctxt → KeyPrefix (already present in bridge config, no new column needed).

### 2. Plural forms
PO: `msgstr[0]`, `msgstr[1]`, …  
Sheets: separate columns with `PluralForm = 0, 1, …` (already in `LocaExcelBridge`)

The mapping exists for the pull direction. The reverse must be implemented for push.

### 3. Bridge configuration: 1:1 or n:m?
**Still open**: should one bridge cover all PO groups, or should it be possible to
link multiple bridges to different groups?

Options:
- **1:1** (simple): one bridge = one sheet = all keys from all groups
- **n:m** (flexible): one bridge per group, so e.g. UI strings and system strings can live in separate sheets

### 4. Column format on first push
When the sheet is empty we need to:
1. Write the header row: `Key | [Context] | {lang1} | {lang1}[0] | {lang1}[1] | {lang2} | …`
2. Append keys from the PO files as rows

Which languages are included is determined by the PO files present in the project.

---

## What Is Dropped

- **XLSX writing**: makes no sense (anyone who wants PO→Excel can use an external tool)
- `LocaExcelBridgePusher`'s XLSX path can be simplified or removed
- Push logic should be implemented as a standalone "sync" action, **not** as part of the generic bridge mechanism

---

## Planned New Components / Changes

| What | Where | Description |
|---|---|---|
| `LocaGettextSheetsSyncer` | `Editor/Loca/` | Sync logic PO↔Sheets (push + pull) |
| `LocaGettextSheetsSyncerEditor` | `Editor/Components/` | Inspector UI / menu items for sync |
| Extend `LocaExcelBridge` | `Runtime/Code/Loca/` | Pull methods with merge semantics |
| Extend `LocaProcessor` | `Editor/Loca/` | Optional auto-sync after POT extraction |
| Extend `GoogleServiceAccountAuth` | `Runtime/Code/Loca/` | Write scope already implemented ✓ |

---

## Next Steps

1. Clarify design decision: 1:1 or n:m bridge groups?
2. Finalise column format for push (especially context column and plural columns)
3. Implement `LocaGettextSheetsSyncer` (push + pull)
4. Integrate into `LocaProcessor` (auto-sync option)
5. Update gh-pages documentation

