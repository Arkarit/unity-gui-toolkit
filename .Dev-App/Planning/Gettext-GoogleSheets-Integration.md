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

All decisions resolved:

### 1. msgctxt → Sheets columns ✅
Map `msgctxt` → `KeyPrefix` (already present in bridge config, no new column needed).

### 2. Plural forms ✅
Use the existing mapping from `LocaExcelBridge.CollectData()` in reverse for push.
No new design work required — implementation only.

### 3. Bridge configuration: 1:1 or n:m? ✅
**n:m**: multiple `LocaExcelBridge` instances can each cover a different PO group,
pointing to separate sheets. One bridge per group.

### 4. Column format on first push ✅
**Source: bridge configuration** (`m_columnDescriptions`), not PO files.

A **[Create by PO]** button in the inspector generates/updates the bridge column
configuration from the PO files currently in the project:
- If the bridge already has columns: show a confirmation dialog ("Sync columns from PO files? OK / Cancel")
- **Keep** existing columns that are already configured
- **Ignore** PO languages that would conflict with existing columns in an unexpected way
- **Append** new languages found in PO files to the end

This keeps the configuration explicit and predictable while still offering a
zero-effort starting point.

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

