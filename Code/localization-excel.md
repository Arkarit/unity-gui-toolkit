---
layout: default
title: Localization - Excel Integration
---

# Excel Integration (Local Files)

This page describes how to use **local Excel spreadsheets** (`.xlsx` files) as a translation source. This approach is ideal for:
- Non-programmers editing translations
- Batch editing multiple languages at once
- Offline workflows without internet access
- Quick prototyping and testing

---

## Overview

The **LocaExcelBridge** component imports translations from Excel files into Unity. It reads the spreadsheet, maps columns to languages and keys, and provides translations at runtime via the `ILocaProvider` interface.

![Excel Workflow](../assets/loca-excel-workflow.png)
*Excel files are imported into Unity as translation providers*

### Advantages
✅ **Familiar interface** — Most people know Excel  
✅ **Side-by-side editing** — See all languages in one view  
✅ **Batch operations** — Copy, sort, filter translations  
✅ **Offline** — No internet required  
✅ **Version control friendly** — Binary diff may be limited, but readable exports possible

### Disadvantages
❌ **Not real-time** — Requires re-import in Unity after editing  
❌ **Binary format** — Less Git-friendly than PO files  
❌ **No gettext tooling** — Can't use Poedit or other translation tools  
❌ **Manual plural handling** — Need separate columns for each plural form

---

## Step-by-Step Guide

### Step 1: Create Excel File

Create a `.xlsx` spreadsheet with the following structure:

| Key | English | German | French |
|-----|---------|--------|--------|
| menu.play | Play | Spielen | Jouer |
| menu.settings | Settings | Einstellungen | Paramètres |
| menu.quit | Quit | Beenden | Quitter |
| game.score | Score: %d | Punktzahl: %d | Score: %d |

**Guidelines**:
- **First column**: Localization keys (msgid)
- **Subsequent columns**: Translations for each language
- **First row**: Optional headers (can be skipped via "Start Row" setting)
- **Placeholders**: Use `%d` (integer), `%s` (string), or `{0}`, `{1}` for format strings

**Save as**: `MyTranslations.xlsx` in your project (e.g., `Assets/Localization/MyTranslations.xlsx`)

### Step 2: Create LocaExcelBridge Asset

1. **Right-click in Project window** → **Create > UI Toolkit > Loca Excel Bridge**
2. Name it descriptively (e.g., `MyTranslationsExcelBridge`)

![Create LocaExcelBridge](../assets/loca-create-excel-bridge.png)
*Creating a new LocaExcelBridge asset*

### Step 3: Configure the Bridge

Select the `LocaExcelBridge` asset and configure it in the Inspector:

![LocaExcelBridge Inspector](../assets/loca-excel-bridge-inspector.png)
*Inspector settings for LocaExcelBridge*

#### Basic Settings

| Field | Description | Example |
|-------|-------------|---------|
| **Source Type** | Select "Local" for local Excel files | `Local` |
| **Local Excel Path** | Path to `.xlsx` file (relative to project root) | `Assets/Localization/MyTranslations.xlsx` |
| **Group** | Translation group (leave empty for default) | `""` or `"ui"` |
| **Start Row** | First row containing data (0-based; row 0 = headers) | `1` (skip header row) |

#### Column Mapping

Click **"Add Column"** for each column in your Excel file:

| Column Index | Column Type | Language ID | Plural Form | Key Prefix | Key Postfix |
|--------------|-------------|-------------|-------------|------------|-------------|
| 0 | Key | *(leave empty)* | -1 | *(optional)* | *(optional)* |
| 1 | LanguageTranslation | `en` | -1 | | |
| 2 | LanguageTranslation | `de` | -1 | | |
| 3 | LanguageTranslation | `fr` | -1 | | |

**Column Types**:
- **Key**: The msgid column (localization key)
- **LanguageTranslation**: A translation column for a specific language
- **Ignore**: Skip this column during import

**Plural Form**:
- `-1` = Singular translation (default)
- `0`, `1`, `2`, `3`, `4`, `5` = Specific plural form index (see [Plural Forms](#plural-forms) below)

**Key Affixes** (optional):
- **Key Prefix**: Prepended to all keys (e.g., `"ui_"` → `"ui_menu.play"`)
- **Key Postfix**: Appended to all keys (e.g., `"_tooltip"` → `"menu.play_tooltip"`)

### Step 4: Collect Data

**Menu**: `Tools > Loca > Process Loca Providers`

This reads the Excel file and stores the parsed translations in the `LocaExcelBridge` asset.

**Output**: Serialized `ProcessedLoca` data embedded in the asset.

**Console log** (if successful):
```
LocaExcelBridge: Loaded 3 entries from Assets/Localization/MyTranslations.xlsx
```

### Step 5: Register the Provider (Editor)

The `LocaExcelBridge` asset must be in a **Resources** folder to be loaded at runtime:

```
Assets/
└── Resources/
    └── LocaProviders/
        └── MyTranslationsExcelBridge.asset
```

After moving to Resources, run:

**Menu**: `Tools > Loca > Process Loca Providers`

This registers the provider in `_locaProviders.json`, making it available at runtime.

### Step 6: Test

Run the game and change language:

```csharp
using GuiToolkit;

public class LanguageTest : MonoBehaviour
{
    void Start()
    {
        // Change to German
        LocaManager.Instance.ChangeLanguage("de");
        
        // Test translation
        string play = LocaManager.Instance.Translate("menu.play");
        Debug.Log(play);  // Output: "Spielen"
    }
}
```

---

## Advanced Configuration

### Plural Forms

To support plural translations in Excel, use **multiple columns** for each plural form:

| Key | Key (plural) | EN (singular) | EN (plural 0) | EN (plural 1) | DE (singular) | DE (plural 0) | DE (plural 1) |
|-----|--------------|---------------|---------------|---------------|---------------|---------------|---------------|
| apple | apples | One apple | %d apple | %d apples | Ein Apfel | %d Apfel | %d Äpfel |
| book | books | One book | %d book | %d books | Ein Buch | %d Buch | %d Bücher |

**Column Mapping**:

| Column Index | Column Type | Language ID | Plural Form |
|--------------|-------------|-------------|-------------|
| 0 | Key | | -1 |
| 1 | Ignore | | -1 |
| 2 | LanguageTranslation | `en` | -1 (singular, ignore) |
| 3 | LanguageTranslation | `en` | 0 |
| 4 | LanguageTranslation | `en` | 1 |
| 5 | LanguageTranslation | `de` | -1 (singular, ignore) |
| 6 | LanguageTranslation | `de` | 0 |
| 7 | LanguageTranslation | `de` | 1 |

**Usage**:
```csharp
int count = 5;
string text = _n("apple", "apples", count);
// Returns: "5 apples" (English) or "5 Äpfel" (German)
```

> **Note:** For languages with more than 2 plural forms (e.g., Polish has 3, Arabic has 6), add additional columns (plural forms 2-5).

### Key Affixes (Namespacing)

Use affixes to namespace keys from shared Excel files:

**Example**: You have a shared Excel file with generic keys:

| Key | English | German |
|-----|---------|--------|
| open | Open | Öffnen |
| close | Close | Schließen |
| save | Save | Speichern |

**Configure two bridges** for different contexts:

**FileMenuBridge**:
- Key Prefix: `"file_"`
- Key Postfix: `""`
- Result: `"file_open"`, `"file_close"`, `"file_save"`

**WindowMenuBridge**:
- Key Prefix: `"window_"`
- Key Postfix: `""`
- Result: `"window_open"`, `"window_close"`, `"window_save"`

### Context Support

Excel doesn't natively support `msgctxt` (context), but you can **encode context in the key**:

| Key | English | German |
|-----|---------|--------|
| open\|verb | Open | Öffnen |
| open\|adjective | Open | Geöffnet |
| save\|verb | Save | Speichern |
| save\|noun | Save | Speicherstand |

**Usage**:
```csharp
// Manual context handling
string key = $"open|verb";
string translation = _(key);
```

**Alternatively**, use the **Context column** approach:

| Context | Key | English | German |
|---------|-----|---------|--------|
| verb | open | Open | Öffnen |
| adjective | open | Open | Geöffnet |

**Column Mapping**: Add a custom preprocessor (requires scripting) to combine Context + Key into `"context\u0004key"`.

---

## Workflow Tips

### Editing Workflow

1. **Edit Excel file** in Microsoft Excel, LibreOffice Calc, or Google Sheets (download as `.xlsx`)
2. **Save the file**
3. **Return to Unity** and run **`Tools > Loca > Process Loca Providers`**
4. **Test in Play Mode**

> **Tip**: Keep Unity open while editing Excel. After saving, just click the menu item to refresh.

### Multi-Bridge Setup

Use **multiple LocaExcelBridge assets** for different translation categories:

```
Assets/Resources/LocaProviders/
├── UITranslations.asset       ← Group: "ui"
├── ItemTranslations.asset     ← Group: "items"
├── DialogTranslations.asset   ← Group: "dialog"
└── AudioTranslations.asset    ← Group: "audio"
```

Each bridge can reference a different Excel file or different sheets within the same file.

### Export from Excel to PO

If you later want to migrate to PO files:

1. Export Excel to **CSV**
2. Use a conversion tool (e.g., [csv2po](http://docs.translatehouse.org/projects/translate-toolkit/en/latest/commands/csv2po.html)) to generate PO files
3. Import PO files as described in [PO Files Workflow](localization-gettext.html)

---

## Troubleshooting

### Excel File Not Found

**Problem**: Error: `File not found: Assets/Localization/MyTranslations.xlsx`

**Solutions**:
- Verify the path is correct (relative to project root, not `Assets/`)
- Check file extension (`.xlsx`, not `.xls` or `.csv`)
- Ensure file is committed to version control (if using Git LFS for large files)

### Translations Not Loading

**Problem**: Translations don't appear at runtime.

**Solutions**:
- Ensure `LocaExcelBridge` asset is in a **Resources** folder
- Run **`Tools > Loca > Process Loca Providers`** to collect data
- Check column mapping: Language ID must match exactly (case-sensitive)
- Enable **Debug Loca** to see which providers are loaded

### Wrong Column Imported

**Problem**: Wrong language appears.

**Solutions**:
- Check **Column Index** matches Excel (0-based: A=0, B=1, C=2, etc.)
- Verify **Column Type** is set to `LanguageTranslation`
- Check **Language ID** is correct (e.g., `"en"`, not `"English"`)

### Plural Forms Not Working

**Problem**: Plural translation returns singular form.

**Solutions**:
- Ensure **Plural Form** is set correctly: `-1` (singular), `0`, `1`, `2`, etc.
- Verify the key uses `_n()` function: `_n("singular", "plural", count)`
- Run **`Tools > Loca > Generate Plural Rules`** to regenerate plural logic
- Check that both singular and plural keys exist in Excel

---

## Comparison: Excel vs. PO Files

| Feature | Excel | PO Files |
|---------|-------|----------|
| **Ease of Use** | ✅ Familiar to non-programmers | ⚠️ Requires gettext knowledge |
| **Side-by-Side View** | ✅ All languages visible at once | ❌ One language per file |
| **Version Control** | ⚠️ Binary format, limited diffs | ✅ Text-based, merge-friendly |
| **Translation Tools** | ❌ No specialized tools | ✅ Poedit, Lokalize, etc. |
| **Plural Forms** | ⚠️ Manual column setup | ✅ Built-in with `msgid_plural` |
| **Context Support** | ⚠️ Manual encoding | ✅ Built-in `msgctxt` |
| **Offline Editing** | ✅ Yes | ✅ Yes |
| **Cloud Collaboration** | ⚠️ Requires download/upload | ❌ Not natively |

**Recommendation**: Use **Excel** for quick prototyping or teams with non-technical translators. Use **PO files** for long-term projects with version control and professional translation workflows.

---

## Example Excel Template

Download a template to get started quickly:

| Key | English | German | French | Spanish | Comments |
|-----|---------|--------|--------|---------|----------|
| menu.play | Play | Spielen | Jouer | Jugar | Main menu |
| menu.settings | Settings | Einstellungen | Paramètres | Ajustes | Main menu |
| menu.quit | Quit | Beenden | Quitter | Salir | Main menu |
| game.score | Score: %d | Punktzahl: %d | Score: %d | Puntuación: %d | HUD |
| game.health | Health: %d | Leben: %d | Vie: %d | Vida: %d | HUD |
| dialog.confirm | Are you sure? | Sind Sie sicher? | Êtes-vous sûr? | ¿Estás seguro? | Confirmation dialog |
| dialog.yes | Yes | Ja | Oui | Sí | Button label |
| dialog.no | No | Nein | Non | No | Button label |

> **Note**: The "Comments" column can be set to **Ignore** in the column mapping — it's for human reference only.

---

## Advanced: Scripted Import

For complex scenarios, you can extend `LocaExcelBridge` or create a custom `ILocaProvider`:

```csharp
using GuiToolkit;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/My Excel Bridge")]
public class CustomExcelBridge : ScriptableObject, ILocaProvider
{
    [SerializeField] private string m_excelPath;
    private ProcessedLoca m_processedLoca;
    
    public ProcessedLoca Localization => m_processedLoca ?? new ProcessedLoca();
    
    public void Load(string _language)
    {
        // Filter entries for the requested language
        // (called at runtime when language changes)
    }
    
    public void Unload()
    {
        m_processedLoca = null;
    }
    
#if UNITY_EDITOR
    public void CollectData()
    {
        // Read Excel file using a library like EPPlus or ClosedXML
        // Parse rows and populate m_processedLoca
        
        // Example pseudocode:
        // var excel = new ExcelReader(m_excelPath);
        // foreach (var row in excel.Rows())
        // {
        //     var entry = new ProcessedLocaEntry
        //     {
        //         Key = row["Key"],
        //         LanguageId = "en",
        //         Text = row["English"]
        //     };
        //     m_processedLoca.Entries.Add(entry);
        // }
    }
#endif
}
```

This allows full control over parsing logic, error handling, and data transformation.

---

## Next Steps

- **[Google Sheets](localization-google-sheets.html)** — Cloud-based collaboration
- **[PO Files Workflow](localization-gettext.html)** — Version control friendly approach
- **[General Workflow](localization-workflow.html)** — Common patterns and best practices

---

## Additional Resources

- **[Microsoft Excel](https://www.microsoft.com/excel)** — Official Excel application
- **[LibreOffice Calc](https://www.libreoffice.org/discover/calc/)** — Free, open-source alternative
- **[EPPlus Library](https://github.com/EPPlusSoftware/EPPlus)** — C# library for reading Excel files
- **[ClosedXML](https://github.com/ClosedXML/ClosedXML)** — Another C# Excel library
