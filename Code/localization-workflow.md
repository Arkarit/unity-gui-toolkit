---
layout: default
title: Localization - General Workflow
---

# General Localization Workflow

This page describes the common workflow patterns for using the localization system, regardless of which translation method you choose.

---

## Initial Setup

### 1. Configure UiToolkitConfiguration

Before using localization, configure the toolkit settings:

1. Navigate to **Edit > Project Settings > UI Toolkit**
2. Set **POT Path**: Directory where extracted keys will be saved (e.g., `Assets/Localization/`)
3. Set **Generated Assets Dir**: Where generated code (e.g., `LocaPlurals.cs`) is stored
4. Enable **Debug Loca** if you want detailed logging during development

![UiToolkitConfiguration Settings](../assets/loca-configuration.png)
*UI Toolkit configuration panel*

### 2. Mark Strings for Translation

Use the gettext-style functions in your code. Inherit from `LocaMonoBehaviour` for convenience:

```csharp
using GuiToolkit;

public class MainMenu : LocaMonoBehaviour
{
    void Start()
    {
        // Simple translation
        string title = _("Main Menu");
        
        // Mark for extraction without translating (useful for editor-only code)
        string editorLabel = __("Inspector Label");
        
        // Context-aware (when same word has different meanings)
        string fileOpen = pgettext("Open", "verb");  // "Open file"
        string storeOpen = pgettext("Open", "adjective");  // "Store is open"
        
        // Plural forms
        int score = 100;
        string scoreText = _n("You have %d point", "You have %d points", score);
    }
}
```

**Available Functions:**

| Function | Description | Example |
|----------|-------------|---------|
| `_(key)` | Simple translation | `_("Hello")` |
| `_(key, group)` | Translation from specific group | `_("hello", "ui")` |
| `gettext(key, group)` | Long form of `_()` | `gettext("Hello", "ui")` |
| `pgettext(key, context, group)` | Context-aware translation | `pgettext("Open", "verb", "ui")` |
| `_n(singular, plural, n)` | Plural translation | `_n("1 item", "%d items", count)` |
| `ngettext(singular, plural, n, group)` | Long form with group | `ngettext("1 item", "%d items", count, "ui")` |
| `__(key)` | Mark for extraction only (no runtime translation) | `__("Editor label")` |

> **Note:** All these functions ultimately call `LocaManager.Instance.Translate()`. The shortcuts are for convenience and gettext compatibility.

### 3. Set Up UI Components

For TextMeshPro text elements that should be localized:

#### Option A: UiLocalizedTextMeshProUGUI (Recommended)

Replace `TextMeshProUGUI` with `UiLocalizedTextMeshProUGUI`:

```csharp
using GuiToolkit;
using UnityEngine;

public class DynamicLabel : MonoBehaviour
{
    [SerializeField] private UiLocalizedTextMeshProUGUI m_label;
    
    void UpdateLabel(string locaKey)
    {
        // Setting the LocaKey automatically translates
        m_label.LocaKey = locaKey;
        
        // Or set via the text property (when AutoLocalize is enabled)
        m_label.text = "greeting.morning";  // Treated as localization key
    }
}
```

**Inspector Settings:**
- **Auto Localize**: Enable automatic translation
- **Group**: Which translation group to use (leave empty for default)
- **Loca Key**: The translation key (can be set via code or inspector)

![UiLocalizedTextMeshProUGUI Inspector](../assets/loca-component-inspector.png)
*Inspector for localized text component*

#### Option B: Manual Translation

For more control, translate manually and set the text:

```csharp
using GuiToolkit;
using TMPro;

public class CustomLabel : LocaMonoBehaviour
{
    [SerializeField] private TMP_Text m_label;
    
    protected override void OnLanguageChanged(string _languageId)
    {
        base.OnLanguageChanged(_languageId);
        
        // Update text when language changes
        m_label.text = _("greeting.morning");
    }
    
    void Start()
    {
        // Initial translation
        m_label.text = _("greeting.morning");
    }
}
```

> **Important:** If you manually translate, always override `OnLanguageChanged()` to refresh translations when the language switches.

---

## Extracting Translation Keys

After marking strings in code and UI components:

### 1. Run Key Extraction

**Menu:** `Tools > Loca > Process Loca Keys`

This scans:
- All C# scripts for `_()`, `__()`, `gettext()`, `pgettext()`, `ngettext()` calls
- Scene files and prefabs for `UiLocalizedTextMeshProUGUI` components
- All ScriptableObjects implementing `ILocaKeyProvider` (including `LocaJsonKeyProvider`)

**Output:** POT (Portable Object Template) files in your configured **POT Path**:
```
Assets/Localization/
├── default.pot        ← Keys with no group
├── ui.pot             ← Keys from "ui" group
└── items.pot          ← Keys from "items" group
```

### 2. Review Extracted Keys

Open the `.pot` files in a text editor. Each key has one or more `#:` source reference comments that show exactly where it comes from:

- **Prefabs & Scenes**: include the full **GameObject path** within the prefab/scene (e.g. `Canvas/Header/TitleLabel`)
- **C# scripts**: include the **first 30 characters of the source line** (stripped of indentation) so you can quickly find the call site

```pot
# SOME DESCRIPTIVE TITLE.
# Copyright (C) YEAR THE PACKAGE'S COPYRIGHT HOLDER
# This file is distributed under the same license as the PACKAGE package.
# FIRST AUTHOR <EMAIL@ADDRESS>, YEAR.
#
msgid ""
msgstr ""
"Content-Type: text/plain; charset=UTF-8\n"

#: Assets/Scripts/MainMenu.cs | string title = _("Main
msgid "Main Menu"
msgstr ""

#: Assets/Prefabs/HUD.prefab | Canvas/Header/TitleLabel
msgctxt "verb"
msgid "Open"
msgstr ""

#: Assets/Scripts/StatusBar.cs | scoreText = _n("You have
msgid "You have %d point"
msgid_plural "You have %d points"
msgstr[0] ""
msgstr[1] ""
```

> **Note:** POT files are **templates**. They contain the source keys but empty translations (`msgstr ""`). You'll create `.po` files from these for each language.

---

## Harvesting JSON File Keys

If your game loads data from JSON files at runtime and displays string values from them, use a **`LocaJsonKeyProvider`** to harvest those strings into a POT file.

1. **Right-click in Project window** → **Create > Loca > JSON Key Provider**
2. Name it descriptively (e.g., `TutorialJsonKeys`)
3. In the Inspector:
   - Add one or more **Entries**: each entry has a **JSON File** (TextAsset) and a list of **Field Names** to search for (e.g., `tutorialText`, `title`, `taskName`)
   - Set the **Group** (e.g., `Json`) — this determines which POT/PO file the keys go into
4. **Run `Tools > Loca > Process Loca Keys`** — keys are extracted into `loca_Json.pot`
5. Create corresponding PO files (`en_Json.po`, `de_Json.po`, etc.) and add translations
6. In your runtime code, pass the JSON values through `_()` with the matching group:
   ```csharp
   label.text = _(jsonData.tutorialText, "Json");
   ```

> **Note:** The `LocaJsonKeyProvider` is editor-only — it is compiled away in builds. The PO files it generates are what the runtime uses.

---

## Creating Translations

### Method 1: Manual PO File Creation

1. Copy `default.pot` to `en.po` (for English)
2. Fill in the `msgstr` values:

```po
msgid "Main Menu"
msgstr "Main Menu"

msgctxt "verb"
msgid "Open"
msgstr "Open"

msgid "You have %d point"
msgid_plural "You have %d points"
msgstr[0] "You have %d point"
msgstr[1] "You have %d points"
```

3. Copy `en.po` to `de.po` and translate to German:

```po
msgid "Main Menu"
msgstr "Hauptmenü"

msgctxt "verb"
msgid "Open"
msgstr "Öffnen"

msgid "You have %d point"
msgid_plural "You have %d points"
msgstr[0] "Du hast %d Punkt"
msgstr[1] "Du hast %d Punkte"
```

4. Place finished `.po` files in `Assets/Resources/`:
```
Assets/Resources/
├── en.po
├── en_ui.po
├── de.po
└── de_ui.po
```

See [PO Files Workflow](localization-gettext.html) for details.

### Method 2: Excel/Google Sheets

Use `LocaExcelBridge` to import from spreadsheets. Google Sheets also supports a code-driven workflow where new keys are pushed directly from PO files and translations are pulled back once filled in. See:
- [Excel Integration](localization-excel.html)
- [Google Sheets](localization-google-sheets.html)

---

## Merging POT Changes into PO Files

As your project evolves, new strings are added and old ones are removed. The **merge pipeline** keeps your PO translation files in sync with the POT template without losing existing translations.

### What the Merge Does

When you merge a POT into a PO file, the engine:

1. **Adds new keys** — Keys present in the POT but missing from the PO are added with an empty `msgstr`, ready for translation.
2. **Preserves existing translations** — Keys found in both files keep their current `msgstr` value intact.
3. **Marks removed keys as obsolete** — Keys in the PO that are no longer in the POT are prefixed with `#~` (obsolete) rather than deleted outright. Obsolete entries can be reviewed and cleaned up later.

### Auto-Merge

Enable **Auto-Merge** in **Edit > Project Settings > UI Toolkit** (`UiToolkitConfiguration.AutoMergePotToPo`). When enabled, the merge runs automatically every time the POT files are regenerated (e.g., after **`Tools > Loca > Process Loca Keys`**), keeping all PO files up to date with no extra steps.

**Auto-Sync to Google Sheets:** Enable **Auto-Sync After Merge** (`UiToolkitConfiguration.AutoSyncAfterMerge`) in the same settings panel. When enabled, every `LocaExcelBridge` with GoogleDocs + authentication configured will automatically have its new keys pushed to the linked Google Sheet immediately after the merge. Translators always see the latest keys without any manual step.

### Manual Merge

**Menu:** `Gui Toolkit > Localization > Merge POT into PO Files`

This merges every POT file (from the configured **POT Path**) into each matching PO file found in `Assets/Resources/`.

### SSoT Protection

PO files generated from a linked spreadsheet carry an **SSoT (Single Source of Truth) header**:

```
# Generated from Spreadsheet SSoT
# Bridge: MyBridge (GUID: ...)
# Source: https://...
# DO NOT EDIT MANUALLY — Changes will be overwritten.
```

Files with this header are **protected**: the merge pipeline skips them to avoid overwriting spreadsheet-managed content. To edit such a file manually, use the **"Make Local Copy"** action in the bridge inspector, which strips the SSoT header and detaches the file from the spreadsheet ("detached" state). A detached file can be freely edited and will participate in future merges normally.

---

## Runtime Language Switching

### Changing Language

```csharp
using GuiToolkit;

public class SettingsMenu : MonoBehaviour
{
    public void OnLanguageDropdownChanged(int index)
    {
        string[] languages = { "en", "de", "fr", "es" };
        string selectedLanguage = languages[index];
        
        // Change language
        bool success = LocaManager.Instance.ChangeLanguage(selectedLanguage);
        
        if (!success)
        {
            Debug.LogError($"Failed to load language: {selectedLanguage}");
        }
    }
}
```

**What Happens When Language Changes:**
1. `LocaManager` loads all `.po` files for the new language (e.g., `de.po`, `de_ui.po`)
2. All registered `ILocaProvider` instances call their `Load(languageId)` method
3. `UiEventDefinitions.EvLanguageChanged` event is broadcast
4. All `UiLocalizedTextMeshProUGUI` components and `LocaMonoBehaviour` subclasses receive `OnLanguageChanged()` callbacks
5. UI automatically updates with new translations

### Persisting Language Choice

The selected language is automatically saved to **PlayerPrefs** and restored on next launch:

```csharp
// The LocaManager handles this automatically
// Key: "PhoenixGrafik_Language"
```

To initialize with a specific language on first launch:

```csharp
void Awake()
{
    if (!PlayerPrefs.HasKey("PhoenixGrafik_Language"))
    {
        // First launch - detect system language or set default
        string systemLang = Application.systemLanguage.ToString().Substring(0, 2).ToLower();
        LocaManager.Instance.ChangeLanguage(systemLang);
    }
}
```

---

## Responding to Language Changes

### Automatic (Recommended)

Use `UiLocalizedTextMeshProUGUI` — no code needed. It automatically refreshes.

### Manual (Custom Components)

Inherit from `LocaMonoBehaviour` and override `OnLanguageChanged()`:

```csharp
public class CustomWidget : LocaMonoBehaviour
{
    [SerializeField] private Image m_icon;
    [SerializeField] private TMP_Text m_label;
    
    protected override void OnLanguageChanged(string _languageId)
    {
        base.OnLanguageChanged(_languageId);
        
        // Update text
        m_label.text = _("widget.label");
        
        // Load language-specific icon
        string iconPath = $"Icons/{_languageId}/widget_icon";
        m_icon.sprite = Resources.Load<Sprite>(iconPath);
    }
}
```

### Via Event Bus (No Inheritance Required)

```csharp
using GuiToolkit;
using UnityEngine;

public class StandaloneWidget : MonoBehaviour
{
    void OnEnable()
    {
        UiEventDefinitions.EvLanguageChanged.RegisterListener(OnLanguageChanged);
    }
    
    void OnDisable()
    {
        UiEventDefinitions.EvLanguageChanged.UnregisterListener(OnLanguageChanged);
    }
    
    void OnLanguageChanged(string languageId)
    {
        Debug.Log($"Language changed to: {languageId}");
        RefreshUI();
    }
    
    void RefreshUI()
    {
        // Update UI elements
    }
}
```

---

## Handling Missing Translations

### Fallback Behavior

By default, if a translation is not found, the **key itself** is returned:

```csharp
string missing = _("nonexistent.key");
// Returns: "nonexistent.key" (easy to spot in UI during testing)
```

You can customize this behavior:

```csharp
using GuiToolkit;

string result = LocaManager.Instance.Translate(
    _key: "missing.key",
    _group: null,
    _retValIfNotFound: LocaManager.RetValIfNotFound.EmptyString
);
// Returns: "" (empty string instead of key)
```

**Options:**
- `RetValIfNotFound.Key` — Return the key itself (default, best for debugging)
- `RetValIfNotFound.EmptyString` — Return empty string
- `RetValIfNotFound.Null` — Return null

### Development Mode ("dev" Language)

Load the special `"dev"` language to always return keys instead of translations:

```csharp
LocaManager.Instance.ChangeLanguage("dev");

string text = _("greeting");
// Always returns: "greeting" (even if English translation exists)
```

This is useful for:
- Identifying untranslated strings
- QA testing to verify all text uses localization keys
- Debugging translation issues

---

## Debugging Tips

### Enable Debug Logging

In **Project Settings > UI Toolkit**, enable **Debug Loca**. This logs:
- Which `.po` files are loaded
- Translation lookups (key → result)
- Missing keys
- Fuzzy translation warnings

### Check Available Languages

```csharp
// Current language
string current = LocaManager.Instance.Language;
Debug.Log($"Current language: {current}");

// Check if a key exists
bool exists = LocaManager.Instance.HasKey("main.menu", null);
Debug.Log($"Key exists: {exists}");
```

### Inspect Translation Sources

During development, you can query which providers are active:

```csharp
var manager = LocaManager.Instance as LocaManagerDefaultImpl;
if (manager != null)
{
    // Access to internal state for debugging
    // (Note: This requires casting to the implementation class)
}
```

### Common Issues

| Problem | Cause | Solution |
|---------|-------|----------|
| Keys not extracted | Code not using `_()` or `__()` | Use gettext-style functions |
| Translations not loading | PO file not in `Resources/` | Move `.po` files to `Assets/Resources/` |
| Wrong plural form used | Plural rules not generated | Run `Tools > Loca > Generate Plural Rules` |
| Context not working | Missing `msgctxt` in PO file | Add `msgctxt "context"` before `msgid` |
| UI not updating | Not overriding `OnLanguageChanged()` | Use `UiLocalizedTextMeshProUGUI` or implement callback |

---

## Best Practices

### ✅ DO:
- **Use descriptive keys**: `"menu.file.open"` instead of `"mfo"`
- **Namespace keys by context**: Avoid collisions between different parts of your app
- **Run extraction regularly**: Update POT files as you add new strings
- **Test with "dev" language**: Catch missing translations early
- **Use context for ambiguous words**: `pgettext("Open", "verb")` vs `pgettext("Open", "adjective")`
- **Keep keys in English**: Standard practice for gettext (msgid = English, msgstr = translation)

### ❌ DON'T:
- **Hardcode strings in UI**: Always use localization keys
- **Concatenate translated strings**: Word order varies by language
- **Use punctuation in keys**: Makes extraction fragile (e.g., `"Hello!"` vs `"Hello"`)
- **Forget plural forms**: Languages have complex plural rules (Polish has 3 forms!)
- **Modify POT files manually**: They're auto-generated; edit PO files instead

---

## Advanced Topics

### Custom Providers

For dynamic content (DLC, user mods, server-based translations), implement `ILocaProvider`. See [Custom Providers](localization-custom-providers.html) *(coming soon)*.

### Build-Time Validation

The toolkit includes a **LocaPreBuildProcessor** that validates translations before building:

```csharp
// Example: Warn if "dev" language is active in production build
#if !UNITY_EDITOR
if (LocaManager.Instance.Language == "dev")
{
    Debug.LogWarning("Dev language is active in production build!");
}
#endif
```

You can extend this to enforce stricter rules (e.g., require all keys to have translations).

---

## Next Steps

Choose your translation workflow:
- [PO Files Workflow](localization-gettext.html) — Traditional gettext with version control
- [Excel Integration](localization-excel.html) — Offline spreadsheet editing
- [Google Sheets](localization-google-sheets.html) — Cloud collaboration

Or dive into advanced topics:
- [Plural Forms Reference](localization-plurals.html) *(coming soon)*
- [Custom Providers](localization-custom-providers.html) *(coming soon)*
- [Migration Guide](localization-migration.html) *(coming soon)*
