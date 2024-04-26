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
			Dictionary<Type, UiAbstractApplyStyleBase> dict = new();
			EditorUiUtility.FindAllComponentsInAllAssets<UiAbstractApplyStyleBase>(component =>
			{
				if (!dict.ContainsKey(component.SupportedMonoBehaviourType))
					dict.Add(component.SupportedMonoBehaviourType, component);
			});

			var thisUiMainStyleConfig = target as UiMainStyleConfig;
			var skins = thisUiMainStyleConfig.Skins;
			

			foreach (var skin in skins)
			{
			}





			foreach (var kv in dict)
			{
				var style = kv.Value;
Debug.Log($"Monobehaviour:{kv.Key} : {kv.Value.GetType().Name}");
				foreach (var skin in skins)
				{
					var styleTypes = skin.GetStyleTypes();

					if (!styleTypes.Contains(style.SupportedStyleType))
					{

					}
				}
			}
		}
	}
}
