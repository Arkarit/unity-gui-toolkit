using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Editor tool that creates a pre-configured <see cref="UiLanguageSelectDropdown"/> prefab.
	/// Invoked via: Gui Toolkit > Localization > Misc > Create Language Select Dropdown Prefab…
	/// </summary>
	public static class UiLanguageSelectDropdownCreator
	{
		[MenuItem(StringConstants.LOCA_MISC_CREATE_LANGUAGE_DROPDOWN_MENU_NAME,
			priority = Constants.LOCA_MISC_CREATE_LANGUAGE_DROPDOWN_MENU_PRIORITY)]
		public static void CreatePrefab()
		{
			string savePath = EditorUtility.SaveFilePanelInProject(
				"Save Language Select Dropdown Prefab",
				"LanguageSelectDropdown",
				"prefab",
				"Choose a location inside your Assets folder to save the prefab."
			);

			if (string.IsNullOrEmpty(savePath))
				return;

			// Build the full TMP_Dropdown hierarchy using the same factory Unity uses for
			// GameObject > UI > Dropdown - TextMeshPro.
			var resources = new TMP_DefaultControls.Resources();

			// Load default TMP sprites if available (TMP Essentials package).
			resources.standard    = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
			resources.background  = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
			resources.inputField  = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd");
			resources.knob        = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
			resources.checkmark   = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
			resources.dropdown    = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd");
			resources.mask        = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd");

			GameObject go = TMP_DefaultControls.CreateDropdown(resources);
			go.name = "LanguageSelectDropdown";

			// Add the runtime language-selector component.
			go.AddComponent<UiLanguageSelectDropdown>();

			// Save as prefab asset and immediately clean up the transient scene object.
			PrefabUtility.SaveAsPrefabAsset(go, savePath);
			Object.DestroyImmediate(go);

			AssetDatabase.Refresh();
			Debug.Log($"[GuiToolkit] Language Select Dropdown prefab saved to {savePath}");

			// Ping the new asset in the Project window.
			EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(savePath));
		}
	}
}
