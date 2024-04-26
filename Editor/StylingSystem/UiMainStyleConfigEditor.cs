using System;
using System.Collections.Generic;
using GuiToolkit.Style;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiMainStyleConfig), true)]
	public class UiMainStyleConfigEditor : UnityEditor.Editor
	{
		protected virtual void OnEnable()
		{
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			DrawDefaultInspector();

			if (GUILayout.Button("Sync Styles"))
				SyncStyles(false);
			if (GUILayout.Button("Replace Styles"))
				SyncStyles(true);

			serializedObject.ApplyModifiedProperties();
		}

		private void SyncStyles(bool reset)
		{
			Dictionary<int, UiAbstractApplyStyleBase> applianceComponentByKey = new();
			EditorUiUtility.FindAllComponentsInAllAssets<UiAbstractApplyStyleBase>(component =>
			{
				if (!applianceComponentByKey.ContainsKey(component.Key))
					applianceComponentByKey.Add(component.Key, component);
			});

			var thisUiMainStyleConfig = target as UiMainStyleConfig;
			var skins = thisUiMainStyleConfig.Skins;

			bool configWasChanged = false;

			foreach (var skin in skins)
			{
				if (reset)
					skin.Styles.Clear();

				List<UiAbstractStyleBase> newStyles = new();

				foreach (var kv in applianceComponentByKey)
				{
					var key = kv.Key;
					var applyStyleComponent = kv.Value;

					if (skin.StyleByKey(key) == null)
					{
						var newStyle = applyStyleComponent.CreateStyle();
						newStyles.Add(newStyle);
					}
				}

				if (newStyles.Count > 0)
				{
					skin.Styles.AddRange(newStyles);
					configWasChanged = true;
				}
			}

			if (configWasChanged)
			{
				UiMainStyleConfig.EditorSave(thisUiMainStyleConfig);
			}
		}
	}
}
