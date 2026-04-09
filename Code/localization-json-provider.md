---
layout: default
title: Localization - JSON Key Provider
---

# JSON Key Provider

## Overview

The **`LocaJsonKeyProvider`** is an editor-only `ScriptableObject` that automatically harvests localization keys from JSON data files. It is designed for games that load runtime data (tutorial texts, shop item names, quest descriptions, etc.) from JSON files and want those strings to be translatable.

At a high level the workflow is:
1. Create a `LocaJsonKeyProvider` asset pointing to your JSON files
2. Run **`Tools > Loca > Process Loca Keys`** — keys are extracted into a POT file
3. Provide PO translations for each language
4. In runtime code, wrap JSON values in `_(..., group)` to apply translations

> **Editor-only**: `LocaJsonKeyProvider` is compiled only in the Unity Editor. At runtime the game relies on the generated PO files, not the provider itself.

---

## Creating a JSON Key Provider

1. **Right-click** in the Project window → **Create > Loca > JSON Key Provider**
2. Name the asset descriptively (e.g., `TutorialJsonKeys`, `ShopItemJsonKeys`)

---

## Inspector Fields

| Field | Description |
|-------|-------------|
| **Entries** | List of JSON sources. Each entry has a JSON file and field names |
| **JSON File** | The `TextAsset` referencing the JSON file to scan |
| **Field Names** | Property names to search for recursively (e.g. `tutorialText`, `title`) |
| **Group** | Translation group for the harvested keys (e.g. `Json`) |

---

## How Key Extraction Works

The provider scans each JSON file **recursively**. Any string value found under a matching property name (at any nesting depth) is added as a localization key.

**Example JSON** (`bow_tutorial.json`):
```json
[
  {
    "category": "Stunts",
    "tutorialSteps": [
      { "tutorialText": "Swipe left to start a stunt!" },
      { "tutorialText": "Hold to charge for bonus points." }
    ]
  }
]
```

**Field Names configured**: `tutorialText`

**Extracted keys**:
```
Swipe left to start a stunt!
Hold to charge for bonus points.
```

These keys are written into the POT file for the configured group (e.g., `loca_Json.pot`).

**Filtering**: Pure numbers and very short/whitespace-only strings are automatically skipped.

---

## Step-by-Step Example

### Step 1 — Create the Provider Asset

Right-click → **Create > Loca > JSON Key Provider**, name it `ShopJsonKeys`.

### Step 2 — Configure Entries

In the Inspector, add an Entry:
- **JSON File**: drag in `BowShopItems.json`
- **Field Names**: `title`
- **Group**: `Json`

Add a second Entry if you have more JSON files to harvest.

### Step 3 — Run Key Extraction

**Menu**: `Tools > Loca > Process Loca Keys`

This generates (or updates) `loca_Json.pot` in your configured POT Path:
```pot
#: BowShopItems.json
msgid "Premium Bundle"
msgstr ""

msgid "Starter Pack"
msgstr ""
```

### Step 4 — Create PO Files

Create one PO file per language in a `Resources/` folder:
- `Assets/Resources/en_Json.po`
- `Assets/Resources/de_Json.po`
- etc.

Fill in translations:
```po
msgid "Premium Bundle"
msgstr "Premium-Paket"
```

> **Tip:** Use `Tools > Loca > Merge POT into PO Files` (or enable **Auto-Merge** in UiToolkitConfiguration) to automatically update existing PO files when new keys are added.

### Step 5 — Register the Group

Run **`Tools > Loca > Process Loca Keys`** once — it auto-generates `uitk_loca_groups.txt` in the Resources folder, which tells the runtime to load the `Json` group PO files.

### Step 6 — Use Translations at Runtime

In your runtime code, pass the JSON value through `_()` with the matching group:

```csharp
using GuiToolkit;

public class ShopItemView : LocaMonoBehaviour
{
    [SerializeField] private TMP_Text m_titleLabel;

    public void SetItem(ShopItem item)
    {
        // item.title comes from JSON — translate it at display time
        m_titleLabel.text = _(item.title, "Json");
    }
}
```

---

## Multiple JSON Files in One Provider

A single `LocaJsonKeyProvider` can reference multiple JSON files with different field sets:

| Entry | JSON File | Field Names | Group |
|-------|-----------|-------------|-------|
| 0 | `bow_tutorial.json` | `tutorialText` | `Json` |
| 1 | `BowShopItems.json` | `title` | `Json` |
| 2 | `TaskManagerDataJSON.json` | `taskName` | `Json` |

All extracted keys land in the same `loca_Json.pot` (and corresponding PO files).

Alternatively, create **separate providers per JSON file** for better organization.

---

## Notes & Caveats

- **Keys are the raw JSON string values** (e.g., `"Swipe left to start a stunt!"`), not symbolic keys. This means the English source string is used directly as the key, following standard gettext convention.
- **Recursive scanning**: The provider walks the entire JSON tree. If the same field name appears at multiple nesting levels, all string values under it are collected.
- **No deduplication across providers**: If two providers extract the same key into the same group, the POT file will contain a duplicate entry. The runtime handles duplicates gracefully (first value wins), but it is cleaner to avoid them.
- **Auto-Merge recommended**: Enable **Auto-Merge** in UiToolkitConfiguration so PO files are kept in sync with the POT file automatically after each key extraction run.

---

## Troubleshooting

| Problem | Cause | Solution |
|---------|-------|----------|
| Keys not appearing in POT | Provider asset not in a scanned folder | Ensure it is somewhere under `Assets/` |
| JSON file not found | TextAsset reference is null | Assign the JSON TextAsset in the Inspector |
| Runtime translation not working | Group not loaded | Run `Process Loca Keys` to regenerate `uitk_loca_groups.txt` |
| Runtime translation not working | Wrong group in `_()` call | Make sure `_()` is called with the same group as configured in the provider |

---

## Next Steps

- [PO Files Workflow](localization-gettext.html) — Editing and managing PO files
- [General Workflow](localization-workflow.html) — Complete end-to-end workflow
- [Google Sheets](localization-google-sheets.html) — Push keys to Sheets for translator access
