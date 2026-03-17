---
layout: default
title: Setup
---

# Setup

This guide walks through installing the UI Toolkit in a client Unity project, including all required dependencies and the one-time DLL setup needed for Unity versions before Unity 6.

---

## Requirements

| Requirement | Details |
|---|---|
| **Unity** | 2022.3 LTS or newer |
| **TextMeshPro** | Included with Unity 2022.3 via `com.unity.ugui` — see [Step 3](#step-3-import-textmeshpro-essential-resources) |
| **Newtonsoft.Json** | `com.unity.nuget.newtonsoft-json` — see [Step 2](#step-2-install-newtonsoftjson) |

---

## Step 1: Add the Package

### Option A: UPM Git URL (Recommended)

Open `Packages/manifest.json` in your project root and add the following to the `"dependencies"` block:

```json
"de.phoenixgrafik.ui-toolkit": "https://github.com/Arkarit/unity-gui-toolkit.git#v-00-01-01"
```

Replace `#v-00-01-01` with the [release tag](https://github.com/Arkarit/unity-gui-toolkit/releases) you want to use.

Full example with all required dependencies:

```json
{
  "dependencies": {
    "de.phoenixgrafik.ui-toolkit": "https://github.com/Arkarit/unity-gui-toolkit.git#v-00-01-01",
    "com.unity.nuget.newtonsoft-json": "3.2.1",
    "com.unity.ugui": "2.0.0"
  }
}
```

After saving the file, Unity will download the package automatically and trigger a recompile.

### Option B: Sub-Repository in Assets

If you want the source available for debugging or customisation, clone the repo into any folder inside `Assets/`:

```bash
cd Assets
git clone https://github.com/Arkarit/unity-gui-toolkit.git ExternalPackages/unity-gui-toolkit
```

> **Tip:** Check out a specific release tag (`git checkout v-00-01-01`) rather than staying on `master` to avoid unexpected breakage.

---

## Step 2: Install Newtonsoft.Json

Newtonsoft.Json is a required dependency for the toolkit's runtime storage and serialization system. It must be present in your project.

**If your project already uses `com.unity.addressables` or another package that depends on Newtonsoft.Json**, it may already be available as a transitive dependency — check `Packages/packages-lock.json` for `com.unity.nuget.newtonsoft-json`.

**If it is not yet installed**, add it via `Packages/manifest.json`:

```json
"com.unity.nuget.newtonsoft-json": "3.2.1"
```

Or via the Package Manager UI:

1. **Window > Package Manager**
2. Click **+** → **Add package by name...**
3. Enter `com.unity.nuget.newtonsoft-json` and click **Add**

---

## Step 3: Import TextMeshPro Essential Resources

TextMeshPro is bundled with Unity 2022.3+ and does not require a separate package install. However, the TMP Essential Resources (fonts, shaders, default settings) must be imported once per project.

If you have not done this yet:

1. **Window > TextMeshPro > Import TMP Essential Resources**
2. Click **Import** in the dialog

This creates a `TextMesh Pro/` folder in your project's `Assets/` with the required default assets. Without this, any TextMeshPro-based component will show a warning in the console and render with no font.

---

## Step 4: DLL Setup for Unity &lt; 6

> **This step is only required for Unity versions before Unity 6 (version numbers below 6000).**  
> If you are on Unity 6 or newer, skip this step — Roslyn is built in.

The toolkit uses **Roslyn** (C# code analysis) for localization key extraction and **ExcelDataReader** for Excel import. In Unity 6+, Roslyn is natively available. In earlier versions, these libraries must be copied into your project as Editor-only plugins.

### Installing the DLL Bridge

After the package finishes importing, open the menu:

**`Gui Toolkit > Dlls > Install DLL Hack for Unity Version < 6`**

A confirmation dialog will appear. Click **Install**.

Unity will then:

1. Copy 8 DLLs from the package into `Assets/Plugins/Roslyn2022Hack/`:

   | DLL | Purpose |
   |-----|---------|
   | `Microsoft.CodeAnalysis.dll` | C# syntax tree analysis (Roslyn core) |
   | `Microsoft.CodeAnalysis.CSharp.dll` | C# compiler APIs |
   | `System.Collections.Immutable.dll` | Roslyn dependency |
   | `System.Reflection.Metadata.dll` | Roslyn dependency |
   | `System.Runtime.CompilerServices.Unsafe.dll` | Roslyn dependency |
   | `ExcelDataReader.dll` | Read `.xlsx` files |
   | `ExcelDataReader.DataSet.dll` | Convert Excel sheets to DataSet |
   | `System.Text.Encoding.CodePages.dll` | Legacy encoding support (CP1252 etc.) |

2. Create `Assets/Plugins/Roslyn2022Hack/Roslyn2022Hack.asmdef` — marks all DLLs as **Editor-only** (never included in player builds).

3. Add the scripting define `UITK_USE_ROSLYN` to all build target groups, enabling the toolkit's code-analysis features.

Unity will recompile after the DLLs are imported.

### What the DLL Bridge Enables

| Feature | Without DLL bridge | With DLL bridge |
|---------|--------------------|-----------------|
| Loca key extraction (`Tools > Loca > Process Loca Keys`) | ❌ | ✅ |
| Excel / XLSX import via `LocaExcelBridge` | ❌ | ✅ |
| Google Sheets sync (Push / Pull / Create by PO) | ❌ | ✅ |
| Runtime localization (PO file loading) | ✅ | ✅ |
| All other toolkit features | ✅ | ✅ |

### Removing the DLL Bridge

To undo the installation (e.g., when migrating to Unity 6):

**`Gui Toolkit > Dlls > Remove DLL Hack for Unity Version < 6`**

This deletes all copied DLLs, removes the `Roslyn2022Hack.asmdef`, and clears the `UITK_USE_ROSLYN` scripting define from all build targets.

---

## Step 5: Add UiMain to Your Scene

Every project using the toolkit needs a **UiMain** instance in its bootstrap scene. `UiMain` is the central singleton that manages views, dialogs, pooling, and navigation. It ships pre-configured with references to 13 standard prefabs (buttons, requesters, toast messages, settings dialog, popup menu, …). Wiring all of these by hand from scratch is tedious — **the recommended way is to start from a Prefab Variant**.

### Option A: Prefab Variant (Recommended)

1. In the **Project** window, navigate to  
   `Packages / UI Toolkit / Runtime / Prefabs / UIMain`
2. Select the `UIMain` prefab, then right-click it → **Create Variant** → choose a placement option:

   | Option | Where the variant is saved |
   |---|---|
   | **Mirror Package Hierarchy** | `Assets/PackageVariants/de.phoenixgrafik.ui-toolkit/Runtime/Prefabs/UIMain Variant.prefab` — mirrors the package folder structure; keeps things tidy when you have variants from multiple packages |
   | **Flat in Assets** | `Assets/UIMain Variant.prefab` — quick and simple |
   | **Select common Path** | Prompts for a folder once — use this for batches |
   | **Select each Path** | Prompts for each prefab individually |

   > **Note:** Unity disables the standard *Create → Prefab Variant* option for read-only package folders. The **Create Variant** submenu is a toolkit-provided workaround that creates a proper Prefab Variant in your `Assets/` folder.

3. Drag the newly created `UIMain Variant` into your bootstrap scene.

All standard element slots (buttons, requesters, toast view, settings dialog, popup menu, …) are already wired inside the variant. `UiMain` is immediately usable.

> **Why Variants for standard elements?**  
> Prefabs inside a UPM package are read-only — you cannot edit them directly (Unity even greys out the normal *Create → Prefab Variant* option for package folders). Creating a Prefab Variant via the **Create Variant** right-click menu places an editable copy in your `Assets/` folder. You can override any property (color, font, animation, prefab reference) while inheriting everything else from the original. Package updates automatically propagate to anything you have *not* overridden. Apply the same pattern to any standard element (buttons, dialogs, etc.) you want to customise.

### Customising the Variant

Open your `UIMain` variant and override only the slots you need:

| Field | Description |
|---|---|
| `m_standardButtonPrefab` | Default button used by factory methods |
| `m_okButtonPrefab` / `m_cancelButtonPrefab` | Buttons used in requesters |
| `m_requesterPrefab` | OK / Yes-No dialog |
| `m_toastMessageViewPrefab` | Toast notification overlay |
| `m_settingsDialogPrefab` | Player settings dialog |
| `m_popupMenuPrefab` | Context/popup menu |
| `m_gridPickerPrefab` | Grid picker dialog |
| `m_additionalViews` | Register your own `UiView` prefabs for use with `CreateView<T>()` |

To replace a standard element with your own look, create a Prefab Variant of that element (e.g., of `StandardButton`) and assign the variant to the corresponding slot in your `UIMain` variant.

### Option B: From Scratch

If you prefer full manual control:

1. Create an empty GameObject in your bootstrap scene (e.g., named `UiMain`)
2. Add the `UiMain` component: **Add Component → Gui Toolkit → Ui Main**
3. Assign a **Camera** reference
4. Manually assign every prefab slot listed in the table above

> **Tip:** If you access `UiMain.Instance` at startup, always guard with `UiMain.IsAwake` first, or use `UiMain.AfterAwake(action)` to defer until the singleton is ready.

---

## Step 6: Verify the Installation

After completing all steps, check that everything works:

### No Compile Errors

The project should compile cleanly. If you see errors, use the table below:

| Error message | Cause | Fix |
|---------------|-------|-----|
| `The type or namespace 'JsonConvert' could not be found` | Newtonsoft.Json not installed | Add `com.unity.nuget.newtonsoft-json` to `manifest.json` — see [Step 2](#step-2-install-newtonsoftjson) |
| `The type 'TMP_Text' could not be found` | TMP Essential Resources not imported | **Window > TextMeshPro > Import TMP Essential Resources** |
| `The type 'IExcelDataReader' could not be found` | Unity &lt; 6, DLL bridge not installed | Run **`Gui Toolkit > Dlls > Install DLL Hack for Unity Version < 6`** |
| Features requiring `UITK_USE_ROSLYN` are missing | DLL bridge not installed | Same as above |
| `Roslyn2022Hack` assembly reference errors | DLL bridge partially installed | Run **Remove** then **Install** again |

### Smoke Test

1. Enter Play Mode — check the **Console** for any errors
2. Confirm `UiMain` appears in the scene hierarchy and logs no errors on startup

---

See also: [Contributing](contributing.html)
