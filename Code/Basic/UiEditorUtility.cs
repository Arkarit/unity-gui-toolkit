// Note: don't move this file to an Editor folder, since it needs to be available
// for inplace editor code
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;

namespace GuiToolkit
{
	//Note: This file must reside outside of an "Editor" folder, since it must be accessible
	// from mixed game/editor classes (even though all accesses are in #if UNITY_EDITOR clauses)
	// See https://answers.unity.com/questions/426184/acces-script-in-the-editor-folder.html for reasons.
	public static class UiEditorUtility
	{
		public const int SKIP_LINE_SPACE = -20;
		public const int LARGE_SPACE_HEIGHT = 20;
		public const int SMALL_SPACE_HEIGHT = 10;
		public const int LARGE_BUTTON_HEIGHT = 40;
		public const int MEDIUM_POPUP_HEIGHT = 30;
		public const int NORMAL_POPUP_HEIGHT = 20;
		public const int LARGE_POPUP_HEIGHT = LARGE_BUTTON_HEIGHT;

		public const int LABEL_WIDTH = 120;

		public static GUIStyle Italic
		{
			get
			{
				GUIStyle result = new GUIStyle(GUI.skin.label);
				result.fontStyle = FontStyle.Italic;
				return result;
			}
		}

		public delegate void DelegateButtonAction();

		public static void Button( string _labelText, string _buttonText, DelegateButtonAction _delegate, int _height = NORMAL_POPUP_HEIGHT )
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(_labelText, GUILayout.Width(EditorGUIUtility.labelWidth));
			if (GUILayout.Button(_buttonText, GUILayout.Height(_height)))
				_delegate();
			EditorGUILayout.EndHorizontal();
		}

		public static void Button( Rect _rect, string _labelText, string _buttonText, DelegateButtonAction _delegate, int _height = NORMAL_POPUP_HEIGHT )
		{
			EditorGUI.PrefixLabel(_rect, GUIUtility.GetControlID(FocusType.Passive), new GUIContent( _labelText ));
			_rect.x += EditorGUIUtility.labelWidth;
			_rect.width -= EditorGUIUtility.labelWidth;
			if (GUI.Button(_rect, _buttonText))
				_delegate();
		}

		public static void PropertyField( string _labelText, SerializedProperty _prop, bool _noPropertyLabel = false )
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(_labelText, GUILayout.Width(EditorGUIUtility.labelWidth));
			EditorGUIUtility.labelWidth = LABEL_WIDTH;
			if (_noPropertyLabel)
				EditorGUILayout.PropertyField(_prop, new GUIContent(""), true);
			else
				EditorGUILayout.PropertyField(_prop, true);
			EditorGUILayout.EndHorizontal();
		}

		public static void PropertyField( string _labelText, string _propertyLabelText, SerializedProperty _prop )
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(_labelText, GUILayout.Width(EditorGUIUtility.labelWidth));
			GUILayout.Label(_propertyLabelText, GUILayout.Width(LABEL_WIDTH));
			EditorGUILayout.PropertyField(_prop, new GUIContent(""), true);
			EditorGUILayout.EndHorizontal();
		}

		public static bool Toggle( string _labelText, string _buttonText, ref bool _bool )
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUIUtility.labelWidth = EditorGUIUtility.labelWidth;
			GUILayout.Label(_labelText, GUILayout.Width(EditorGUIUtility.labelWidth));
			EditorGUIUtility.labelWidth = LABEL_WIDTH;
			bool current = _bool;
			_bool = GUILayout.Toggle(_bool, _buttonText);
			EditorGUILayout.EndHorizontal();
			return current != _bool;
		}

		public static void Label( string _labelText, string _labelText2 )
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUIUtility.labelWidth = EditorGUIUtility.labelWidth;
			GUILayout.Label(_labelText, GUILayout.Width(EditorGUIUtility.labelWidth));
			EditorGUIUtility.labelWidth = LABEL_WIDTH;
			GUILayout.Label(_labelText2);
			EditorGUILayout.EndHorizontal();
		}

		public static void LabelItalic( string _labelText, string _labelText2 )
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUIUtility.labelWidth = EditorGUIUtility.labelWidth;
			GUILayout.Label(_labelText, Italic, GUILayout.Width(EditorGUIUtility.labelWidth));
			EditorGUIUtility.labelWidth = LABEL_WIDTH;
			GUILayout.Label(_labelText2, Italic);
			EditorGUILayout.EndHorizontal();
		}

		public static bool EnumPopup<T>( string _labelText, ref T _val, string _labelText2 = "" ) where T : System.Enum
		{
			string[] types = Enum.GetNames(typeof(T));
			T[] tvalues = (T[])Enum.GetValues(typeof(T));
			T currentVal = _val;
			int currentIdx = Array.FindIndex<T>(tvalues, v => (int)(object)v == (int)(object)currentVal);

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(_labelText, GUILayout.Width(EditorGUIUtility.labelWidth));
			if (!string.IsNullOrEmpty(_labelText2))
				GUILayout.Label(_labelText2, GUILayout.Width(LABEL_WIDTH));
			int newIdx = EditorGUILayout.Popup(currentIdx, types);
			EditorGUILayout.EndHorizontal();

			bool result = currentIdx != newIdx;

			_val = tvalues[newIdx];
			return result;
		}

		public static bool EnumPopup<T>( string _labelText, ref SerializedProperty _prop, string _labelText2 = "" ) where T : System.Enum
		{
			T val = (T)(object)_prop.intValue;
			bool result = EnumPopup(_labelText, ref val, _labelText2);
			_prop.intValue = (int)(object)val;
			return result;
		}

		public static bool StringPopup( string _labelText, string[] _strings, string _current, out string _new, string _labelText2 = " " )
		{
			_new = _current;
			if (_strings.Length == 0)
				return false;

			int currentInt = 0;
			for (int i = 0; i < _strings.Length; i++)
			{
				if (_strings[i] == _current)
				{
					currentInt = i;
					break;
				}
			}

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(_labelText, GUILayout.Width(EditorGUIUtility.labelWidth));
			if (!string.IsNullOrEmpty(_labelText2))
				GUILayout.Label(_labelText2, GUILayout.Width(LABEL_WIDTH));
			int newInt = EditorGUILayout.Popup(currentInt, _strings);
			EditorGUILayout.EndHorizontal();

			_new = _strings[newInt];

			return currentInt != newInt;
		}

		public static bool BoolBar<T>( ref T _filters ) where T : System.Enum
		{
			int filters = (int)(object)_filters;
			bool result = false;
			string[] types = Enum.GetNames(typeof(T));
			T[] tvalues = (T[])Enum.GetValues(typeof(T));

			using (new EditorGUILayout.HorizontalScope())
			{
				for (int i = 0; i < types.Length; i++)
				{
					if (types[i].StartsWith("All") || types[i].StartsWith("None"))
						continue;

					int currentEnumVal = (int)(object)tvalues[i];
					bool currentVal = (filters & currentEnumVal) != 0;
					bool newVal = GUILayout.Toggle(currentVal, types[i]);
					if (currentVal != newVal)
					{
						result = true;
						if (newVal)
							filters |= currentEnumVal;
						else
							filters &= ~currentEnumVal;
						_filters = (T)(object)filters;
					}
				}
			}

			return result;
		}

		public static bool BoolGrid<T>(
			  Rect _rect
			, int _columns
			, ref T _filters
			, string _title = null
			, bool _showSeparateAllField = true
			, bool _showEmptyBits = false
			, bool _showMultiBits = false
			, float _rowHeight = 16
			) where T : System.Enum
		{
			int filters = (int)(object)_filters;
			bool result = false;
			string[] names = Enum.GetNames(typeof(T));
			T[] values = (T[])Enum.GetValues(typeof(T));
			int rectIdxInitIdx = _showSeparateAllField ? 1 : 0;
			float columnWidth = _rect.width / _columns;

			EditorGUI.DrawRect(_rect, new Color(0.79f, 0.79f, 0.79f));

			if (!string.IsNullOrEmpty(_title))
			{
				EditorGUI.LabelField(_rect, _title);
				_rect.y += _rowHeight;
				_rect.height -= _rowHeight;
			}

			if (_showSeparateAllField)
			{
				int allBitsSetVal = 0;
				foreach (var value in values)
					allBitsSetVal |= (int)(object)value;
				Rect r = new Rect(_rect.x, _rect.y, _rowHeight, _rowHeight);
				bool isAll = filters == allBitsSetVal;
				bool newVal = EditorGUI.Toggle(r, isAll);
				if (isAll != newVal)
				{
					filters = newVal ? allBitsSetVal : 0;
					_filters = (T)(object)filters;
					result = true;
				}
				r.x += _rowHeight;
				r.width = columnWidth - _rowHeight;
				EditorGUI.LabelField(r, "(All)");
			}

			using (new EditorGUILayout.HorizontalScope())
			{
				for (int i = 0, rectidx = rectIdxInitIdx; i < names.Length; i++)
				{
					Rect r = new Rect();
					r.x = (rectidx % _columns) * columnWidth + _rect.x;
					r.width = _rowHeight;
					r.y = (rectidx / _columns) * _rowHeight + _rect.y;
					r.height = _rowHeight;


					int currentEnumVal = (int)(object)values[i];
					bool currentVal;
					bool isEmptyBit = currentEnumVal == 0;
					bool isMultiBit = MultipleBitsSet(currentEnumVal);

					if (isEmptyBit && !_showEmptyBits)
						continue;
					if (isMultiBit && !_showMultiBits)
						continue;

					rectidx++;

					if (isEmptyBit)
						currentVal = filters == 0;
					else if (isMultiBit)
						currentVal = filters == currentEnumVal;
					else
						currentVal = (filters & currentEnumVal) != 0;

					bool newVal = EditorGUI.Toggle(r, currentVal);

					r.x += _rowHeight;
					r.width = columnWidth - _rowHeight;
					EditorGUI.LabelField(r, names[i]);

					if (currentVal != newVal)
					{
						result = true;
						if (isEmptyBit)
						{
							if (newVal)
								filters = 0;
							else
								filters = 0xffff;
						}
						else if (isMultiBit)
						{
							if (newVal)
								filters = currentEnumVal;
							else
								filters = ~currentEnumVal;
						}
						else
						{
							if (newVal)
								filters |= currentEnumVal;
							else
								filters &= ~currentEnumVal;
						}
						_filters = (T)(object)filters;
					}
				}
			}

			return result;
		}

		public static void RemoveArrayElementAtIndex( SerializedProperty _list, int _idx )
		{
			if (!ValidateListAndIndex(_list, _idx))
				return;

			for (int i = _idx; i < _list.arraySize - 1; i++)
				_list.MoveArrayElement(i + 1, i);
			_list.arraySize--;
		}

		public static void SwapArrayElement( SerializedProperty _list, int _idx1, int _idx2 )
		{
			if (_idx1 == _idx2 || !ValidateListAndIndex(_list, _idx1) || !ValidateListAndIndex(_list, _idx2))
				return;
			SerializedProperty pt = _list.GetArrayElementAtIndex(_idx1);
			_list.MoveArrayElement(_idx2, _idx1);
			SerializedProperty p = _list.GetArrayElementAtIndex(_idx2);
			p = pt;
		}

		public static bool IsEditingPrefab( GameObject _go )
		{
			return PrefabStageUtility.GetPrefabStage(_go) != null || _go.scene.name == null;
		}

		public static GameObject GetEditedPrefab( GameObject _go )
		{
			PrefabStage stage = PrefabStageUtility.GetPrefabStage(_go);
			if (stage == null)
				return null;
			string path = stage.prefabAssetPath;
			return AssetDatabase.LoadAssetAtPath<GameObject>(path);
		}

		public static bool IsPrefab( GameObject _go )
		{
			if (_go == null)
				return false;
			return _go.scene.name == null;
		}

		public static bool IsSceneObject( GameObject _go )
		{
			if (_go == null)
				return false;
			return _go.scene.name != null;
		}

		public static bool IsRoot( GameObject _go )
		{
			if (IsEditingPrefab(_go))
				return _go.transform.parent != null && _go.transform.parent.parent == null;
			else
				return _go.transform.parent == null;
		}

		public static bool HasAnyLabel<T>( GameObject _go, T _labelsToFind ) where T : ICollection<string>
		{
			string[] currentLabels = AssetDatabase.GetLabels(_go);
			List<string> newLabels = new List<string>();
			foreach (string label in currentLabels)
				if (_labelsToFind.Contains(label))
					return true;
			return false;
		}

		public static bool HasLabel( GameObject _go, string _labelToFind )
		{
			string[] currentLabels = AssetDatabase.GetLabels(_go);
			foreach (string label in currentLabels)
				if (label == _labelToFind)
					return true;
			return false;
		}


		public static void RemoveLabels<T>( GameObject _go, T _labelsToRemove ) where T : ICollection<string>
		{
			string[] currentLabels = AssetDatabase.GetLabels(_go);
			List<string> newLabels = new List<string>();
			foreach (string label in currentLabels)
				if (!_labelsToRemove.Contains(label))
					newLabels.Add(label);
			AssetDatabase.SetLabels(_go, newLabels.ToArray());
		}

		public static void RemoveLabel( GameObject _go, string _label )
		{
			RemoveLabels(_go, new HashSet<string>() { _label });
		}

		public static void AddLabels<T>( GameObject _go, T _labelsToAdd ) where T : ICollection<string>
		{
			string[] labelsArr = AssetDatabase.GetLabels(_go);
			List<string> labels = new List<string>(labelsArr);
			foreach (string labelToAdd in _labelsToAdd)
			{
				if (!labels.Contains(labelToAdd))
					labels.Add(labelToAdd);
			}
			AssetDatabase.SetLabels(_go, labels.ToArray());
		}

		public static void AddLabel( GameObject _go, string _label )
		{
			AddLabels(_go, new HashSet<string>() { _label });
		}

		// Workaround for the completely idiotic (and insanely named) PrefabUtility functions, which returns funny and completely differing values
		// depending if you are currently editing a prefab or have it selected in the Project or Hierarchy window.
		// This function sorts out all of these circumstances, and returns either when:
		// _rootPrefab == false : the topmost variant if variants exist, the root prefab if no variant exists, or null if _go is not a prefab
		// _rootPrefab == true : the root prefab or null if _go is not a prefab
		public static GameObject GetPrefab( GameObject _go, bool _rootPrefab = false )
		{
			// _go is a prefab root being edited.
			if (IsEditingPrefab(_go) && IsRoot(_go))
			{
				GameObject go = GetEditedPrefab(_go);
				return _rootPrefab ? PrefabUtility.GetCorrespondingObjectFromOriginalSource(_go) : go;
			}

			// _go is a prefab asset selected in the project window
			if (_go.scene.name == null)
				return _rootPrefab ? PrefabUtility.GetCorrespondingObjectFromOriginalSource(_go) : _go;

			// _go is a prefab instance either in the prefab editor or hierarchy window
			if (PrefabUtility.IsAnyPrefabInstanceRoot(_go))
			{
				if (_rootPrefab)
					return PrefabUtility.GetCorrespondingObjectFromOriginalSource(_go);
				string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(_go);
				if (!string.IsNullOrEmpty(path))
					return AssetDatabase.LoadAssetAtPath<GameObject>(path);
			}

			// _go is not a prefab
			return null;
		}

		public static bool MultipleBitsSet( int _n )
		{
			return (_n & (_n - 1)) != 0;
		}

		private static bool ValidateListAndIndex( SerializedProperty _list, int _idx )
		{
			if (!ValidateList(_list))
				return false;

			return ValidateListIndex(_idx, _list);
		}

		private static bool ValidateList( SerializedProperty _list )
		{
			if (!_list.isArray)
			{
				Debug.LogError("Attempt to access array element from a SerializedProperty which isn't an array");
				return false;
			}
			return true;
		}

		private static bool ValidateListIndex( int _idx, SerializedProperty _list )
		{
			if (_idx < 0 || _idx >= _list.arraySize)
			{
				Debug.LogError("Out of Bounds when accessing an array element");
				return false;
			}
			return true;
		}

	}
}
#endif

