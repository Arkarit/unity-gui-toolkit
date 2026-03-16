---
layout: default
title: Localization - Google Sheets
---

# Google Sheets Integration

This page describes how to use **Google Sheets** as a cloud-based translation source. This approach is ideal for:
- Remote collaboration with translators
- Translators without Unity access
- Real-time updates from non-technical team members
- Shared editing across multiple time zones

---

## Overview

The **LocaExcelBridge** component can import translations directly from Google Sheets URLs. It downloads the spreadsheet as Excel format (`.xlsx`), parses it, and provides translations at runtime.

![Google Sheets Workflow](../assets/loca-google-sheets-workflow.png)
*Translators edit Google Sheets; Unity imports the data*

### Advantages
✅ **Cloud-based** — No file transfers needed  
✅ **Real-time collaboration** — Multiple translators can edit simultaneously  
✅ **No Unity required** — Translators work in a familiar web interface  
✅ **Revision history** — Google Sheets tracks changes automatically  
✅ **Comments/notes** — Translators can leave comments for clarification

### Disadvantages
❌ **Internet required** — Cannot import offline  
❌ **Authentication complexity** — May require Google Service Account for private sheets  
❌ **Download timeout** — Large sheets may fail (60-second limit)  
❌ **No offline work** — If Google Sheets is down, you can't import

---

## Step-by-Step Guide

### Step 1: Create Google Sheet

1. Go to [Google Sheets](https://sheets.google.com/)
2. Create a new spreadsheet
3. Set up the structure:

| Key | English | German | French |
|-----|---------|--------|--------|
| menu.play | Play | Spielen | Jouer |
| menu.settings | Settings | Einstellungen | Paramètres |
| menu.quit | Quit | Beenden | Quitter |
| game.score | Score: %d | Punktzahl: %d | Score: %d |

**Guidelines**:
- **First row**: Headers (optional, can be skipped via "Start Row" setting)
- **First column**: Localization keys (msgid)
- **Subsequent columns**: Translations for each language
- **Formatting**: Keep it simple — bold headers are fine, but avoid complex formatting

### Step 2: Share the Sheet

#### Option A: Public Sheet (No Authentication)

1. Click **Share** button (top-right)
2. Change **General access** to **Anyone with the link** → **Viewer**
3. Click **Copy link**

![Google Sheets Sharing](../assets/loca-google-sheets-share-public.png)
*Make the sheet publicly viewable*

> **Warning**: Anyone with the link can view the sheet. Do not store sensitive data (passwords, secret keys, etc.).

#### Option B: Private Sheet (Requires Service Account)

Keep the sheet private and use Google Service Account authentication (see [Authentication Setup](#authentication-setup) below).

### Step 3: Create LocaExcelBridge Asset

1. **Right-click in Project window** → **Create > UI Toolkit > Loca Excel Bridge**
2. Name it descriptively (e.g., `GoogleSheetsTranslations`)

### Step 4: Configure the Bridge

Select the `LocaExcelBridge` asset and configure it:

![LocaExcelBridge Google Sheets Inspector](../assets/loca-google-sheets-inspector.png)
*Inspector settings for Google Sheets import*

#### Basic Settings

| Field | Description | Example |
|-------|-------------|---------|
| **Source Type** | Select **"GoogleDocs"** | `GoogleDocs` |
| **Google Docs URL** | Paste the Google Sheets URL | `https://docs.google.com/spreadsheets/d/1A2B3C4D5E6F7G8H9I0J/edit#gid=0` |
| **Use Google Auth** | Enable for private sheets (requires Service Account) | `false` (public) / `true` (private) |
| **Service Account JSON Path** | Path to Service Account JSON (if using auth) | `Assets/Localization/service-account.json` |
| **Group** | Translation group (leave empty for default) | `""` or `"ui"` |
| **Start Row** | First row containing data (0-based) | `1` (skip header row) |

#### Column Mapping

Configure columns exactly as you would for [local Excel files](localization-excel.html#step-3-configure-the-bridge):

| Column Index | Column Type | Language ID | Plural Form |
|--------------|-------------|-------------|-------------|
| 0 | Key | | -1 |
| 1 | LanguageTranslation | `en` | -1 |
| 2 | LanguageTranslation | `de` | -1 |
| 3 | LanguageTranslation | `fr` | -1 |

> **Tip:** Instead of configuring columns manually, use the **`[Create by PO]`** button in the **Columns** section of the bridge inspector. It reads the PO files on disk for the bridge's group and sets up columns automatically — including the Key column and per-language singular/plural columns. If a configuration already exists you will be asked to confirm before any new columns are appended.

### Step 5: Import Data

**Menu**: `Tools > Loca > Process Loca Providers`

This:
1. Downloads the Google Sheet as Excel (`.xlsx` format)
2. Parses the data according to column mappings
3. Stores the translations in the `LocaExcelBridge` asset

**Console log** (if successful):
```
LocaExcelBridge: Downloaded and loaded 3 entries from Google Sheets
```

**Timeout**: Download times out after **60 seconds**. For large sheets, consider splitting into multiple sheets.

### Step 6: Register the Provider

Move the `LocaExcelBridge` asset to a **Resources** folder:

```
Assets/
└── Resources/
    └── LocaProviders/
        └── GoogleSheetsTranslations.asset
```

Run **`Tools > Loca > Process Loca Providers`** to register it in `_locaProviders.json`.

### Step 7: Test

Run the game and change language:

```csharp
using GuiToolkit;

public class TestGoogleSheets : MonoBehaviour
{
    void Start()
    {
        LocaManager.Instance.ChangeLanguage("de");
        
        string play = LocaManager.Instance.Translate("menu.play");
        Debug.Log(play);  // Output: "Spielen"
    }
}
```

---

## Authentication Setup

For **private** Google Sheets, you need a **Google Service Account**:

### Step 1: Create Service Account

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project (or select an existing one)
3. Navigate to **APIs & Services > Credentials**
4. Click **Create Credentials > Service Account**
5. Fill in details:
   - **Service account name**: `unity-loca-importer`
   - **Service account ID**: (auto-generated)
   - **Role**: *None required for Sheets API*
6. Click **Done**

![Create Service Account](../assets/loca-google-service-account-create.png)
*Creating a Google Service Account*

### Step 2: Create JSON Key

1. Click on the newly created service account
2. Go to **Keys** tab
3. Click **Add Key > Create new key**
4. Select **JSON** format
5. Click **Create** — the JSON file is downloaded

**Save the JSON file** to your Unity project (e.g., `Assets/Localization/service-account.json`).

> **Security Warning**: Do **NOT** commit this JSON file to public repositories. Add it to `.gitignore`:
> ```
> # .gitignore
> **/service-account.json
> ```

### Step 3: Enable Google Sheets API

1. In Google Cloud Console, go to **APIs & Services > Library**
2. Search for **Google Sheets API**
3. Click **Enable**

![Enable Sheets API](../assets/loca-google-sheets-api-enable.png)
*Enable Google Sheets API*

### Step 4: Share Sheet with Service Account

1. Open your Google Sheet
2. Click **Share** button
3. Add the **service account email** (from JSON: `client_email` field)
   - Example: `unity-loca-importer@my-project-12345.iam.gserviceaccount.com`
4. Set permission to **Viewer**
5. Click **Send**

![Share with Service Account](../assets/loca-google-sheets-share-service-account.png)
*Grant the service account access*

### Step 5: Configure LocaExcelBridge

In the `LocaExcelBridge` Inspector:
- **Use Google Auth**: ✅ Enabled
- **Service Account JSON Path**: `Assets/Localization/service-account.json`

Now **`Tools > Loca > Process Loca Providers`** will authenticate and download the private sheet.

---

## Legacy Push (Overwrite)

> **Warning:** This section describes the **legacy full-overwrite push**. It replaces the entire sheet with the current in-memory translation data. For a safer, additive workflow that only appends missing keys without touching existing cells, see [Gettext-Driven Workflow (Code → Sheets)](#gettext-driven-workflow-code--sheets) below.

In addition to pulling translations **from** a Google Sheet, the toolkit can write translations back to the sheet. This is useful when you've made edits locally (e.g., to the PO files) and want to sync them back to the sheet for your translators.

### Prerequisites

- The Service Account must have **Editor** permission on the sheet (not just Viewer).
- **Use Authentication** must be enabled in the `LocaExcelBridge` inspector.

### Enabling Write Access

1. In [Google Cloud Console](https://console.cloud.google.com/), the **Google Sheets API** must be enabled with the `spreadsheets` scope (not `spreadsheets.readonly`).
2. In your Google Sheet, re-share with the service account email and set the permission to **Editor**.
3. In the `LocaExcelBridge` inspector, tick **Use Authentication** and set the **Service Account JSON Path**.

### Using the Push Button

Once authentication is configured and the service account has Editor access, a **Push** button becomes available in the **Push** section of the `LocaExcelBridge` inspector. Clicking it:

1. Serializes the current in-memory translation data to a sheet-compatible format.
2. Writes the data directly to the linked Google Sheet, **overwriting its entire contents**.

> **Warning:** Pushing **overwrites** the sheet content. Make sure translators have saved their work or that you are pushing intentionally. Consider using Google Sheets' revision history to recover from unintended overwrites.

> **Note on OAuth scope:** The standard read-only import uses the `spreadsheets.readonly` scope. Push operations require the full `spreadsheets` scope. The toolkit automatically requests the correct scope based on whether write operations are needed.

---

## Gettext-Driven Workflow (Code → Sheets)

This workflow connects code-level gettext strings directly to Google Sheets, letting developers and translators collaborate without any manual file transfers. Keys flow from code → PO files → Google Sheet; finished translations flow back from the sheet → PO files → runtime.

```
Developer writes _("key")
       ↓
Tools > Loca > Process Loca Keys   (generates POT → merges into PO)
       ↓
[Push new keys]  ───────────────→  Google Sheet  (translators fill in)
                                        ↓
[Pull from Sheets] ←────────────  Translations ready
       ↓
PO files updated  →  Runtime loads translations
```

### Requirements

All three of the following must be true for the **`[Push new keys]`** and **`[Pull from Sheets]`** buttons to be active:

- **Source Type** is set to **GoogleDocs**
- **Use Authentication** is enabled
- **Service Account JSON Path** is configured

### Step 1: Set Up PO Files

Make sure PO files for the bridge's group already exist in `Assets/Resources/`. If none exist yet, create stub files (even empty ones) for each language you support:

```
Assets/Resources/
├── en.po
├── de.po
└── fr.po
```

See [PO Files Workflow](localization-gettext.html) for details on PO file conventions.

### Step 2: Auto-Build Column Configuration

In the bridge inspector, click **`[Create by PO]`** in the **Columns** section. The tool:

- Reads all PO files on disk for the bridge's group
- Creates the **Key** column entry
- Adds a **LanguageTranslation** column for each language it finds (singular and plural forms where applicable)

If a column configuration already exists, a confirmation dialog lets you choose whether to keep existing columns and append new ones.

> **Note:** You only need to do this once per bridge (or when you add a new language).

### Step 3: Enable Auto-Sync (Optional but Recommended)

In **Edit > Project Settings > UI Toolkit**, enable **Auto-Sync After Merge** (`UiToolkitConfiguration.AutoSyncAfterMerge`).

When enabled, every `LocaExcelBridge` with GoogleDocs + authentication configured will automatically push new keys to its linked Google Sheet immediately after the POT→PO merge — so translators always see the latest keys with no extra steps.

### Step 4: Extract Keys and Push

Run **`Tools > Loca > Process Loca Keys`**. This:

1. Scans code and scenes, writes POT files
2. Merges POT → PO (if `AutoMergePotToPo` is enabled)
3. Pushes new keys to Google Sheets (if `AutoSyncAfterMerge` is enabled)

To push manually without running the full extraction, click **`[Push new keys]`** in the **Sync** section of the bridge inspector. The button:

- Reads all PO files for the bridge's group
- Fetches the current sheet content via the Sheets API
- **Appends only the rows whose keys are not already in the sheet** — existing rows are never touched

### Step 5: Translators Fill In the Sheet

Translators open the Google Sheet and fill in the translation columns. The key column is pre-populated; translation cells are left blank by the push step so translators know exactly what needs their attention.

### Step 6: Pull Translations Back

Click **`[Pull from Sheets]`** in the **Sync** section of the bridge inspector. The tool:

1. Downloads the spreadsheet (same path as the regular import)
2. Iterates over every row and every language column
3. For each `msgstr` that is **currently empty** in the local PO file, copies the sheet value in
4. **Never overwrites** a translation that already has content
5. Creates a timestamped backup of each PO file before writing

> **Note:** Only **Viewer** permission on the sheet is needed for `[Pull from Sheets]`. **Editor** permission is required for `[Push new keys]` (and the legacy full-overwrite push).

### Complete Workflow Summary

| Step | Action | Who |
|------|--------|-----|
| 1 | Write `_("key")` in C# | Developer |
| 2 | `Tools > Loca > Process Loca Keys` | Developer |
| 3 | (Auto) POT merged into PO files | Toolkit |
| 4 | (Auto) New keys pushed to Google Sheet | Toolkit |
| 5 | Fill in translations in Google Sheet | Translator |
| 6 | Click `[Pull from Sheets]` in bridge inspector | Developer |
| 7 | PO files updated, runtime loads translations | Toolkit |

---

## Advanced Configuration

### Multiple Sheets in One Document

Google Sheets documents can have multiple **sheets** (tabs). To import from a specific sheet:

1. Open the Google Sheets document
2. Click on the desired sheet tab
3. Copy the URL — it contains `#gid=XXXXXX` at the end
4. Use this URL in the `LocaExcelBridge`

**Example**:
```
https://docs.google.com/spreadsheets/d/1A2B3C4D5E6F7G8H9I0J/edit#gid=123456789
                                                                   ↑
                                                      This identifies the specific sheet
```

Each `LocaExcelBridge` can import from a different sheet within the same document.

### Plural Forms

Same as [Excel plural forms](localization-excel.html#plural-forms) — use multiple columns:

| Key | Key (plural) | EN (singular) | EN (plural 0) | EN (plural 1) | DE (singular) | DE (plural 0) | DE (plural 1) |
|-----|--------------|---------------|---------------|---------------|---------------|---------------|---------------|
| apple | apples | One apple | %d apple | %d apples | Ein Apfel | %d Apfel | %d Äpfel |

### Comments and Notes

Translators can use Google Sheets' **comment** feature to leave notes:

1. **Right-click a cell** → **Comment**
2. Type the note (e.g., "Is this a verb or noun?")
3. Click **Comment**

![Google Sheets Comments](../assets/loca-google-sheets-comments.png)
*Leave comments for clarification*

Comments are **not imported** — they're for translator reference only.

### Conditional Formatting

Use conditional formatting to highlight missing translations:

1. Select the translation columns
2. **Format > Conditional formatting**
3. **Format rules**: **Is empty**
4. **Formatting style**: Red background

![Conditional Formatting](../assets/loca-google-sheets-conditional-format.png)
*Highlight empty cells in red*

---

## Workflow Tips

### Translator Workflow

1. **Translator receives Google Sheets link** (view or edit access)
2. **Translator fills in translations** directly in the browser
3. **Translator leaves comments** for unclear keys
4. **Developer imports data** in Unity via **`Tools > Loca > Process Loca Providers`**
5. **Developer tests** and provides feedback
6. **Repeat until complete**

### Continuous Integration (CI)

For automated builds, you can:

1. **Store Service Account JSON as CI secret** (e.g., GitHub Secrets, GitLab CI Variables)
2. **Inject JSON at build time** into the Unity project
3. **Run import during build**: Call `LocaExcelBridge.CollectData()` via script
4. **Build with latest translations**

**Example Unity Editor script**:
```csharp
#if UNITY_EDITOR
using UnityEditor;
using GuiToolkit;

public static class LocaBuildHelper
{
    [MenuItem("Build/Import Translations")]
    public static void ImportTranslations()
    {
        var bridges = Resources.LoadAll<LocaExcelBridge>("LocaProviders");
        foreach (var bridge in bridges)
        {
            bridge.CollectData();
            EditorUtility.SetDirty(bridge);
        }
        AssetDatabase.SaveAssets();
    }
}
#endif
```

Call this before building:
```bash
# CI pipeline
unity -batchmode -executeMethod LocaBuildHelper.ImportTranslations -quit
unity -batchmode -buildWindows64Player build/game.exe -quit
```

### Version Control

**Do commit**:
- `LocaExcelBridge` asset (contains serialized translations after import)
- Google Sheets URL (in the asset)

**Do NOT commit**:
- Service Account JSON file (add to `.gitignore`)

**Alternative**: Use placeholder values in the asset and override at build time.

---

## Troubleshooting

### Sync Buttons Greyed Out

**Problem**: The `[Push new keys]` or `[Pull from Sheets]` buttons are disabled.

**Solutions**:
- **Source Type** — Must be set to **GoogleDocs** (not Local or other)
- **Use Authentication** — Must be ticked in the bridge inspector
- **Service Account JSON Path** — Must point to a valid service account JSON file

### Push Succeeds but No New Rows Appear

**Problem**: `[Push new keys]` completes without errors but the sheet looks unchanged.

**Solution**: This is expected behaviour — all keys from the PO files were already present in the sheet. No duplicate rows are appended. If you expected new rows, verify that the PO files contain entries not yet in the sheet.

### Download Fails

**Problem**: Error: `Failed to download Google Sheets`

**Solutions**:
- **Check internet connection**
- **Verify URL** — Must be a valid Google Sheets URL
- **Check sharing settings** — If public, ensure "Anyone with the link" can view
- **If using auth**, verify Service Account JSON path is correct
- **Enable Sheets API** in Google Cloud Console
- **Check timeout** — Large sheets may exceed 60-second limit; split into multiple sheets

### Authentication Fails

**Problem**: Error: `401 Unauthorized` or `403 Forbidden`

**Solutions**:
- **Service Account email** — Ensure the sheet is shared with the service account email
- **JSON file path** — Verify the path is correct (relative to project root)
- **JSON file format** — Ensure it's a valid Google Service Account JSON (not corrupted)
- **Sheets API enabled** — Check that Google Sheets API is enabled in Cloud Console
- **Permission level** — Service account needs at least **Viewer** access

### Wrong Sheet Imported

**Problem**: Importing from the wrong tab.

**Solutions**:
- Copy the URL **from the specific sheet tab** (should have `#gid=XXXXXX`)
- If URL has no `gid`, it defaults to the first sheet (leftmost tab)

### Translations Not Loading at Runtime

**Problem**: Translations imported successfully but don't appear in the game.

**Solutions**:
- **LocaExcelBridge in Resources** — Must be in a `Resources/` folder
- **Run `Tools > Loca > Process Loca Providers`** after moving to Resources
- **Check column mapping** — Language ID must match exactly (case-sensitive)
- **Enable Debug Loca** to see which providers are loaded

### Slow Import

**Problem**: Import takes a long time or times out.

**Solutions**:
- **Reduce sheet size** — Split large sheets into multiple documents
- **Remove unused columns** — Set Column Type to `Ignore` for columns you don't need
- **Simplify formatting** — Remove images, charts, and complex formatting
- **Increase timeout** *(requires code modification)*:
  ```csharp
  // In LocaExcelBridge.cs (if you have source access)
  private const int DOWNLOAD_TIMEOUT_SECONDS = 120;  // Default: 60
  ```

---

## Security Best Practices

### ✅ DO:
- **Use Service Accounts** for private sheets
- **Add JSON to `.gitignore`** — Never commit to public repos
- **Use CI secrets** for JSON in automated builds
- **Restrict Service Account permissions** — Grant only **Viewer** access when you only need to pull translations or run the regular import; upgrade to **Editor** only when you need `[Push new keys]` or the legacy full-overwrite push
- **Rotate keys periodically** — Delete old keys and create new ones

> **Note:** `[Pull from Sheets]` and the standard import (`Tools > Loca > Process Loca Providers`) require only **Viewer** permission. `[Push new keys]` and the legacy **Push** button require **Editor** permission.

### ❌ DON'T:
- **Store secrets in sheets** — No passwords, API keys, etc.
- **Commit Service Account JSON** to version control
- **Share JSON files** via email or chat
- **Grant Editor access** unless necessary — Viewer is sufficient for import and pull operations
- **Use personal Google accounts** for automation — Use Service Accounts

---

## Comparison: Google Sheets vs. Local Excel

| Feature | Google Sheets | Local Excel |
|---------|---------------|-------------|
| **Collaboration** | ✅ Real-time, multi-user | ❌ Manual file sharing |
| **Accessibility** | ✅ Web browser, no Unity needed | ⚠️ Requires Excel/LibreOffice |
| **Offline Support** | ❌ Internet required | ✅ Works offline |
| **Version History** | ✅ Automatic revision tracking | ⚠️ Manual backups |
| **Authentication** | ⚠️ May require Service Account | ✅ No setup needed |
| **Speed** | ⚠️ Download time (60s timeout) | ✅ Instant read |
| **Large Files** | ⚠️ May timeout | ✅ No limit |

**Recommendation**: Use **Google Sheets** for distributed teams and translators without Unity access. Use **Local Excel** for offline work or very large translation files.

---

## Example Google Sheets Template

Create a copy of this template to get started:

**[Localization Template (View Only)](https://docs.google.com/spreadsheets/d/EXAMPLE_ID)**

Columns:
- **A**: Key
- **B**: English
- **C**: German
- **D**: French
- **E**: Spanish
- **F**: Comments (set to `Ignore` in column mapping)

---

## Next Steps

- **[Excel Integration](localization-excel.html)** — Offline editing with local files
- **[PO Files Workflow](localization-gettext.html)** — Traditional gettext approach
- **[General Workflow](localization-workflow.html)** — Common patterns and best practices

---

## Additional Resources

- **[Google Sheets](https://sheets.google.com/)** — Create and edit spreadsheets
- **[Google Cloud Console](https://console.cloud.google.com/)** — Manage Service Accounts and APIs
- **[Google Sheets API Documentation](https://developers.google.com/sheets/api)** — Official API reference
- **[Service Account Best Practices](https://cloud.google.com/iam/docs/best-practices-service-accounts)** — Security guidelines
