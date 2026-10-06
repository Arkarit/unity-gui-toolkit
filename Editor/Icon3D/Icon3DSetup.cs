using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Project setup for 3D icons: the reserved layer and its validation.
	/// </summary>
	[EditorAware]
	public static class Icon3DSetup
	{
		private static readonly int[] s_msaaValues = { 1, 2, 4, 8 };
		private static readonly GUIContent[] s_msaaLabels = { new("Off"), new("2x"), new("4x"), new("8x") };

		/// <summary>Draws the 3D icon section of the Gui Toolkit configuration window.</summary>
		public static void DrawConfiguration( SerializedObject _config )
		{
			EditorGUILayout.LabelField("3D Icons", EditorStyles.boldLabel);

			var layerNameProp = _config.FindProperty("m_icon3DLayerName");
			EditorGUILayout.PropertyField(layerNameProp, new GUIContent("Layer", UiToolkitConfiguration.HELP_ICON3D_LAYER));

			string layerName = layerNameProp.stringValue;
			int layer = string.IsNullOrEmpty(layerName) ? -1 : LayerMask.NameToLayer(layerName);
			if (layer < 0)
			{
				EditorGUILayout.HelpBox($"Layer '{layerName}' does not exist. 3D icons fall back to layer 31 until it does.", MessageType.Warning);
				if (!string.IsNullOrEmpty(layerName) && GUILayout.Button($"Create layer '{layerName}'"))
					CreateLayer(layerName);
			}
			else if (GUILayout.Button(new GUIContent("Check scenes for objects on this layer",
				         "No scene object may use the 3D icon layer; the icon camera would render it into every icon.")))
			{
				ReportObjectsOnLayer(layer);
			}

			var msaaProp = _config.FindProperty("m_icon3DMsaaSamples");
			msaaProp.intValue = EditorGUILayout.IntPopup(new GUIContent("MSAA", UiToolkitConfiguration.HELP_ICON3D_MSAA),
				msaaProp.intValue, s_msaaLabels, s_msaaValues);

			EditorGUILayout.PropertyField(_config.FindProperty("m_icon3DRendersPerFrame"),
				new GUIContent("Renders per Frame", UiToolkitConfiguration.HELP_ICON3D_RENDERS_PER_FRAME));
			EditorGUILayout.PropertyField(_config.FindProperty("m_icon3DRenderMilliseconds"),
				new GUIContent("Render Milliseconds per Frame", UiToolkitConfiguration.HELP_ICON3D_RENDER_MILLISECONDS));
			EditorGUILayout.PropertyField(_config.FindProperty("m_icon3DDefaultPreset"),
				new GUIContent("Default Preset", UiToolkitConfiguration.HELP_ICON3D_DEFAULT_PRESET));
		}

		/// <summary>Adds a user layer with the given name in the first free slot. Returns the layer or -1.</summary>
		public static int CreateLayer( string _name )
		{
			int existing = LayerMask.NameToLayer(_name);
			if (existing >= 0)
				return existing;

			var tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
			if (tagManagerAsset == null || tagManagerAsset.Length == 0)
			{
				UiLog.LogError("TagManager asset not found");
				return -1;
			}

			var tagManager = new SerializedObject(tagManagerAsset[0]);
			var layers = tagManager.FindProperty("layers");

			// 0-7 are Unity's built-in layers
			for (int i = 8; i < layers.arraySize; i++)
			{
				var element = layers.GetArrayElementAtIndex(i);
				if (!string.IsNullOrEmpty(element.stringValue))
					continue;

				element.stringValue = _name;
				tagManager.ApplyModifiedPropertiesWithoutUndo();
				UiLog.Log($"Created layer '{_name}' at index {i}");
				return i;
			}

			UiLog.LogError($"No free layer slot for '{_name}'");
			return -1;
		}

		[MenuItem(StringConstants.MENU_HEADER + "3D Icons/Check Scenes for Objects on Icon Layer")]
		private static void CheckLayerMenu()
		{
			int layer = UiIcon3DRenderer.Layer;
			ReportObjectsOnLayer(layer);
		}

		/// <summary>Logs every object in the open scenes (and the open prefab stage) that uses the icon layer.</summary>
		public static int ReportObjectsOnLayer( int _layer )
		{
			var offenders = new List<GameObject>();
			for (int i = 0; i < EditorSceneManager.sceneCount; i++)
			{
				var scene = EditorSceneManager.GetSceneAt(i);
				if (!scene.isLoaded)
					continue;

				foreach (var root in scene.GetRootGameObjects())
					Collect(root.transform, _layer, offenders);
			}

			var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
			if (prefabStage != null)
				Collect(prefabStage.prefabContentsRoot.transform, _layer, offenders);

			string layerName = LayerMask.LayerToName(_layer);
			if (offenders.Count == 0)
			{
				UiLog.Log($"No scene object uses the 3D icon layer '{layerName}' ({_layer})");
				return 0;
			}

			foreach (var go in offenders)
				UiLog.LogWarning($"'{GetPath(go.transform)}' uses the 3D icon layer '{layerName}' and would appear in every 3D icon", go);

			return offenders.Count;
		}

		private static void Collect( Transform _transform, int _layer, List<GameObject> _result )
		{
			if ((_transform.gameObject.hideFlags & HideFlags.DontSave) != 0)
				return;   // e.g. the icon stage itself

			if (_transform.gameObject.layer == _layer)
				_result.Add(_transform.gameObject);

			foreach (Transform child in _transform)
				Collect(child, _layer, _result);
		}

		private static string GetPath( Transform _transform ) =>
			_transform.parent == null ? _transform.name : GetPath(_transform.parent) + "/" + _transform.name;
	}
}
