using Codice.CM.Common;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Style.Editor
{
	[CustomEditor(typeof(UiStyleConfig), true)]
	public class UiStyleConfigEditor : UnityEditor.Editor
	{
		public enum ESortType
		{
			PathAscending,
			PathDescending,
			FlatPathAscending,
			FlatPathDescending,
			FlatTypeAscending,
			FlatTypeDescending,
		}

		private SerializedProperty m_skinsProp;
		private SerializedProperty m_currentSkinIdxProp;
		private UiStyleConfig m_thisUiStyleConfig;

		private static string s_filterString = string.Empty;
		private static readonly UiStyleEditorFilter s_filter = new();
		private static bool s_synchronizeFoldouts = false;
		private static ESortType s_sortType;
		private static bool s_toolsExpanded = true;
		private static string s_mergePath = Application.dataPath;


		public static UiStyleEditorFilter DisplayFilter => s_filter;
		public static bool SynchronizeFoldouts => s_synchronizeFoldouts;		
		public static ESortType SortType => s_sortType;

		protected virtual void OnEnable()
		{
			m_skinsProp = serializedObject.FindProperty("m_skins");
			m_currentSkinIdxProp = serializedObject.FindProperty("m_currentSkinIdx");
			m_thisUiStyleConfig = target as UiStyleConfig;
			Undo.undoRedoPerformed += OnUndoOrRedo;
		}

		protected void OnDisable()
		{
			Undo.undoRedoPerformed -= OnUndoOrRedo;
		}

		private void OnUndoOrRedo()
		{
			EditorApplication.delayCall += () => UiEventDefinitions.EvSkinChanged.InvokeAlways(0);
		}

		public override void OnInspectorGUI()
		{
			UiStyleEditorUtility.SelectSkinByPopup(m_thisUiStyleConfig);
			serializedObject.Update();
			string lastFilterString = s_filterString;
			s_filterString = EditorGUILayout.TextField
			(
				new GUIContent
				(
					"Filter", 
					  "Filter for skins and styles.\n"
					+ "It knows these filter keywords:\n"
					+ "skin: skins\n"
					+ "t: styles which support a specific class, e.g. Image"
				), 
				s_filterString
			);
			if (s_filterString != lastFilterString)
			{
				s_filter.Update(s_filterString);
			}
			s_sortType = (ESortType) EditorGUILayout.EnumPopup("Sort by", s_sortType);
			s_synchronizeFoldouts = EditorGUILayout.Toggle("Synchronize Foldouts", s_synchronizeFoldouts);

			EditorGUILayout.Space(10);
			s_toolsExpanded = EditorGUILayout.Foldout(s_toolsExpanded, "Tools");
			if (s_toolsExpanded)
			{
				EditorGUILayout.BeginHorizontal();
				Label("JSON tools");
				if (GUILayout.Button("Export to JSON"))
				{
					if (ExportToJson())
					{
						GUIUtility.ExitGUI();
						return;
					}
				}

				if (GUILayout.Button("Import from JSON"))
				{
					if (ImportFromJson())
					{
						GUIUtility.ExitGUI();
						return;
					}
				}

				EditorGUILayout.EndHorizontal();
				EditorGUILayout.Space(5);

				EditorGUILayout.BeginHorizontal();
				Label("Merge tools");
				if (GUILayout.Button("Merge into this UiStyleConfig"))
				{
					if (Merge())
					{
						GUIUtility.ExitGUI();
						return;
					}
				}

				EditorGUILayout.EndHorizontal();

			}

			EditorGUILayout.Space(10);

			Draw();


			serializedObject.ApplyModifiedProperties();
		}

		private bool Merge()
		{
			var path = EditorUtility.OpenFilePanel("Select Style Config to merge", s_mergePath, "asset");
			if (string.IsNullOrEmpty(path))
				return false;
			s_mergePath = Path.GetDirectoryName(path);
			var unityPath = EditorFileUtility.GetUnityPathInternal(path, false, true);
			var otherConfig = AssetDatabase.LoadAssetAtPath<UiStyleConfig>(unityPath);
			if (!otherConfig)
			{
				EditorUtility.DisplayDialog("Invalid asset",
					$"The file '{path}' seems not to be a {nameof(UiStyleConfig)} and can't be merged.", "Ok");
				return false;
			}

			if (otherConfig == m_thisUiStyleConfig)
			{
				EditorUtility.DisplayDialog("Invalid asset",
					$"Config can't be merged with itself.", "Ok");
				return false;

			}

			UiStyleConfigMerger.Merge(otherConfig, m_thisUiStyleConfig, UiStyleConfigMerger.MergeStyleOptions.Merge);
			return true;
		}

		private void Label(string _text) => GUILayout.Label(_text, EditorStyles.label, new []{GUILayout.Width(EditorGUIUtility.labelWidth)});
		private void Draw()
		{
			try
			{
				for (int i = 0; i < m_skinsProp.arraySize; i++)
				{
					var skinProp = m_skinsProp.GetArrayElementAtIndex(i);
					if (!DisplayFilter.HasSkin(skinProp.displayName))
						continue;

					EditorGUILayout.PropertyField(skinProp);
				}
			}
			catch
			{
				throw;
			}
		}

		// both _name and _copyFromName have to be the actual names and not aliases
		private string AddSkin(string _name, string _copyFromName)
		{
			if (m_thisUiStyleConfig.SkinNames.Contains(_name))
				return string.Empty;

			var newSkin = new UiSkin(m_thisUiStyleConfig, _name);

			UiSkin copyFrom = null;
			if (!string.IsNullOrEmpty(_copyFromName))
			{
				foreach (var skin in m_thisUiStyleConfig.Skins)
				{
					if (skin.Name == _copyFromName)
					{
						copyFrom = skin;
						break;
					}
				}
			}

			if (copyFrom != null)
			{
				foreach (var style in copyFrom.Styles)
				{
					var newStyle = style.DeepClone();
					newSkin.Styles.Add(newStyle);
				}
			}

			m_skinsProp.arraySize += 1;
			m_skinsProp.GetArrayElementAtIndex(m_skinsProp.arraySize - 1).boxedValue = newSkin;
			serializedObject.ApplyModifiedProperties();
			UiStyleConfig.SetDirty(m_thisUiStyleConfig);
			return _name;
		}
		
		// Json Import/Export.
		
		[Serializable]
		private class JsonHelper
		{
			public List<UiSkin> Skins = new();
		}
		
		private bool ExportToJson()
		{
			var path = EditorUtility.SaveFilePanel("Save UiStyleConfig JSON", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "UiStyleConfig", "json");
			if (string.IsNullOrEmpty(path))
				return false;
			
			var skins = m_thisUiStyleConfig.Skins;
			var jsonHelper = new JsonHelper()
			{
				Skins = skins
			};
			
			var jsonStr = UnityEngine.JsonUtility.ToJson(jsonHelper, true);
			
			try
			{
				File.WriteAllText(path, jsonStr);
			}
			catch(Exception e)
			{
				Debug.LogError($"Couldn't write file, reason: {e.Message}");
			}
			
			return true;
		}

		private bool ImportFromJson()
		{
			var path = EditorUtility.OpenFilePanel("Save UIStyleConfig JSON", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "json");
			if (string.IsNullOrEmpty(path))
				return false;

			try
			{
				var jsonStr = File.ReadAllText(path);
				var jsonHelper = UnityEngine.JsonUtility.FromJson<JsonHelper>(jsonStr);
				m_thisUiStyleConfig.Skins = jsonHelper.Skins;
				EditorUtility.SetDirty(m_thisUiStyleConfig);
			}
			catch (Exception e)
			{
				Debug.LogError($"Couldn't write file, reason: {e.Message}");
			}
			
			return true;
		}
	}
}
