using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Stable identity of a toolkit "standard element" — the shared vocabulary read by dumb runtime code
	/// (UiMain), the screen-authoring catalog, and humans. The first two values are guaranteed stable no
	/// matter what is appended below, so serialized data never shifts as the enum grows:
	/// <list type="bullet">
	/// <item><see cref="None"/> — not a standard element.</item>
	/// <item><see cref="Custom"/> — a project-specific standard element; its identity is the
	/// <see cref="UiStandardElement.CustomId"/> string rather than an enum value.</item>
	/// </list>
	/// Every other value names a toolkit built-in. Client projects add their own standard elements via
	/// <see cref="Custom"/> + a string key (enums can't be extended), never by editing this enum. Values
	/// are only ever APPENDED here so existing serialized indices stay valid.
	/// </summary>
	public enum EStandardElement
	{
		None = 0,
		Custom = 1,

		// --- Toolkit built-ins. APPEND-ONLY — never insert or reorder (serialized indices must stay
		//     stable). A value's name matches its prefab; whether it is a standalone authoring building
		//     block or an internal sub-part is NOT encoded here — that is the per-prefab m_internal flag. ---

		// UiMain's functional prefab set (the original built-ins).
		StandardButton,
		OkButton,
		CancelButton,
		StandardButtonSmall,
		CloseButton,
		StandardIconButton,
		LanguageToggle,
		Requester,
		SettingsDialog,
		ToastMessageView,
		KeyPressRequester,
		GridPicker,
		PopupMenu,
		StartupOverlayView,

		// Containers / panels / decoration / overlays.
		StandardButtonBar,
		StandardPanelBackground,
		StandardPanelBackgroundWithHeadline,
		StandardHeadlineBackground,
		StandardColorPatch,
		HorizontalDecoLine,
		StandardClickCatcher,

		// Inputs.
		StandardCheckbox,
		StandardRadio,
		StandardSliderHor,
		UiDropdown,
		UiLanguageSelectDropdown,
		StandardInputField,

		// Tabs.
		StandardTab,
		StandardTabChapter,
		StandardTabPage,
		StandardTabPageWithScrollRect,

		// Directional buttons (variants of UpDownLeftRightButton).
		UpButton,
		DownButton,
		LeftButton,
		RightButton,

		// Text.
		StandardHeadline,
		StandardHeadline2ndOrder,
		StandardHeadline3rdOrder,
		StandardText,
		StandardTextSmall,

		// Dialogs.
		FullScreenTabDialog,

		// Date / time pickers.
		DatePicker,
		TimePicker,
		DateTimePanel,

		// Player-setting rows.
		PlayerSettingButton,
		PlayerSettingCheckbox,
		PlayerSettingDropdown,
		PlayerSettingKeyBinding,
		PlayerSettingLanguage,
		PlayerSettingLanguageDropdown,
		PlayerSettingRadiobutton,
		PlayerSettingSlider,
		PlayerSettingFPS,
		PlayerSettingText,
		BackgroundPlayerSetting,
		HoverPlayerSettings,

		// Internal sub-parts (tagged with m_internal = true; resolve but hidden from the vocabulary).
		StandardCheckboxCheckmark,
		UpDownLeftRightButton,
		GridPickerCell,
		GridPickerCellText,
		DialogStub,
		CalenderUI,
		DateDisplay,
		DateTimePartPanel,
		IncDecPickPicker,
		WiggleAnimation,

		// Display widgets. Appended here rather than next to the other inputs above because this enum
		// is append-only - the position in the list carries the serialized index, not the meaning.
		StandardChip,
		StandardCountIndicator,
	}

	/// <summary>
	/// Marks a prefab's root as a named <see cref="EStandardElement"/> — the single source of truth for
	/// "this prefab IS the OkButton (etc.)". A prefab variant inherits its base's tag automatically, so a
	/// client variant of a tagged toolkit prefab claims the same identity and, being a non-library asset,
	/// out-ranks the toolkit default when the registry is generated. Override the tag on a variant only to
	/// claim a DIFFERENT identity than its base.
	///
	/// Editor tooling scans these markers to generate the runtime <c>UiStandardElementRegistry</c> and to
	/// enrich the authoring catalog's palette.
	///
	/// This marker IS authorable, and for a generated screen it has to be: a bake rebuilds its prefab from
	/// the description, so a marker added to the asset afterwards is discarded on the next bake and the
	/// registry entry silently disappears with it. A screen that wants to be resolvable by identity at
	/// runtime therefore authors the marker on its root — <c>"components": ["UiStandardElement"]</c> plus
	/// <c>"props": { "element": "Custom", "customId": "…" }</c>. Keeping it out of the vocabulary also blinded
	/// read-back and edit preservation to markers, which is how they got lost unnoticed.
	/// </summary>
	[DisallowMultipleComponent]
	public class UiStandardElement : MonoBehaviour
	{
		[Tooltip("Which standard element this prefab represents. Use Custom + Custom Id for project-specific elements.")]
		[SerializeField] private EStandardElement m_element = EStandardElement.None;

		[Tooltip("Identity string used only when Element == Custom (project-specific standard elements).")]
		[SerializeField] private string m_customId = "";

		[Tooltip("Internal building block: a sub-part of a larger prefab, not a standalone screen element. " +
			"Still gets a registry identity (for library-internal use and variant resolution), but is excluded " +
			"from the screen-authoring vocabulary the AI composes from.")]
		[SerializeField] private bool m_internal = false;

		public EStandardElement Element => m_element;
		public string CustomId => m_customId;

		/// <summary>
		/// True for a sub-part of a larger prefab (e.g. a checkbox checkmark, a picker cell). Such elements
		/// still resolve through the registry but are hidden from the screen-authoring palette/catalog.
		/// </summary>
		public bool IsInternal => m_internal;

		/// <summary>The effective identity key: the enum name, or the custom id when Element == Custom.</summary>
		public string Key => m_element == EStandardElement.Custom ? m_customId : m_element.ToString();
	}
}
