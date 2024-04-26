using System;
using System.Collections.Generic;
using GuiToolkit.Style;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiMainStyleConfig), true)]
	/// <summary>
	/// 
	/// </summary>
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
				SyncStyles();

			serializedObject.ApplyModifiedProperties();
		}

		private void SyncStyles()
		{
			Dictionary<Type, UiAbstractApplyStyleBase> applianceComponentByType = new();
			EditorUiUtility.FindAllComponentsInAllAssets<UiAbstractApplyStyleBase>(component =>
			{
				if (!applianceComponentByType.ContainsKey(component.SupportedMonoBehaviourType))
					applianceComponentByType.Add(component.SupportedMonoBehaviourType, component);
			});

			var thisUiMainStyleConfig = target as UiMainStyleConfig;
			var skins = thisUiMainStyleConfig.Skins;

			bool configWasChanged = false;

			foreach (var skin in skins)
			{
				List<UiAbstractStyleBase> newStyles = new();

				foreach (var kv in applianceComponentByType)
				{
					var supportedMonoBehaviourType = kv.Key;
					var applyStyleComponent = kv.Value;

					if (skin.StyleBySupportedMonoBehaviour(supportedMonoBehaviourType) == null)
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
