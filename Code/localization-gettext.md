---
layout: default
title: Localization - PO Files (gettext)
---

# PO Files Workflow (GNU gettext)

This page describes the traditional GNU gettext workflow using **PO** (Portable Object) files. This approach is ideal for:
- Projects using version control (Git) — PO files are text-based and diff-friendly
- Teams familiar with gettext workflows
- Integration with professional translation tools (Poedit, Lokalize, etc.)
- Open-source projects with community translators

---

## What Are PO Files?

**PO files** are plain-text files containing translations in the GNU gettext format. They consist of:
- **msgid**: The source key (usually English)
- **msgstr**: The translated text
- **Optional metadata**: Context, plural forms, comments, source references

Example `de.po` (German):
```po
# German translation for Main Menu
msgid "Main Menu"
msgstr "Hauptmenü"

msgid "Play"
msgstr "Spielen"

msgid "Settings"
msgstr "Einstellungen"
```

**POT files** (Portable Object Template) are templates with empty translations — they serve as the master list of keys:

```pot
# Main Menu template
msgid "Main Menu"
msgstr ""

msgid "Play"
msgstr ""
```

---

## Workflow Overview

![PO Files Workflow](../assets/loca-po-workflow.png)
*From code to translated UI: the PO file workflow*

### 1. Mark Strings in Code
Use gettext-style functions (`_()`, `__()`, etc.) to mark translatable strings.

### 2. Extract Keys (Generate POT)
Run **`Tools > Loca > Process Loca Keys`** to scan your project and generate POT files.

### 3. Create PO Files
Copy POT files to create language-specific PO files (e.g., `en.po`, `de.po`, `fr.po`).

### 4. Translate
Fill in the `msgstr` values for each language (manually or using tools like Poedit).

### 5. Deploy
Place finished PO files in `Assets/Resources/` — Unity loads them at runtime.

### 6. Generate Plural Rules (One-Time Setup)
Run **`Tools > Loca > Generate Plural Rules`** to extract plural logic from PO headers.

---

## Step-by-Step Guide

### Step 1: Mark Strings for Translation

In your C# scripts, use gettext-style functions:

```csharp
using GuiToolkit;

public class GameUI : LocaMonoBehaviour
{
    void Start()
    {
        // Simple translation
        titleText.text = _("Game Title");
        
        // Context-aware (disambiguate ambiguous words)
        saveButton.text = pgettext("Save", "verb");  // "Save file"
        saveIcon.tooltip = pgettext("Save", "noun");  // "Save data"
        
        // Plural forms
        int lives = player.RemainingLives;
        statusText.text = _n("1 life remaining", "%d lives remaining", lives);
    }
}
```

Or use `UiLocalizedTextMeshProUGUI` components in the Inspector:

![Localized Text Component](../assets/loca-component-inspector.png)
*Set the Loca Key directly in the Inspector*

### Step 2: Extract Keys

**Menu**: `Tools > Loca > Process Loca Keys`

This tool scans:
- **C# scripts** for `_()`, `__()`, `gettext()`, `pgettext()`, `ngettext()`, etc.
- **Scenes and prefabs** for `UiLocalizedTextMeshProUGUI` components
- **ScriptableObjects** implementing `ILocaKeyProvider`

**Output**: POT files in the directory configured in **Project Settings > UI Toolkit > POT Path**:

```
Assets/Localization/
├── default.pot        ← Keys with no group
├── ui.pot             ← Keys from "ui" group
└── items.pot          ← Keys from "items" group
```

**Example POT file** (`default.pot`):
```pot
# SOME DESCRIPTIVE TITLE.
# Copyright (C) YEAR THE PACKAGE'S COPYRIGHT HOLDER
# This file is distributed under the same license as the PACKAGE package.
#
msgid ""
msgstr ""
"Content-Type: text/plain; charset=UTF-8\n"

#: Assets/Scripts/GameUI.cs:12
msgid "Game Title"
msgstr ""

#: Assets/Scripts/GameUI.cs:15
msgctxt "verb"
msgid "Save"
msgstr ""

#: Assets/Scripts/GameUI.cs:16
msgctxt "noun"
msgid "Save"
msgstr ""

#: Assets/Scripts/GameUI.cs:20
msgid "1 life remaining"
msgid_plural "%d lives remaining"
msgstr[0] ""
msgstr[1] ""
```

> **Note:** The `#:` comments show where each key was found in the source code.

### Step 3: Create Language-Specific PO Files

Copy the POT template for each language:

```bash
# Windows (PowerShell)
Copy-Item "Assets/Localization/default.pot" "Assets/Localization/en.po"
Copy-Item "Assets/Localization/default.pot" "Assets/Localization/de.po"
Copy-Item "Assets/Localization/default.pot" "Assets/Localization/fr.po"

# macOS/Linux
cp Assets/Localization/default.pot Assets/Localization/en.po
cp Assets/Localization/default.pot Assets/Localization/de.po
cp Assets/Localization/default.pot Assets/Localization/fr.po
```

**Important:** The filename determines the language ID:
- `en.po` → Language ID: `"en"`
- `de.po` → Language ID: `"de"`
- `en_ui.po` → Language ID: `"en"`, Group: `"ui"`

### Step 4: Add PO File Headers

Edit each PO file and fill in the header:

**English** (`en.po`):
```po
# English translation for MyGame
# Copyright (C) 2026 MyCompany
# This file is distributed under the same license as the MyGame package.
# John Doe <john@example.com>, 2026.
#
msgid ""
msgstr ""
"Project-Id-Version: MyGame 1.0\n"
"POT-Creation-Date: 2026-03-11 10:00+0000\n"
"PO-Revision-Date: 2026-03-11 10:00+0000\n"
"Last-Translator: John Doe <john@example.com>\n"
"Language-Team: English\n"
"Language: en\n"
"MIME-Version: 1.0\n"
"Content-Type: text/plain; charset=UTF-8\n"
"Content-Transfer-Encoding: 8bit\n"
"Plural-Forms: nplurals=2; plural=(n != 1);\n"
```

**German** (`de.po`):
```po
# German translation for MyGame
# Copyright (C) 2026 MyCompany
# This file is distributed under the same license as the MyGame package.
# Anna Schmidt <anna@example.com>, 2026.
#
msgid ""
msgstr ""
"Project-Id-Version: MyGame 1.0\n"
"POT-Creation-Date: 2026-03-11 10:00+0000\n"
"PO-Revision-Date: 2026-03-11 10:30+0000\n"
"Last-Translator: Anna Schmidt <anna@example.com>\n"
"Language-Team: German\n"
"Language: de\n"
"MIME-Version: 1.0\n"
"Content-Type: text/plain; charset=UTF-8\n"
"Content-Transfer-Encoding: 8bit\n"
"Plural-Forms: nplurals=2; plural=(n != 1);\n"
```

> **Critical:** The `Plural-Forms:` header defines how plural forms work for this language. See [Plural Forms Reference](#plural-forms-reference) below.

### Step 5: Translate

Fill in the `msgstr` values:

**English** (`en.po`):
```po
#: Assets/Scripts/GameUI.cs:12
msgid "Game Title"
msgstr "Game Title"

#: Assets/Scripts/GameUI.cs:15
msgctxt "verb"
msgid "Save"
msgstr "Save"

#: Assets/Scripts/GameUI.cs:16
msgctxt "noun"
msgid "Save"
msgstr "Save"

#: Assets/Scripts/GameUI.cs:20
msgid "1 life remaining"
msgid_plural "%d lives remaining"
msgstr[0] "1 life remaining"
msgstr[1] "%d lives remaining"
```

**German** (`de.po`):
```po
#: Assets/Scripts/GameUI.cs:12
msgid "Game Title"
msgstr "Spieltitel"

#: Assets/Scripts/GameUI.cs:15
msgctxt "verb"
msgid "Save"
msgstr "Speichern"

#: Assets/Scripts/GameUI.cs:16
msgctxt "noun"
msgid "Save"
msgstr "Speicherstand"

#: Assets/Scripts/GameUI.cs:20
msgid "1 life remaining"
msgid_plural "%d lives remaining"
msgstr[0] "1 Leben übrig"
msgstr[1] "%d Leben übrig"
```

#### Using Translation Tools

Professional tools can streamline this process:

- **[Poedit](https://poedit.net/)** — Cross-platform GUI editor (Windows/macOS/Linux)
- **[Lokalize](https://apps.kde.org/lokalize/)** — KDE translation tool
- **[Virtaal](http://virtaal.translatehouse.org/)** — Simple, lightweight editor
- **[GTranslator](https://wiki.gnome.org/Apps/Gtranslator)** — GNOME translation editor

These tools provide:
- Side-by-side source/translation view
- Fuzzy matching against translation memory
- Validation (missing translations, format string mismatches)
- Statistics (completion percentage)

![Poedit Screenshot](../assets/loca-poedit-screenshot.png)
*Editing PO files in Poedit*

### Step 6: Deploy PO Files

Move the finished PO files to Unity's `Assets/Resources/` folder:

```
Assets/
└── Resources/
    ├── en.po
    ├── en_ui.po
    ├── de.po
    ├── de_ui.po
    ├── fr.po
    └── fr_ui.po
```

> **Important:** PO files **must** be in a folder named `Resources` (case-sensitive). Unity loads them at runtime via `Resources.Load()`.

### Step 7: Generate Plural Rules (One-Time)

After adding PO files with `Plural-Forms:` headers, run:

**Menu**: `Tools > Loca > Generate Plural Rules`

This extracts the plural logic from all PO files and generates a C# class (`LocaPlurals.cs`) in your **Generated Assets Dir**:

**Generated Code** (`LocaPlurals.cs`):
```csharp
namespace GuiToolkit
{
    public static partial class LocaPlurals
    {
        static partial void GetPluralIdx(string _languageId, int _number, 
                                          ref int _numPluralForms, ref int _pluralIdx)
        {
            int nplurals = 2;
            int plural = 0;
            
            switch (_languageId)
            {
                case "en":
                    nplurals = 2;
                    plural = (_number != 1) ? 1 : 0;
                    break;
                
                case "de":
                    nplurals = 2;
                    plural = (_number != 1) ? 1 : 0;
                    break;
                
                case "pl":  // Polish has 3 forms!
                    nplurals = 3;
                    plural = (_number == 1) ? 0 
                           : ((_number % 10 >= 2 && _number % 10 <= 4 && (_number % 100 < 10 || _number % 100 >= 20)) ? 1 : 2);
                    break;
                
                default:  // Fallback to English rules
                    nplurals = 2;
                    plural = (_number != 1) ? 1 : 0;
                    break;
            }
            
            _numPluralForms = nplurals;
            _pluralIdx = plural;
        }
    }
}
```

> **Re-run this tool** whenever you add support for new languages with different plural rules.

### Step 8: Test

Change the language at runtime and verify translations load:

```csharp
using GuiToolkit;

public class LanguageSelector : MonoBehaviour
{
    public void SetEnglish() => LocaManager.Instance.ChangeLanguage("en");
    public void SetGerman() => LocaManager.Instance.ChangeLanguage("de");
    public void SetFrench() => LocaManager.Instance.ChangeLanguage("fr");
}
```

All `UiLocalizedTextMeshProUGUI` components automatically update when the language changes.

---

## PO File Format Reference

### Basic Entry (Singular)

```po
msgid "Hello"
msgstr "Hallo"
```

### Context (Disambiguation)

```po
msgctxt "greeting"
msgid "Hello"
msgstr "Hallo"

msgctxt "phone"
msgid "Hello"
msgstr "Hallo?"  # Different tone for phone greeting
```

**Lookup key**: `"greeting\u0004Hello"` (context + `\u0004` separator + msgid)

### Plural Forms

```po
msgid "One item"
msgid_plural "%d items"
msgstr[0] "Ein Gegenstand"
msgstr[1] "%d Gegenstände"
```

**Usage**:
```csharp
int count = 5;
string text = _n("One item", "%d items", count);
// Returns: "5 Gegenstände" (uses msgstr[1] because count != 1)
```

### Fuzzy Translations

Mark uncertain translations as fuzzy:

```po
#, fuzzy
msgid "Deprecated feature"
msgstr "Veraltete Funktion"
```

**Runtime behavior**: Fuzzy translations are **still used** but a warning is logged (if debug logging enabled).

### Comments

```po
#. Translator comment (for human translators)
#: Assets/Scripts/Menu.cs:42
#: Assets/Scripts/Dialog.cs:18
msgid "Cancel"
msgstr "Abbrechen"
```

- `#.` — **Translator comment**: Context for translators (not used at runtime)
- `#:` — **Source reference**: Where the key appears in code (auto-generated by extraction)
- `#,` — **Flags**: e.g., `fuzzy`, `c-format`, `no-c-format`

### Escape Sequences

```po
msgid "Line 1\nLine 2"
msgstr "Zeile 1\nZeile 2"

msgid "He said \"Hello\""
msgstr "Er sagte \"Hallo\""
```

**Supported escapes**:
- `\n` — Newline
- `\r` — Carriage return
- `\"` — Literal quote
- `\\` — Literal backslash

### Multiline Strings

```po
msgid ""
"This is a very long string that spans "
"multiple lines for readability."
msgstr ""
"Dies ist ein sehr langer String, der sich "
"über mehrere Zeilen erstreckt."
```

**Note:** Empty string on first line, then concatenated strings. The PO parser joins them automatically.

---

## Plural Forms Reference

Different languages have different plural rules. The `Plural-Forms:` header defines:
- **nplurals**: How many plural forms exist (1-6)
- **plural**: Formula to select form based on number `n`

### Common Plural Forms

| Language | nplurals | Plural Formula | Example |
|----------|----------|----------------|---------|
| **English** | 2 | `n != 1` | 1 item / 2 items |
| **German** | 2 | `n != 1` | 1 Artikel / 2 Artikel |
| **French** | 2 | `n > 1` | 1 élément / 2 éléments |
| **Russian** | 3 | `(n%10==1 && n%100!=11) ? 0 : (n%10>=2 && n%10<=4 && (n%100<10 \|\| n%100>=20)) ? 1 : 2` | 1 файл / 2 файла / 5 файлов |
| **Polish** | 3 | `(n==1) ? 0 : (n%10>=2 && n%10<=4 && (n%100<10 \|\| n%100>=20)) ? 1 : 2` | 1 plik / 2 pliki / 5 plików |
| **Czech** | 3 | `(n==1) ? 0 : (n>=2 && n<=4) ? 1 : 2` | 1 soubor / 2 soubory / 5 souborů |
| **Arabic** | 6 | Complex formula | 0 / 1 / 2 / 3-10 / 11-99 / 100+ |
| **Japanese** | 1 | `0` | 1冊 / 2冊 (no plural distinction) |

### Example PO Headers

**English**:
```po
"Plural-Forms: nplurals=2; plural=(n != 1);\n"
```

**Polish** (3 forms):
```po
"Plural-Forms: nplurals=3; plural=(n==1) ? 0 : (n%10>=2 && n%10<=4 && (n%100<10 || n%100>=20)) ? 1 : 2;\n"
```

**Arabic** (6 forms):
```po
"Plural-Forms: nplurals=6; plural=(n==0) ? 0 : (n==1) ? 1 : (n==2) ? 2 : (n%100>=3 && n%100<=10) ? 3 : (n%100>=11) ? 4 : 5;\n"
```

**Japanese** (no plurals):
```po
"Plural-Forms: nplurals=1; plural=0;\n"
```

For a comprehensive list, see: [GNU gettext Plural Forms](https://www.gnu.org/software/gettext/manual/html_node/Plural-forms.html)

---

## Version Control Best Practices

### What to Commit

✅ **Commit to Git:**
- `.pot` files (templates — master key list)
- `.po` files (translations)
- `LocaPlurals.cs` (generated plural rules)
- `uitk_loca_groups` (group list)

❌ **Do NOT commit:**
- POT metadata files (`.pot~`, `.po~` backups created by editors)
- `.DS_Store`, `Thumbs.db`

### Merge Conflicts

PO files are **text-based** and merge-friendly. If conflicts occur:

1. **Manual merge** — Edit the conflicting `msgstr` values
2. **Use tools** — Poedit can merge PO files: `Catalog > Update from POT File`
3. **Re-extract** — If POT changed significantly, re-run key extraction and manually merge translations

### Example `.gitignore`

```gitignore
# Ignore backup files from PO editors
*.po~
*.pot~
*.po.bak
*.pot.bak
```

---

## Updating Translations

When you add new strings or modify existing ones:

### 1. Extract Keys Again

**Menu**: `Tools > Loca > Process Loca Keys`

This **overwrites** the POT files with the current state of the codebase.

### 2. Update PO Files

Use a tool like Poedit to merge changes:
1. Open the existing `de.po` file in Poedit
2. Go to **Catalog > Update from POT File**
3. Select the new `default.pot`
4. Poedit will:
   - Add new entries (with empty `msgstr`)
   - Mark removed entries as obsolete (`#~`)
   - Preserve existing translations

**Manual merge** (without tools):
- Copy new entries from POT to PO files
- Translate the new entries
- Remove obsolete entries (lines starting with `#~`)

### 3. Re-run Plural Rules (If Needed)

If you added support for a new language with different plural rules:

**Menu**: `Tools > Loca > Generate Plural Rules`

---

## Advanced Topics

### Groups (Namespacing)

Use groups to organize translations into logical categories:

**Code**:
```csharp
string uiText = _("settings", "ui");
string itemName = _("sword", "items");
string audioClip = _("bgm_01", "audio");
```

**PO Files**:
```
Assets/Resources/
├── en.po          ← Default group
├── en_ui.po       ← "ui" group
├── en_items.po    ← "items" group
├── en_audio.po    ← "audio" group
├── de.po
├── de_ui.po
├── de_items.po
└── de_audio.po
```

**Benefits**:
- Split large PO files into smaller, manageable chunks
- Assign different translators to different groups
- Load only relevant translations (e.g., DLC content)

**Group List**: Managed in `Assets/Resources/uitk_loca_groups` (plain text, one group per line):
```
ui
items
audio
```

### Context Best Practices

Use `msgctxt` to disambiguate words with multiple meanings:

```csharp
// Verb: "Open file"
string openFile = pgettext("Open", "verb");

// Adjective: "Store is open"
string storeStatus = pgettext("Open", "adjective");

// Noun: "Participate in the open"
string tournament = pgettext("Open", "noun");
```

**PO file**:
```po
msgctxt "verb"
msgid "Open"
msgstr "Öffnen"

msgctxt "adjective"
msgid "Open"
msgstr "Geöffnet"

msgctxt "noun"
msgid "Open"
msgstr "Offenes Turnier"
```

Without context, translators cannot know which meaning is intended.

### Preserving Format Strings

When using `string.Format()` or interpolation:

**Code**:
```csharp
int level = player.Level;
string text = string.Format(_("Level: {0}"), level);
```

**PO file**:
```po
msgid "Level: {0}"
msgstr "Stufe: {0}"
```

> **Important:** Ensure placeholders (`{0}`, `%d`, `%s`) are preserved in translations. Tools like Poedit can validate this.

### Marking Fuzzy Translations

If unsure about a translation, mark it as fuzzy:

```po
#, fuzzy
msgid "Experimental feature"
msgstr "Experimentelle Funktion"
```

**Effect**: Translation is used, but a warning is logged (if debug enabled). This alerts QA that the translation needs review.

---

## Troubleshooting

### Translations Not Loading

**Problem**: Changed language but text doesn't update.

**Solutions**:
- Verify PO file is in `Assets/Resources/`
- Check filename matches language ID (`en.po` for `"en"`)
- Enable **Debug Loca** in Project Settings to see load logs
- Ensure `UiLocalizedTextMeshProUGUI` components have correct `LocaKey`

### Wrong Plural Form Used

**Problem**: Plural form incorrect (e.g., "1 items" instead of "1 item").

**Solutions**:
- Run **`Tools > Loca > Generate Plural Rules`** to regenerate `LocaPlurals.cs`
- Check `Plural-Forms:` header in PO file is correct for the language
- Verify `msgstr[0]`, `msgstr[1]`, etc. are filled in correctly

### Keys Not Extracted

**Problem**: Key exists in code but doesn't appear in POT file.

**Solutions**:
- Use gettext-style functions: `_()`, `__()`, `gettext()`, `pgettext()`, `ngettext()`
- Inherit from `LocaMonoBehaviour` to access these functions
- Ensure key is a **string literal**, not a variable: `_("key")` ✅, `_(variableKey)` ❌
- Run **`Tools > Loca > Process Loca Keys`** to regenerate POT

### Context Not Working

**Problem**: `pgettext()` always returns the same translation.

**Solutions**:
- Ensure `msgctxt "context"` line is **before** `msgid` in PO file
- Check that context parameter is not empty: `pgettext("key", "", null)` won't work
- Verify the composed key `"context\u0004key"` exists in the PO file

---

## External Resources

- **[GNU gettext Manual](https://www.gnu.org/software/gettext/manual/)** — Official documentation
- **[Poedit](https://poedit.net/)** — Popular PO file editor
- **[Plural Forms Calculator](http://docs.translatehouse.org/projects/localization-guide/en/latest/l10n/pluralforms.html)** — Generate plural formulas
- **[ISO 639-1 Language Codes](https://en.wikipedia.org/wiki/List_of_ISO_639-1_codes)** — Standard language IDs

---

## Next Steps

- **[Excel Integration](localization-excel.html)** — Use spreadsheets for collaborative translation
- **[Google Sheets](localization-google-sheets.html)** — Cloud-based translation workflow
- **[General Workflow](localization-workflow.html)** — Common patterns and best practices
