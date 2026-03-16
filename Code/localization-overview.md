---
layout: default
title: Localization - Overview
---

# Localization System

## Abstract

The **unity-gui-toolkit** localization system provides comprehensive translation support for Unity projects. It is built on the industry-standard **GNU gettext** format, using `.po` (Portable Object) files to store translations.

The system supports:
- **Multiple translation sources**: Standard PO files, Excel spreadsheets, and Google Sheets
- **Gettext compatibility**: Standard msgid, msgctxt, and plural forms
- **Custom providers**: Extensible architecture for DLC, databases, or other data sources
- **Runtime language switching**: Change languages on-the-fly without restarting
- **Automatic UI updates**: Components automatically refresh when language changes
- **Push/pull synchronization**: Pull translations from Google Sheets or push local changes back; merge POT template changes into PO files while preserving existing translations
- **Gettext-Sheets sync**: Push new translation keys from code directly to Google Sheets; pull translations back into PO files
- **CSV export**: Export all translations to a spreadsheet-compatible CSV for review or offline editing

![Localization System Architecture](../assets/loca-architecture.png)
*The localization system connects translation sources to UI components via the LocaManager*

---

## Core Components

### LocaManager
The central singleton that manages all translations. Provides the API for loading languages, translating keys, and managing custom providers.

### Translation Sources
Three built-in methods for providing translations:
1. **PO Files** — Standard GNU gettext files stored in Unity Resources
2. **Excel/Google Sheets** — Tabular data imported via `LocaExcelBridge`
3. **Custom Providers** — Implement `ILocaProvider` for any data source

### UI Components
- **UiLocalizedTextMeshProUGUI** — Modern component for automatic text localization
- **LocaMonoBehaviour** — Base class providing gettext convenience functions

### Editor Tools
- **LocaProcessor** — Extracts localization keys from code and scenes
- **LocaPluralProcessor** — Generates plural form rules from PO headers
- **LocaGettextSheetsSyncer** — Syncs PO file keys to/from Google Sheets (push new keys, pull translations, auto-build column config)

---

## Quick Start Example

```csharp
using GuiToolkit;

public class MyMenu : LocaMonoBehaviour
{
    void Start()
    {
        // Simple translation
        string greeting = _("hello");
        
        // Context-aware translation (disambiguate "Cancel" button vs menu item)
        string cancelBtn = pgettext("Cancel", "button");
        
        // Plural form (automatically selects correct form for language)
        int count = 5;
        string apples = _n("One apple", "%d apples", count);
        
        // Change language at runtime
        LocaManager.Instance.ChangeLanguage("de");
        // All UI components automatically update
    }
}
```

---

## When to Use Each Translation Method

| Method | Best For | Editor/Runtime |
|--------|----------|----------------|
| **PO Files** | Traditional gettext workflow, version control friendly | Both |
| **Excel (Local)** | Non-programmers, offline editing, batch updates | Editor only |
| **Google Sheets** | Team collaboration, translators without Unity access; supports code-driven workflow (Code → PO → Sheets → PO) via `[Push new keys]` / `[Pull from Sheets]` | Editor only |
| **Custom Provider** | Dynamic content (DLC, user mods, server-based) | Runtime |

---

## GNU Gettext Compatibility

✅ **Supported GNU Gettext Features:**
- Standard PO/POT file format
- `msgid` (singular translations)
- `msgctxt` (context disambiguation)
- `msgid_plural` + `msgstr[n]` (plural forms)
- Fuzzy entries (`#, fuzzy`)
- Translator comments (`#.`) and source references (`#:`)

⚠️ **Non-Standard Behavior:**

| Feature | GNU Gettext | unity-gui-toolkit | Impact |
|---------|-------------|-------------------|--------|
| **pgettext parameter order** | `pgettext(context, msgid)` | `pgettext(msgid, context, group)` | 🔴 External tools may not work |
| **Group namespace** | Not supported | Third parameter `group` | ⚠️ Extension (optional) |
| **"dev" language** | Not standard | Returns keys instead of translations | ⚠️ Development feature |
| **Return value control** | Fixed behavior | `RetValIfNotFound` enum | ⚠️ Convenience extension |

> **Important:** The **parameter order difference** in `pgettext()` means that standard gettext extraction tools (like `xgettext`) will not parse this code correctly. Use the built-in **LocaProcessor** tool instead: `Tools > Loca > Process Loca Keys`

---

## File Organization

```
YourProject/
├── Assets/
│   └── Resources/
│       ├── en.po              ← English translations (default group)
│       ├── en_ui.po           ← English UI group
│       ├── de.po              ← German translations
│       ├── de_ui.po           ← German UI group
│       └── uitk_loca_groups   ← List of group names
│
└── Packages/
    └── de.phoenixgrafik.ui-toolkit/
        ├── Editor/Loca/
        │   └── LocaProcessor.cs     ← Key extraction tool
        └── Runtime/Code/Loca/
            └── LocaManager.cs       ← Core localization API
```

---

## Translation Groups

**Groups** organize translations into logical categories (e.g., `ui`, `audio`, `items`). This allows:
- Splitting large translation files into manageable chunks
- Loading only relevant translations (e.g., DLC content)
- Namespace separation to avoid key conflicts

```csharp
// Translate from "ui" group
string label = _("settings", "ui");

// Translate from "items" group  
string weapon = _("sword", "items");
```

Each language can have multiple PO files:
- `en.po` — Default/no group
- `en_ui.po` — UI group
- `en_items.po` — Items group

---

## Next Steps

Explore the detailed workflows for each translation method:
- [PO Files Workflow](localization-gettext.html) — Traditional gettext approach
- [Excel Integration](localization-excel.html) — Local spreadsheet editing
- [Google Sheets](localization-google-sheets.html) — Cloud-based collaboration
- [General Workflow](localization-workflow.html) — Common patterns and best practices

---

## Additional Resources

- **API Documentation**: Generated by Doxygen (see project repository)
- **Code Examples**: `Assets/Demo/` in the development app
- **Issue Tracking**: [GitHub Issues](https://github.com/Arkarit/unity-gui-toolkit/issues)
