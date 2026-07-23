using UnityEngine;
using UnityEngine.Serialization;

namespace GuiToolkit
{
	/// <summary>
	/// Holds the set of built-in prefabs that <see cref="UiMain"/> uses to create its standard
	/// widgets and dialogs (buttons, requester, toast, settings dialog, popup, …).
	///
	/// Split out of <see cref="UiMain"/> so a client project can keep its prefab set in a single
	/// asset and — via the "Create Default Prefabs Variants" tool in this asset's inspector —
	/// override the toolkit defaults with project-specific prefab variants. Assign an instance to
	/// <see cref="UiMain"/>; its entries then take precedence over UiMain's (deprecated) inline fields.
	///
	/// Field naming: every prefab field must end in "Prefab" — the variant-creation tool discovers
	/// them by that suffix (see the config's editor).
	/// </summary>
	[CreateAssetMenu(fileName = "UiPrefabConfig", menuName = StringConstants.MENU_HEADER + "Ui Prefab Config")]
	public class UiPrefabConfig : ScriptableObject
	{
		[Header("Buttons")]
		[SerializeField] private UiButton m_standardButtonPrefab;
		[SerializeField] private UiButton m_okButtonPrefab;
		[SerializeField] private UiButton m_cancelButtonPrefab;
		[SerializeField] private UiButton m_standardButtonSmallPrefab;
		[SerializeField] private UiLanguageToggle m_languageTogglePrefab;
		[SerializeField] private UiButton m_closeButtonPrefab;
		[SerializeField] private UiButton m_standardIconButtonPrefab;

		[Header("Dialogs & Views")]
		[SerializeField] private UiRequester m_requesterPrefab;
		[SerializeField] private UiPlayerSettingsDialog m_settingsDialogPrefab;
		[FormerlySerializedAs("m_splashMessagePrefab")]
		[SerializeField] private UiToastMessageView m_toastMessageViewPrefab;
		[SerializeField] private UiKeyPressRequester m_keyPressRequesterPrefab;
		[SerializeField] private UiGridPicker m_gridPickerPrefab;
		[SerializeField] private UiPopup m_popupMenuPrefab;

		[Tooltip("Optional startup-overlay host view, created automatically on startup by UiMain.")]
		[SerializeField] private UiStartupOverlayView m_startupOverlayViewPrefab;

		public UiButton StandardButtonPrefab => m_standardButtonPrefab;
		public UiButton OkButtonPrefab => m_okButtonPrefab;
		public UiButton CancelButtonPrefab => m_cancelButtonPrefab;
		public UiButton StandardButtonSmallPrefab => m_standardButtonSmallPrefab;
		public UiLanguageToggle LanguageTogglePrefab => m_languageTogglePrefab;
		public UiButton CloseButtonPrefab => m_closeButtonPrefab;
		public UiButton StandardIconButtonPrefab => m_standardIconButtonPrefab;
		public UiRequester RequesterPrefab => m_requesterPrefab;
		public UiPlayerSettingsDialog SettingsDialogPrefab => m_settingsDialogPrefab;
		public UiToastMessageView ToastMessageViewPrefab => m_toastMessageViewPrefab;
		public UiKeyPressRequester KeyPressRequesterPrefab => m_keyPressRequesterPrefab;
		public UiGridPicker GridPickerPrefab => m_gridPickerPrefab;
		public UiPopup PopupMenuPrefab => m_popupMenuPrefab;
		public UiStartupOverlayView StartupOverlayViewPrefab => m_startupOverlayViewPrefab;
	}
}
