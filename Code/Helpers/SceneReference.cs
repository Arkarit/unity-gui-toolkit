using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

// Based on SceneReference JohannesMP (2018-08-12)
// https://gist.github.com/JohannesMP/ec7d3f0bcf167dab3d0d3bb480e0e07b
// See ZZZ_Legal\SceneReference\LICENSE

namespace GuiToolkit
{

	/// <summary>
	/// A wrapper that provides the means to safely serialize Scene Asset References.
	/// </summary>
	[System.Serializable]
	public class SceneReference : ISerializationCallbackReceiver
	{
#if UNITY_EDITOR
		// What we use in editor to select the scene
		[SerializeField]
		private UnityEngine.Object m_sceneAsset = null;

		bool IsValidSceneAsset
		{
			get
			{
				if (m_sceneAsset == null)
					return false;
				return m_sceneAsset.GetType().Equals(typeof(SceneAsset));
			}
		}
#endif

		// This should only ever be set during serialization/deserialization!
		[SerializeField]
		private string m_scenePath = string.Empty;

		// Use this when you want to actually have the scene path
		public string ScenePath
		{
			get
			{
#if UNITY_EDITOR
				// In editor we always use the asset's path
				return GetScenePathFromAsset();
#else
            // At runtime we rely on the stored path value which we assume was serialized correctly at build time.
            // See OnBeforeSerialize and OnAfterDeserialize
            return m_scenePath;
#endif
			}
			set
			{
				m_scenePath = value;
#if UNITY_EDITOR
				m_sceneAsset = GetSceneAssetFromPath();
#endif
			}
		}

		public static implicit operator string( SceneReference sceneReference )
		{
			return sceneReference.ScenePath;
		}

		// Called to prepare this data for serialization. Stubbed out when not in editor.
		public void OnBeforeSerialize()
		{
#if UNITY_EDITOR
			HandleBeforeSerialize();
#endif
		}

		// Called to set up data for deserialization. Stubbed out when not in editor.
		public void OnAfterDeserialize()
		{
#if UNITY_EDITOR
			// We sadly cannot touch assetdatabase during serialization, so defer by a bit.
			EditorApplication.update += HandleAfterDeserialize;
#endif
		}



#if UNITY_EDITOR
		private SceneAsset GetSceneAssetFromPath()
		{
			if (string.IsNullOrEmpty(m_scenePath))
				return null;
			return AssetDatabase.LoadAssetAtPath<SceneAsset>(m_scenePath);
		}

		private string GetScenePathFromAsset()
		{
			if (m_sceneAsset == null)
				return string.Empty;
			return AssetDatabase.GetAssetPath(m_sceneAsset);
		}

		private void HandleBeforeSerialize()
		{
			// Asset is invalid but have Path to try and recover from
			if (IsValidSceneAsset == false && string.IsNullOrEmpty(m_scenePath) == false)
			{
				m_sceneAsset = GetSceneAssetFromPath();
				if (m_sceneAsset == null)
					m_scenePath = string.Empty;

				UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
			}
			// Asset takes precendence and overwrites Path
			else
			{
				m_scenePath = GetScenePathFromAsset();
			}
		}

		private void HandleAfterDeserialize()
		{
			EditorApplication.update -= HandleAfterDeserialize;
			// Asset is valid, don't do anything - Path will always be set based on it when it matters
			if (IsValidSceneAsset)
				return;

			// Asset is invalid but have path to try and recover from
			if (string.IsNullOrEmpty(m_scenePath) == false)
			{
				m_sceneAsset = GetSceneAssetFromPath();
				// No asset found, path was invalid. Make sure we don't carry over the old invalid path
				if (m_sceneAsset == null)
					m_scenePath = string.Empty;

				if (Application.isPlaying == false)
					UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
			}
		}
#endif
	}


#if UNITY_EDITOR
	/// <summary>
	/// Display a Scene Reference object in the editor.
	/// If scene is valid, provides basic buttons to interact with the scene's role in Build Settings.
	/// </summary>
	[CustomPropertyDrawer(typeof(SceneReference))]
	public class SceneReferencePropertyDrawer : PropertyDrawer
	{
		// The exact name of the asset Object variable in the SceneReference object
		const string SceneAssetPropertyString = "m_sceneAsset";
		// The exact name of  the scene Path variable in the SceneReference object
		const string ScenePathPropertyString = "m_scenePath";

		static readonly RectOffset boxPadding = EditorStyles.helpBox.padding;
		static readonly float padSize = 2f;
		static readonly float lineHeight = EditorGUIUtility.singleLineHeight;
		static readonly float paddedLine = lineHeight + padSize;
		static readonly float footerHeight = 10f;
		static readonly float numberIndent = 12f;

		/// <summary>
		/// Drawing the 'SceneReference' property
		/// </summary>
		public override void OnGUI( Rect position, SerializedProperty property, GUIContent label )
		{
			bool numberFound;
			GUIContent numberGUIContent = DrawUtils.GetNumberGUIContentFromLabel(label, out numberFound);

			var sceneAssetProperty = GetSceneAssetProperty(property);

			// Draw the Box Background
			position.height -= footerHeight;

			if (numberFound)
			{
				DrawUtils.DisplayVerticallyCenteredNumber(position, numberGUIContent);
				float numberIndentPer = numberGUIContent.text.Length * numberIndent;

				position.x += numberIndentPer;
				position.width -= numberIndentPer;
				label = new GUIContent("");
			}

			GUI.Box(EditorGUI.IndentedRect(position), GUIContent.none, EditorStyles.helpBox);
			position = boxPadding.Remove(position);

			position.height = lineHeight;

			// Draw the main Object field
			label.tooltip = "The actual Scene Asset reference.\nOn serialize this is also stored as the asset's path.";

			EditorGUI.BeginProperty(position, GUIContent.none, property);
			EditorGUI.BeginChangeCheck();
			int sceneControlID = GUIUtility.GetControlID(FocusType.Passive);
			UnityEngine.Object selectedObject = EditorGUI.ObjectField(position, label, sceneAssetProperty.objectReferenceValue, typeof(SceneAsset), false);
			BuildSettingsUtility.BuildScene buildScene = BuildSettingsUtility.GetBuildScene(selectedObject);

			if (EditorGUI.EndChangeCheck())
			{
				sceneAssetProperty.objectReferenceValue = selectedObject;

				// If no valid scene asset was selected, reset the stored path accordingly
				if (buildScene.scene == null)
					GetScenePathProperty(property).stringValue = string.Empty;
			}
			position.y += paddedLine;

			if (buildScene.assetGUID.Empty() == false)
			{
				// Draw the Build Settings Info of the selected Scene
				DrawSceneInfoGUI(position, buildScene, sceneControlID + 1);
			}

			EditorGUI.EndProperty();
		}

		/// <summary>
		/// Ensure that what we draw in OnGUI always has the room it needs
		/// </summary>
		public override float GetPropertyHeight( SerializedProperty property, GUIContent label )
		{
			int lines = 2;
			SerializedProperty sceneAssetProperty = GetSceneAssetProperty(property);
// 			if (sceneAssetProperty.objectReferenceValue == null)
// 				lines = 1;

			return boxPadding.vertical + lineHeight * lines + padSize * (lines - 1) + footerHeight;
		}

		/// <summary>
		/// Draws info box of the provided scene
		/// </summary>
		private void DrawSceneInfoGUI( Rect position, BuildSettingsUtility.BuildScene buildScene, int sceneControlID )
		{
			bool readOnly = BuildSettingsUtility.IsReadOnly();
			string readOnlyWarning = readOnly ? "\n\nWARNING: Build Settings is not checked out and so cannot be modified." : "";

			// Label Prefix
			GUIContent iconContent = new GUIContent();
			GUIContent labelContent = new GUIContent();

			bool mainScene = false;

			// Missing from build scenes
			if (buildScene.buildIndex == BuildSettingsUtility.SCENE_NOT_IN_BUILD)
			{
				iconContent = EditorGUIUtility.IconContent("d_winbtn_mac_close");
				labelContent.text = "NOT In Build";
				labelContent.tooltip = "This scene is NOT in build settings.\nIt will be NOT included in builds.";
			}
			// In build scenes and enabled
			else if (buildScene.scene.enabled)
			{
				iconContent = EditorGUIUtility.IconContent("d_winbtn_mac_max");
				mainScene = buildScene.buildIndex == 0;
				if (mainScene)
					labelContent.text = "Main Scene: 0";
				else
					labelContent.text = "BuildIndex: " + buildScene.buildIndex;
				labelContent.tooltip = "This scene is in build settings and ENABLED.\nIt will be included in builds." + readOnlyWarning;
			}
			// In build scenes and disabled
			else
			{
				iconContent = EditorGUIUtility.IconContent("d_winbtn_mac_min");
				labelContent.text = "Disabled";
				labelContent.tooltip = "This scene is in build settings and DISABLED.\nIt will be NOT included in builds.";
			}

			// Left status label
			using (new EditorGUI.DisabledScope(readOnly))
			{
				Rect labelRect = DrawUtils.GetLabelRect(position);
				Rect iconRect = labelRect;
				iconRect.width = iconContent.image.width + padSize;
				labelRect.width -= iconRect.width;
				labelRect.x += iconRect.width;
				EditorGUI.PrefixLabel(iconRect, sceneControlID, iconContent);
				EditorGUI.PrefixLabel(labelRect, sceneControlID, labelContent, mainScene ? EditorStyles.boldLabel : EditorStyles.label);
			}

			// Right context buttons
			Rect buttonRect = DrawUtils.GetFieldRect(position);
			buttonRect.width = (buttonRect.width) / 3;

			string tooltipMsg = "";
			using (new EditorGUI.DisabledScope(readOnly))
			{
				// NOT in build settings
				if (buildScene.buildIndex == BuildSettingsUtility.SCENE_NOT_IN_BUILD)
				{
					buttonRect.width *= 2;
					int addIndex = EditorBuildSettings.scenes.Length;
					tooltipMsg = "Add this scene to build settings. It will be appended to the end of the build scenes as buildIndex: " + addIndex + "." + readOnlyWarning;
					if (DrawUtils.ButtonHelper(buttonRect, "Add...", "Add (buildIndex " + addIndex + ")", EditorStyles.miniButtonLeft, tooltipMsg))
						BuildSettingsUtility.AddBuildScene(buildScene);
					buttonRect.width /= 2;
					buttonRect.x += buttonRect.width;
				}
				// In build settings
				else
				{
					bool isEnabled = buildScene.scene.enabled;
					string stateString = isEnabled ? "Disable" : "Enable";
					tooltipMsg = stateString + " this scene in build settings.\n" + (isEnabled ? "It will no longer be included in builds" : "It will be included in builds") + "." + readOnlyWarning;

					if (DrawUtils.ButtonHelper(buttonRect, stateString, stateString + " In Build", EditorStyles.miniButtonLeft, tooltipMsg))
						BuildSettingsUtility.SetBuildSceneState(buildScene, !isEnabled);
					buttonRect.x += buttonRect.width;

					tooltipMsg = "Completely remove this scene from build settings.\nYou will need to add it again for it to be included in builds!" + readOnlyWarning;
					if (DrawUtils.ButtonHelper(buttonRect, "Remove...", "Remove from Build", EditorStyles.miniButtonMid, tooltipMsg))
						BuildSettingsUtility.RemoveBuildScene(buildScene);
				}
			}

			buttonRect.x += buttonRect.width + 10;
			EditorGUI.BeginDisabledGroup(!buildScene.enabled);
			bool newSceneLoaded = DrawUtils.ToggleHelper(buttonRect, buildScene.loadedInEditor, "Loaded", "Loaded in Editor", EditorStyles.toggle);
			if (newSceneLoaded != buildScene.loadedInEditor)
			{
				if (newSceneLoaded)
				{
					EditorSceneManager.OpenScene(buildScene.assetPath, OpenSceneMode.Additive);
				}
				else
				{
					Scene scene = EditorSceneManager.GetSceneByPath(buildScene.assetPath);
					EditorSceneManager.CloseScene(scene, true);
				}
			}
			EditorGUI.EndDisabledGroup();

		}

		static SerializedProperty GetSceneAssetProperty( SerializedProperty property )
		{
			return property.FindPropertyRelative(SceneAssetPropertyString);
		}

		static SerializedProperty GetScenePathProperty( SerializedProperty property )
		{
			return property.FindPropertyRelative(ScenePathPropertyString);
		}

		private static class DrawUtils
		{
			/// <summary>
			/// Draw a GUI button, choosing between a short and a long button text based on if it fits
			/// </summary>
			static public bool ButtonHelper( Rect position, string msgShort, string msgLong, GUIStyle style, string tooltip = null )
			{
				GUIContent content = new GUIContent(msgLong);
				content.tooltip = tooltip;

				float longWidth = style.CalcSize(content).x;
				if (longWidth > position.width)
					content.text = msgShort;

				return GUI.Button(position, content, style);
			}

			/// <summary>
			/// Draw a GUI button, choosing between a short and a long button text based on if it fits
			/// </summary>
			static public bool ToggleHelper( Rect position, bool toggle, string msgShort, string msgLong, GUIStyle style, string tooltip = null )
			{
				GUIContent content = new GUIContent(msgLong);
				content.tooltip = tooltip;

				float longWidth = style.CalcSize(content).x;
				if (longWidth > position.width)
					content.text = msgShort;

				return GUI.Toggle(position, toggle, content, style);
			}

			/// <summary>
			/// Given a position rect, get its field portion
			/// </summary>
			static public Rect GetFieldRect( Rect position )
			{
				position.width -= EditorGUIUtility.labelWidth;
				position.x += EditorGUIUtility.labelWidth;
				return position;
			}
			/// <summary>
			/// Given a position rect, get its label portion
			/// </summary>
			static public Rect GetLabelRect( Rect position )
			{
				position.width = EditorGUIUtility.labelWidth - padSize;
				return position;
			}

			static public string GetNumberStringFromLabel( string label, out bool numberFound )
			{
				string s = label;
				s = string.Concat(s.ToArray().Reverse().TakeWhile(char.IsNumber).Reverse());
				numberFound = !string.IsNullOrEmpty(s);
				return numberFound ? s : label;
			}

			static public GUIContent GetNumberGUIContentFromLabel( GUIContent label, out bool numberFound )
			{
				return new GUIContent(GetNumberStringFromLabel(label.text, out numberFound));
			}

			static public void DisplayVerticallyCenteredNumber( Rect position, GUIContent numberGUIContent )
			{
				var centeredStyle = GUI.skin.GetStyle("Label");
				centeredStyle.alignment = TextAnchor.MiddleLeft;
				EditorGUI.LabelField(position, numberGUIContent, centeredStyle);
			}
		}

	}

#endif
}