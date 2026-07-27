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

		// --- Toolkit built-ins (currently UiMain's functional prefab set). Palette standards get
		//     appended below as they are tagged. APPEND-ONLY — never insert or reorder. ---
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
	}

	/// <summary>
	/// Marks a prefab's root as a named <see cref="EStandardElement"/> — the single source of truth for
	/// "this prefab IS the OkButton (etc.)". A prefab variant inherits its base's tag automatically, so a
	/// client variant of a tagged toolkit prefab claims the same identity and, being a non-library asset,
	/// out-ranks the toolkit default when the registry is generated. Override the tag on a variant only to
	/// claim a DIFFERENT identity than its base.
	///
	/// Editor tooling scans these markers to generate the runtime <c>UiStandardElementRegistry</c> and to
	/// enrich the authoring catalog's palette. The marker itself is not authorable into a screen.
	/// </summary>
	[DisallowMultipleComponent]
	[UiNotAuthorable]
	public class UiStandardElement : MonoBehaviour
	{
		[Tooltip("Which standard element this prefab represents. Use Custom + Custom Id for project-specific elements.")]
		[SerializeField] private EStandardElement m_element = EStandardElement.None;

		[Tooltip("Identity string used only when Element == Custom (project-specific standard elements).")]
		[SerializeField] private string m_customId = "";

		public EStandardElement Element => m_element;
		public string CustomId => m_customId;

		/// <summary>The effective identity key: the enum name, or the custom id when Element == Custom.</summary>
		public string Key => m_element == EStandardElement.Custom ? m_customId : m_element.ToString();
	}
}
