#if UNITY_EDITOR
using GuiToolkit.Style;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor.Tools
{
	public static class ConvertUiMainStyleConfig
	{
		[MenuItem(StringConstants.CONVERT_MAIN_STYLE_CONFIG)]
		private static void MigrateSelected()
		{
			Object selected = Selection.activeObject;
			if (!selected)
				return;

			string path = AssetDatabase.GetAssetPath(selected);
			if (string.IsNullOrEmpty(path))
				return;

			var oldObj = selected as UiMainStyleConfig;
			if (!oldObj)
				return;

			UiStyleConfig newObj = ScriptableObject.CreateInstance<UiStyleConfig>();

			// Copies all serialized fields Unity knows about (including managed references).
			EditorUtility.CopySerialized(oldObj, newObj);

			// Keep name consistent.
			newObj.name = oldObj.name;

			AssetDatabase.StartAssetEditing();
			try
			{
				AssetDatabase.DeleteAsset(path);
				AssetDatabase.CreateAsset(newObj, path);
				AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
				AssetDatabase.SaveAssets();
			}
			finally
			{
				AssetDatabase.StopAssetEditing();
			}

			EditorUtility.FocusProjectWindow();
			Selection.activeObject = newObj;
		}
	}
}

#endif
