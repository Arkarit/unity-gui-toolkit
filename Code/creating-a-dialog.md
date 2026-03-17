---
layout: default
title: Creating a Dialog
---

# Creating a Dialog

This guide walks through building a dialog from scratch: a `UiPanel` with a localized title, a message, and two buttons (OK / Cancel).

---

## What You'll Build

```
MyDialog (UiPanel)
├── Background (Image)
└── Content
    ├── Title (UiLocalizedTextMeshProUGUI)
    ├── Message (UiLocalizedTextMeshProUGUI)
    └── Buttons
        ├── OkButton (UiButton)
        └── CancelButton (UiButton)
```

The dialog class will:
- Show and hide with a fade animation
- Display a localized title and message (automatically updated when the language changes)
- Close itself when either button is clicked

---

## UiPanel vs UiView

Use **`UiPanel`** for dialogs that live inside an existing Canvas (e.g., a confirmation prompt, an alert box, or any non-full-screen overlay). `UiPanel` is lightweight — no Canvas or navigation-stack management.

Use **`UiView`** when the dialog needs its own Canvas, should participate in the navigation stack (push/pop), or needs to occlude other views as a full-screen layer. See the [API Reference](../Code/html/index.html) for details on `UiView`.

---

## Step 1: Create the Prefab Hierarchy

In the Unity Editor, inside an existing Canvas in your scene (or in a Prefabs folder):

1. **Create an empty GameObject** — name it `MyDialog`
2. **Add a `UiPanel` component** to it: *Add Component → Gui Toolkit → Ui Panel*
3. **Add a background Image**: Right-click `MyDialog` → *UI → Image*, name it `Background`. Set its color and stretch it to fill the panel.
4. **Add a `Content` child** (empty GameObject) inside `MyDialog` for layout.
5. Inside `Content`, create:
   - **Title text**: Right-click → *UI → Text - TextMeshPro*. Name it `Title`.
   - **Message text**: Same, name it `Message`.
   - **Buttons container**: Empty GameObject named `Buttons`.
6. Inside `Buttons`, create two buttons. For each:
   - Right-click → *UI → Button - TextMeshPro*, name them `OkButton` and `CancelButton`.
   - Delete the auto-created `Text (TMP)` child — we will replace it with a localized component below.
   - **Remove** the default `Button` component. Add a **`UiButton`** component instead: *Add Component → Gui Toolkit → Ui Button*.

> **Tip:** `UiButton` wraps Unity's `Button` and adds click-sound, animation, and the toolkit's listener-management pattern. It requires a `Button` component on the same GameObject — Unity adds it automatically as a dependency.

### Replace Button Labels with Localized Text

For each button, add a child text object:
1. Right-click the button → *UI → Text - TextMeshPro*, name it `Label`.
2. **Remove** the `TextMeshProUGUI` component.
3. **Add** a `UiLocalizedTextMeshProUGUI` component: *Add Component → UI → Localized Text Mesh Pro UGUI*.
4. In the inspector, set **Loca Key** to `"ok"` (or `"cancel"` for the second button). Leave **Group** empty to use the default group.

---

## Step 2: Replace Plain TMP with Localized Text

Do the same for the `Title` and `Message` objects created in Step 1:

1. **Remove** the `TextMeshProUGUI` component.
2. **Add** `UiLocalizedTextMeshProUGUI`.
3. Set **Loca Key**:
   - Title: `"my_dialog.title"`
   - Message: `"my_dialog.message"`

`UiLocalizedTextMeshProUGUI` extends `TextMeshProUGUI` directly, so all standard text settings (font, size, alignment, color) work as normal. The component subscribes to language-change events automatically — when the player switches language at runtime, all text updates without any extra code.

> **Other localization approaches:**  
> Instead of inspector-configured keys, you can set the key from code: `m_titleText.LocaKey = "my_dialog.title";`  
> You can also translate strings directly in C# code from a `UiPanel` subclass using the inherited `_("key")` method (see [gettext workflow](localization-gettext.html)) or load translations from [Excel / Google Sheets](localization-excel.html).

---

## Step 3: Write the Dialog Class

Create a new C# script `MyDialog.cs` in your project:

```csharp
using UnityEngine;
using GuiToolkit;

public class MyDialog : UiPanel
{
    [SerializeField] private UiButton m_okButton;
    [SerializeField] private UiButton m_cancelButton;

    protected override void Awake()
    {
        // Register button listeners BEFORE calling base.Awake().
        // The base class wires them on OnEnable and unwires on OnDisable automatically.
        AddOnEnableButtonListeners(
            (m_okButton,     OnOkClicked),
            (m_cancelButton, OnCancelClicked)
        );
        base.Awake();
    }

    private void OnOkClicked()
    {
        Debug.Log("OK clicked");
        Hide();
    }

    private void OnCancelClicked()
    {
        Debug.Log("Cancel clicked");
        Hide();
    }
}
```

Add the `MyDialog` component to the `MyDialog` root GameObject (*Add Component → Scripts → My Dialog*), then assign the `OkButton` and `CancelButton` references in the inspector.

### Why `AddOnEnableButtonListeners`?

Calling `AddOnEnableButtonListeners` before `base.Awake()` tells the base class to wire each `UiButton → UnityAction` pair every time the GameObject is enabled, and to unwire them on disable. This means:
- No memory leaks from stale listeners
- Buttons work correctly even if the dialog is shown/hidden multiple times
- You never call `AddListener` / `RemoveListener` manually

---

## Step 4: Add Localization Keys

The inspector keys you set in Step 2 (`"my_dialog.title"`, `"my_dialog.message"`, `"ok"`, `"cancel"`) must exist in your PO files.

### Option A: Mark Keys in Code and Extract

In any `LocaMonoBehaviour` subclass (which `UiPanel` is, through `UiThing`), use the inherited translation functions to mark keys for extraction:

```csharp
// Inside MyDialog or any LocaMonoBehaviour:
string title   = _("My Dialog Title");      // marks "My Dialog Title" for extraction
string message = _("Do you want to proceed?");
string ok      = _("OK");
string cancel  = _("Cancel");
```

Then run **Gui Toolkit → Localization → Process Loca Keys** to generate/update the POT file. The tool scans all `.cs` files for `_()`, `__()`, and `pgettext()` calls and writes them into the POT template.

See the [Gettext Workflow](localization-gettext.html) for the full extraction and PO-file-creation pipeline.

### Option B: Edit PO Files Directly

Open (or create) `Assets/Resources/en.po` and add the keys manually:

```po
msgid "my_dialog.title"
msgstr "My Dialog"

msgid "my_dialog.message"
msgstr "Do you want to proceed?"

msgid "ok"
msgstr "OK"

msgid "cancel"
msgstr "Cancel"
```

> **Note:** By convention, the `msgid` is the key and `msgstr` is the English text (or the translation for each language's file). For other languages, create `de.po`, `fr.po`, etc. and translate the `msgstr` values there.

### Option C: Google Sheets / Excel

If your project uses the Google Sheets or Excel integration, keys can be pushed and pulled through the `LocaExcelBridge` inspector. See the [Google Sheets guide](localization-google-sheets.html) and [Excel guide](localization-excel.html).

---

## Step 5: Show the Dialog

From any other script in your project:

```csharp
[SerializeField] private MyDialog m_dialogPrefab;

private void OpenDialog()
{
    // Instantiate from prefab and show
    MyDialog dialog = Instantiate(m_dialogPrefab, transform);
    dialog.Show();
}
```

To run code after the dialog finishes its show animation:

```csharp
dialog.Show(_onFinish: () => Debug.Log("Dialog is now fully visible"));
```

To show without animation (instant):

```csharp
dialog.Show(_instant: true);
```

### Reacting to Show / Hide Events

```csharp
dialog.EvOnEndShow.AddListener(OnDialogShown);
dialog.EvOnEndHide.AddListener(OnDialogHidden);

private void OnDialogShown(UiPanel _panel) { /* panel is now visible */ }
private void OnDialogHidden(UiPanel _panel) { /* panel is now hidden — safe to destroy */ }
```

---

## Step 6: Optional — Add a Show/Hide Animation

Assign a `UiSimpleAnimationBase` component to the **Simple Show Hide Animation** field on the `UiPanel`:

1. Add a `UiSimpleAnimation` component to the `MyDialog` root (or a child).
2. Configure its **Show Clip** and **Hide Clip** in the inspector (or use a built-in fade/scale preset).
3. Drag it into the `UiPanel → Simple Show Hide Animation` slot.

`Show()` and `Hide()` will now play the animation and call `_onFinish` after it completes. Without a reference in this slot, all show/hide transitions are instant.

---

## Complete Example

`MyDialog.cs` — full listing:

```csharp
using UnityEngine;
using GuiToolkit;

public class MyDialog : UiPanel
{
    [SerializeField] private UiLocalizedTextMeshProUGUI m_titleText;
    [SerializeField] private UiLocalizedTextMeshProUGUI m_messageText;
    [SerializeField] private UiButton m_okButton;
    [SerializeField] private UiButton m_cancelButton;

    // Called by outside code to configure the dialog before showing it.
    public void Configure(string _titleKey, string _messageKey)
    {
        m_titleText.LocaKey   = _titleKey;
        m_messageText.LocaKey = _messageKey;
    }

    protected override void Awake()
    {
        AddOnEnableButtonListeners(
            (m_okButton,     OnOkClicked),
            (m_cancelButton, OnCancelClicked)
        );
        base.Awake();
    }

    private void OnOkClicked()
    {
        Hide();
        // TODO: trigger your game logic here
    }

    private void OnCancelClicked()
    {
        Hide();
    }
}
```

Usage from another script:

```csharp
[SerializeField] private MyDialog m_dialogPrefab;

private void ShowConfirmation()
{
    MyDialog dialog = Instantiate(m_dialogPrefab, transform);
    dialog.Configure("my_dialog.title", "my_dialog.message");
    dialog.Show();
}
```

---

## Summary

| Step | What you did |
|------|-------------|
| 1 | Built the prefab hierarchy: `UiPanel` root → `UiLocalizedTextMeshProUGUI` texts → `UiButton` buttons |
| 2 | Replaced plain TMP components with `UiLocalizedTextMeshProUGUI` and set Loca Keys |
| 3 | Created `MyDialog : UiPanel`, registered buttons with `AddOnEnableButtonListeners` |
| 4 | Added keys to PO files (or extracted them via the gettext tool) |
| 5 | Instantiated the prefab and called `Show()` / `Hide()` |
| 6 | *(Optional)* Assigned a `UiSimpleAnimation` for animated transitions |
