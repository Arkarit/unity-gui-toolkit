using System;
using System.Collections.Generic;
using System.IO;
using GuiToolkit.Editor;
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
		private SerializedProperty m_parentProp;
		private UiStyleConfig m_thisUiStyleConfig;

		private static string s_filterString = string.Empty;
		private static readonly UiStyleEditorFilter s_filter = new();
		private static bool s_synchronizeFoldouts = false;
		private static ESortType s_sortType;


		public static UiStyleEditorFilter DisplayFilter => s_filter;
		public static bool SynchronizeFoldouts => s_synchronizeFoldouts;		
		public static ESortType SortType => s_sortType;

		/// <summary>
		/// Selects the config that holds a style and opens the inspector on that very row - the skin
		/// foldout, every group along its path, and the filter narrowed to it.
		///
		/// The point of the whole exercise: an inherited style is shown read-only where it is USED, and the
		/// place it can be changed is another asset with seventy styles in it. Finding the row by hand is
		/// the friction that makes people want the read-only lifted instead - so the answer is to remove
		/// the friction, not the protection.
		///
		/// The filter is set, not merely the foldouts opened: in a config of that size an opened row still
		/// has to be scrolled to. What was set stays visible in the Filter field, so clearing it is one
		/// click and nobody is left wondering why half their styles are gone.
		/// </summary>
		public static void Reveal( UiStyleConfig _config, UiSkin _skin, UiAbstractStyleBase _style )
		{
			if (_config == null || _style == null)
				return;

			s_filterString = _style.Alias;
			s_filter.Update(s_filterString);

			if (_skin != null)
			{
				// Through UiSkinDrawer, not through the base class by name: the foldout store is a static of
				// the GENERIC drawer, so there is one per type argument, and only the skin drawer's own is
				// the one these ids mean anything in.
				//
				// The skin group is keyed by the skin's name; the groups below it by their path.
				UiSkinDrawer.SetFoldoutOpen(_skin.Name, true);

				// Every group above the row: "Chip", then "Chip/Default". The last segment is the row's own
				// group, and opening it is what actually puts the values on screen.
				string path = string.Empty;
				foreach (var segment in _style.Alias.Split('/'))
				{
					path = string.IsNullOrEmpty(path) ? segment : path + "/" + segment;
					UiSkinDrawer.SetFoldoutOpen(UiSkinDrawer.StyleGroupFoldoutId(_skin.Name, path), true);
				}
			}

			// Heights were remembered for a differently filtered, differently folded list.
			PropertyDrawerView.ClearHeightCache();

			Selection.activeObject = _config;
			EditorGUIUtility.PingObject(_config);
		}

		protected virtual void OnEnable()
		{
			m_skinsProp = serializedObject.FindProperty("m_skins");
			m_currentSkinIdxProp = serializedObject.FindProperty("m_currentSkinIdx");
			m_parentProp = serializedObject.FindProperty("m_parent");
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

		/// <summary>
		/// The config this one builds on. Written through the SerializedProperty rather than through
		/// UiStyleConfig.Parent, so the invalidation that property does has to happen here as well: every
		/// applier has to resolve again, and the drawers have to forget the row heights they remembered.
		/// </summary>
		private void DrawParentField()
		{
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField
			(
				m_parentProp,
				new GUIContent
				(
					"Inherits from",
					"Another style config this one builds on. Only what is overridden here has to be stored;\n"
					+ "everything else is resolved through that config, matched by skin name and style name.\n"
					+ "Leave empty for a config that stands alone."
				)
			);

			if (EditorGUI.EndChangeCheck())
			{
				if (m_parentProp.objectReferenceValue == m_thisUiStyleConfig)
				{
					UiLog.LogError($"A style config cannot inherit from itself ('{m_thisUiStyleConfig.name}').");
					m_parentProp.objectReferenceValue = null;
				}

				serializedObject.ApplyModifiedProperties();
				PropertyDrawerView.ClearHeightCache();
				UiEventDefinitions.EvSkinChanged.InvokeAlways(0);
			}

			var parent = m_parentProp.objectReferenceValue as UiStyleConfig;
			if (parent == null || m_thisUiStyleConfig.NumSkins == 0)
				return;

			// The skin the popup above selects, not the first one. Reporting Skins[0] told a reassuring
			// story about a skin the reader was not looking at, and hid that the SELECTED one inherited
			// nothing at all - which is where the styles then went missing.
			var skin = m_thisUiStyleConfig.CurrentSkin ?? m_thisUiStyleConfig.Skins[0];
			int own = skin.Styles.Count;
			int effective = skin.EffectiveStyles.Count;

			var source = skin.ParentSkin;
			var headline = source != null
				? $"Skin '{skin.Name}' resolves {effective} styles: {own} of its own, "
					+ $"{effective - own} inherited from '{parent.name}' (skin '{source.Name}')."
				: $"Skin '{skin.Name}' resolves {effective} styles, all of its own: '{parent.name}' has no "
					+ $"skin '{skin.EffectiveInheritFromSkinName}', so this skin inherits nothing. Set "
					+ $"'Inherits skin from' on it to the skin it should build on.";

			EditorGUILayout.HelpBox
			(
				headline + "\n"
				+ "Inherited styles are listed read-only below and can be overridden per skin. "
				+ "Grey: this config's own. Blue: inherited. Yellow: inherited and overridden here.",
				source != null ? MessageType.Info : MessageType.Warning
			);
		}

		public override void OnInspectorGUI()
		{
			UiStyleEditorUtility.SelectSkinByPopup(m_thisUiStyleConfig);
			serializedObject.Update();
			DrawParentField();
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
				// The filter decides which rows are shown at all, so remembered row heights are void.
				GuiToolkit.Editor.PropertyDrawerView.ClearHeightCache();
			}
			s_sortType = (ESortType) EditorGUILayout.EnumPopup("Sort by", s_sortType);
			s_synchronizeFoldouts = EditorGUILayout.Toggle("Synchronize Foldouts", s_synchronizeFoldouts);

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Export to JSON"))
				if (ExportToJson())
				{
					GUIUtility.ExitGUI();
					return;
				}
			
			if (GUILayout.Button("Import from JSON"))
				if (ImportFromJson())
				{
					GUIUtility.ExitGUI();
					return;
				}
			
			EditorGUILayout.EndHorizontal();
			
			EditorGUILayout.Space(10);
			Draw();


			serializedObject.ApplyModifiedProperties();
		}

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
				UiLog.LogError($"Couldn't write file, reason: {e.Message}");
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
				EditorGeneralUtility.SetDirty(m_thisUiStyleConfig);
			}
			catch (Exception e)
			{
				UiLog.LogError($"Couldn't write file, reason: {e.Message}");
			}
			
			return true;
		}
	}
}
