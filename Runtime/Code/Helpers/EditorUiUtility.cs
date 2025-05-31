// Note: don't move this file to an Editor folder, since it needs to be available
// for inplace editor code
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit
{
	/// \brief General Editor Utility
	/// 
	/// This is a collection of common editor helper functions.
	/// 
	/// Note: This file must reside outside of an "Editor" folder, since it must be accessible
	/// from mixed game/editor classes (even though all accesses are in #if UNITY_EDITOR clauses)
	/// See https://answers.unity.com/questions/426184/acces-script-in-the-editor-folder.html for reasons.
	public static class EditorUiUtility
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
		public static Color ColorPerSkin(Color _lightSkin, Color _darkSkin) => EditorGUIUtility.isProSkin ? _darkSkin : _lightSkin;

		public static void DisplayPropertyConditionally(SerializedProperty _condition, SerializedProperty _property, bool _indent = true)
		{
			EditorGUILayout.PropertyField(_condition);
			if (_condition.boolValue)
			{
				if (_indent)
					EditorGUI.indentLevel++;
				
				EditorGUILayout.PropertyField(_property);
				
				if (_indent)
					EditorGUI.indentLevel--;
			}
		}

		public static void Button( string _labelText, string _buttonText, DelegateButtonAction _callback, int height = NORMAL_POPUP_HEIGHT )
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(_labelText, GUILayout.Width(EditorGUIUtility.labelWidth));
			if (GUILayout.Button(_buttonText, GUILayout.Height(height)))
				_callback();
			EditorGUILayout.EndHorizontal();
		}

		public static void Button( Rect _rect, string _labelText, string _buttonText, DelegateButtonAction _callback )
		{
			EditorGUI.PrefixLabel(_rect, GUIUtility.GetControlID(FocusType.Passive), new GUIContent( _labelText ));
			_rect.x += EditorGUIUtility.labelWidth;
			_rect.width -= EditorGUIUtility.labelWidth;
			if (GUI.Button(_rect, _buttonText))
				_callback();
		}

		public static void PropertyField( string _labelText, SerializedProperty _prop, bool _noPropertyLabel = false )
		{
			EditorGUILayout.BeginHorizontal();
			if (!string.IsNullOrEmpty(_labelText))
				GUILayout.Label(_labelText, GUILayout.Width(EditorGUIUtility.labelWidth));
			
			if (_noPropertyLabel)
				EditorGUILayout.PropertyField(_prop, GUIContent.none, true);
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

		public static bool Toggle( string _labelText, string _buttonText, ref bool _boolParm )
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(_labelText, GUILayout.Width(EditorGUIUtility.labelWidth));
			bool current = _boolParm;
			_boolParm = GUILayout.Toggle(_boolParm, _buttonText);
			EditorGUILayout.EndHorizontal();
			return current != _boolParm;
		}

		public static void Label( string _labelText, string _labelText2 )
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(_labelText, GUILayout.Width(EditorGUIUtility.labelWidth));
			GUILayout.Label(_labelText2);
			EditorGUILayout.EndHorizontal();
		}

		public static void LabelItalic( string _labelText, string _labelText2 )
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(_labelText, Italic, GUILayout.Width(EditorGUIUtility.labelWidth));
			GUILayout.Label(_labelText2, Italic);
			EditorGUILayout.EndHorizontal();
		}

		public static void Centered(Action _action)
		{
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			_action.Invoke();
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}

		public static void LabelCentered(string _labelText, GUIStyle _guiStyle = null)
		{
			Centered(() =>
			{
				if (_guiStyle == null)
					GUILayout.Label(_labelText);
				else
					GUILayout.Label(_labelText, _guiStyle);
			});
		}

		public static void WithHeadline(string _headline, Action _action)
		{
			EditorGUILayout.Space(5);
			DrawLine();
			BackgroundBox(new Color(0,0,0,.1f), 22);
			GUILayout.Label("   " + _headline, EditorStyles.boldLabel);
			EditorGUILayout.Space(1);
			DrawLine();
			EditorGUILayout.Space(15);
			_action.Invoke();
			EditorGUILayout.Space(5);
		}

		public static void BackgroundBox(Color _color, int _height)
		{
			DrawLine(_color, _height);
			EditorGUILayout.Space(-_height);
		}

		public static bool EnumPopup<T>( string _labelText, ref T _val, string _labelText2 = "" ) where T : Enum
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

		public static bool EnumPopup<T>( string _labelText, ref SerializedProperty _prop, string _labelText2 = "" ) where T : Enum
		{
			T _val = (T)(object)_prop.intValue;
			bool result = EnumPopup(_labelText, ref _val, _labelText2);
			_prop.intValue = (int)(object)_val;
			return result;
		}

		public static List<string> GetStrings(this SerializedProperty _self)
		{
			List<string> result = new();

			if (!_self.isArray)
				return result;

			for (int i = 0; i < _self.arraySize; i++)
			{
				var elem = _self.GetArrayElementAtIndex(i).stringValue;
				if (string.IsNullOrEmpty(elem))
					continue;

				result.Add(elem);
			}

			return result;
		}

		public static void SetStrings(this SerializedProperty _self, List<string> _strings)
		{
			if (!_self.isArray)
				return;

			int arraySize = _strings.Count;
			_self.arraySize = arraySize;
			for (int i = 0; i < arraySize; i++)
				_self.GetArrayElementAtIndex(i).stringValue = _strings[i];
		}

		public static void HorizontalLine (Color _color) 
		{
			GUIStyle horizontalLine;
			horizontalLine = new GUIStyle();
			horizontalLine.normal.background = EditorGUIUtility.whiteTexture;
			horizontalLine.margin = new RectOffset( 0, 0, 4, 4 );
			horizontalLine.fixedHeight = 1;			
			var savedColor = GUI.color;
			GUI.color = _color;
			GUILayout.Box( GUIContent.none, horizontalLine );
			GUI.color = savedColor;
		}

		public static List<T> GetSerializedReferencesInListProperty<T>(this SerializedProperty _self)
		{
			List<T> result = new();

			if (!_self.isArray)
				return result;

			for (int i = 0; i < _self.arraySize; i++)
			{
				var elem = _self.GetArrayElementAtIndex(i).managedReferenceValue;
				if (elem == null)
					continue;

				result.Add((T) elem);
			}

			return result;
		}

		public static void SetSerializedReferencesInListProperty<T>(this SerializedProperty _self, List<T> _objects)
		{
			if (!_self.isArray)
				return;

			int arraySize = _objects.Count;
			_self.arraySize = arraySize;
			for (int i = 0; i < arraySize; i++)
				_self.GetArrayElementAtIndex(i).managedReferenceValue = _objects[i];
		}

		public static int StringPopup(
			string _labelText, 
			List<string> _strings, 
			string _current, 
			out string _newSelection, 
			string _labelText2 = null, 
			bool _showRemove = false, 
			string _addItemHeadline = null, 
			string _addItemDescription = null,
			string _addItemPreset = null,
			Action<AbstractEditorInputDialog> _additionalContent = null
		)
		{
			_newSelection = _current;
			bool allowAdd = !string.IsNullOrEmpty(_addItemHeadline);

			if (!StringPopupPrepare(_strings, _current, allowAdd, out var currentInt)) 
				return -1;

			EditorGUILayout.BeginHorizontal();


			if (!string.IsNullOrEmpty(_labelText))
				GUILayout.Label(_labelText, GUILayout.Width(EditorGUIUtility.labelWidth));

			if (!string.IsNullOrEmpty(_labelText2))
				GUILayout.Label(_labelText2, GUILayout.Width(LABEL_WIDTH));

			int newInt = EditorGUILayout.Popup(currentInt, _strings.ToArray());

			if (StringPopupAddNewEntryIfNecessary(_strings, newInt, allowAdd, _addItemHeadline, _addItemDescription, _addItemPreset, _additionalContent, ref _newSelection))
				return newInt;

			if (_showRemove && _strings.Count > 0 && GUILayout.Button(EditorGUIUtility.IconContent("P4_DeletedLocal")))
			{
				_strings.RemoveAt(newInt);
				if (newInt >= _strings.Count)
					newInt = 0;
				EditorGUILayout.EndHorizontal();
				_newSelection = _strings.Count > 0 ? _strings[newInt] : null;
				return newInt;
			}

			EditorGUILayout.EndHorizontal();

			if (_strings.Count == 0)
				return -1;

			if (newInt >= _strings.Count || newInt < 0)
				return -1;

			_newSelection = _strings[newInt];
			return currentInt != newInt ? newInt : -1;
		}

		public static bool StringPopup(
			Rect _pos, 
			string _labelText, 
			List<string> _strings, 
			string _current, 
			out string _newSelection, 
			string _labelText2 = null, 
			bool _showRemove = false, 
			string _addItemHeadline = null, 
			string _addItemDescription = null,
			string _addItemPreset = null,
			Action<AbstractEditorInputDialog> _additionalContent = null
		)
		{
			_newSelection = _current;
			bool allowAdd = !string.IsNullOrEmpty(_addItemHeadline);

			if (!StringPopupPrepare(_strings, _current, allowAdd, out var currentInt)) 
				return false;

			Rect labelRect = new Rect(_pos.x, _pos.y, EditorGUIUtility.labelWidth, _pos.height);
			EditorGUI.LabelField(labelRect, _labelText);
			_pos.width -= EditorGUIUtility.labelWidth;
			_pos.x += EditorGUIUtility.labelWidth;

			if (!string.IsNullOrEmpty(_labelText2))
			{
				labelRect.x += EditorGUIUtility.labelWidth;
				EditorGUI.LabelField(labelRect, _labelText2);
				_pos.width -= EditorGUIUtility.labelWidth;
				_pos.x += EditorGUIUtility.labelWidth;
			}

			int removeButtonWidth = _showRemove ? 50 : 0;
			Rect popupRect = new Rect(_pos.x, _pos.y, _pos.width - removeButtonWidth, _pos.height);

			int newInt = EditorGUI.Popup(popupRect, currentInt, _strings.ToArray());

			if (StringPopupAddNewEntryIfNecessary(_strings, newInt, allowAdd, _addItemHeadline, _addItemDescription, _addItemPreset, _additionalContent, ref _newSelection))
				return true;

			_pos.x += popupRect.width;
			_pos.width -= popupRect.width;

			if (_showRemove && _strings.Count > 0 && GUI.Button(_pos, EditorGUIUtility.IconContent("P4_DeletedLocal")))
			{
				_strings.RemoveAt(newInt);
				if (newInt >= _strings.Count)
					newInt = 0;
				_newSelection = _strings.Count > 0 ? _strings[newInt] : null;
				return true;
			}

			if (_strings.Count == 0)
				return false;

			_newSelection = _strings[newInt];
			return currentInt != newInt;
		}

		private static bool StringPopupPrepare(List<string> _strings, string _current, bool _allowAdd, out int _currentIdx)
		{
			_currentIdx = -1;

			if (_allowAdd)
			{
				_strings.Add("");
				_strings.Add("New...");
			}

			if (!_allowAdd && _strings.Count == 0)
				return false;

			if (!string.IsNullOrEmpty(_current))
			{
				for (int i = 0; i < _strings.Count; i++)
				{
					if (_strings[i] == _current)
					{
						_currentIdx = i;
						break;
					}
				}
			}

			return true;
		}

		private static bool StringPopupAddNewEntryIfNecessary(
			List<string> _strings, 
			int _idx, 
			bool _allowAdd, 
			string _addItemHeadline, 
			string _addItemDescription,
			string _addItemPreset,
			Action<AbstractEditorInputDialog> _additionalContent,
			ref string _newSelection
			)
		{
			if (!_allowAdd)
				return false;

			_strings.RemoveRange(_strings.Count-2, 2);
			if (_idx != _strings.Count + 1)
				return false;

			var newEntry = EditorInputDialog.Show( _addItemHeadline, _addItemDescription, _addItemPreset, _additionalContent);
			if (string.IsNullOrEmpty(newEntry))
				return false;

			if (_strings.Contains(newEntry))
			{
				Debug.LogError($"Can not add '{newEntry}'; already contained in the list of _strings");
				return false;
			}

			_strings.Add(newEntry);
			_idx = _strings.Count - 1;
			_newSelection = _strings[_idx];
			return true;
		}

		public static void FlagEnumPopup<T>(SerializedProperty _prop, string _labelText) where T : Enum
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(_labelText, GUILayout.Width(EditorGUIUtility.labelWidth));
			T direction = (T) (object) _prop.intValue;
			direction = (T) EditorGUILayout.EnumFlagsField(direction);
			_prop.intValue = (int) (object) direction;
			EditorGUILayout.EndHorizontal();

			_prop.serializedObject.ApplyModifiedProperties();
		}

		public static bool BoolBar<T>( ref T _filters, string _labelText = null, int _perRow = 0, bool _nicifyNames = true ) where T : Enum
		{
			int filterVal = (int)(object)_filters;
			bool result = false;
			string[] types = Enum.GetNames(typeof(T));
			T[] tvalues = (T[])Enum.GetValues(typeof(T));

			if (_perRow == 0)
				_perRow = types.Length;
			
			float width = Screen.width / _perRow;
			
			EditorGUILayout.BeginHorizontal();

			if (!string.IsNullOrEmpty(_labelText))
				GUILayout.Label(_labelText, GUILayout.Width(EditorGUIUtility.labelWidth));

			for (int i = 0, visible = 0; i < types.Length; i++)
			{
				if (types[i].StartsWith("All") || types[i].StartsWith("None"))
					continue;

				if (_perRow > 0 && visible > 0 && visible % _perRow == 0)
				{
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.BeginHorizontal();
				}
				visible++;

				int currentEnumVal = (int)(object)tvalues[i];
				bool currentVal = (filterVal & currentEnumVal) != 0;
				var name = types[i];
				if (_nicifyNames)
					name = ObjectNames.NicifyVariableName(name);
				
				bool newVal = GUILayout.Toggle(currentVal, name, GUILayout.Width(width));
				if (currentVal != newVal)
				{
					result = true;
					if (newVal)
						filterVal |= currentEnumVal;
					else
						filterVal &= ~currentEnumVal;
					_filters = (T)(object)filterVal;
				}
			}

			EditorGUILayout.EndHorizontal();

			return result;
		}

		public static bool BoolBar( List<string> _names, List<bool> _values, string _labelText = null, int _perRow = 0 )
		{
			if (_names == null || _values == null || _names.Count != _values.Count)
			{
				Debug.LogError("Wrong values in BoolBar");
				return false;
			}
			
			bool result = false;
			if (_perRow == 0)
				_perRow = _names.Count;
			
			bool hasLabel = !string.IsNullOrEmpty(_labelText);
			float width = Screen.width / _perRow;
			
			EditorGUILayout.BeginHorizontal();

			if (hasLabel)
				GUILayout.Label(_labelText, GUILayout.Width(EditorGUIUtility.labelWidth));

			for (int i = 0, visible = 0; i < _names.Count; i++)
			{
				if (_perRow > 0 && visible > 0 && visible % _perRow == 0)
				{
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.BeginHorizontal();
				}
				visible++;

				bool currentVal = _values[i];
				bool newVal = GUILayout.Toggle(currentVal, _names[i], GUILayout.Width(width));
				if (currentVal != newVal)
				{
					result = true;
					_values[i] = newVal;
				}
			}

			EditorGUILayout.EndHorizontal();
			return result;
		}
		
		
		public static bool BoolBar( Dictionary<string, bool> _dict, string _labelText = null, int _perRow = 0 )
		{
			List<string> names = new();
			List<bool> values = new();
			
			foreach (var kv in _dict.OrderBy(x => x.Key))
			{
				names.Add(kv.Key);
				values.Add(kv.Value);
			}
			
			if (!BoolBar(names, values, _labelText, _perRow))
				return false;
			
			for (int i=0; i<names.Count; i++)
				_dict[names[i]] = values[i];

			return true;
		}

		public static bool BoolBar<T>( SerializedProperty _prop, string _labelText = null ) where T : Enum
		{
			EditorGUILayout.BeginHorizontal();
			T t = (T) (object) _prop.intValue;
			bool result = BoolBar<T>(ref t, _labelText);
			_prop.intValue = (int) (object) t;
			EditorGUILayout.EndHorizontal();
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
			) where T : Enum
		{
			int filterVal = (int)(object)_filters;
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
				bool isAll = filterVal == allBitsSetVal;
				bool newVal = EditorGUI.Toggle(r, isAll);
				if (isAll != newVal)
				{
					filterVal = newVal ? allBitsSetVal : 0;
					_filters = (T)(object)filterVal;
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
					bool isMultiBit = EditorGeneralUtility.AreMultipleBitsSet(currentEnumVal);

					if (isEmptyBit && !_showEmptyBits)
						continue;
					if (isMultiBit && !_showMultiBits)
						continue;

					rectidx++;

					if (isEmptyBit)
						currentVal = filterVal == 0;
					else if (isMultiBit)
						currentVal = filterVal == currentEnumVal;
					else
						currentVal = (filterVal & currentEnumVal) != 0;

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
								filterVal = 0;
							else
								filterVal = 0xffff;
						}
						else if (isMultiBit)
						{
							if (newVal)
								filterVal = currentEnumVal;
							else
								filterVal = ~currentEnumVal;
						}
						else
						{
							if (newVal)
								filterVal |= currentEnumVal;
							else
								filterVal &= ~currentEnumVal;
						}
						_filters = (T)(object)filterVal;
					}
				}
			}

			return result;
		}

		public static void DrawLine( SerializedProperty _point0Prop, SerializedProperty _point1Prop, Vector3 _rectPoint0, Vector3 _rectPoint1, Vector2 _rectSize, RectTransform _rt )
		{
			DrawLine(_point0Prop.vector2Value, _point1Prop.vector2Value, _rectPoint0, _rectPoint1, _rectSize, _rt);
		}

		public static void DrawLine( Vector2 _point0, Vector2 _point1, Vector3 _rectPoint0, Vector3 _rectPoint1, Vector2 _rectSize, RectTransform _rt )
		{
			Vector3 offset0 = _rt.TransformVector(Vector3.Scale(_point0, _rectSize));
			_point0 = _rectPoint0 + offset0;
			Vector3 offset1 = _rt.TransformVector(Vector3.Scale(_point1, _rectSize));
			_point1 = _rectPoint1 + offset1;

			Handles.DrawLine(_point0, _point1);
		}
		
		public static void DrawLine(int _height = 1)
		{
			DrawLine(new Color ( 0.5f,0.5f,0.5f, 1 ), _height);
		}

		public static void DrawLine(Color _color, int _height = 1)
		{
			Rect rect = EditorGUILayout.GetControlRect(false, _height );
			rect.height = _height;
			EditorGUI.DrawRect(rect, _color);
		}

		public static bool DoHandle( SerializedProperty _serializedProperty, Vector3 _pointInRect, Vector2 _rectSize, RectTransform _rt, bool _mirrorHorizontal = false, bool _mirrorVertical = false, float _handleSize = 0.08f )
		{
			Vector2 v = _serializedProperty.vector2Value;
			bool result = DoHandle(ref v, _pointInRect, _rectSize, _rt, _mirrorHorizontal, _mirrorVertical, _handleSize);
			if (result)
				_serializedProperty.vector2Value = v;
			return result;
		}
		
		public static bool DoHandle( ref Vector2 _point, Vector3 _pointInRect, Vector2 _rectSize, RectTransform _rt, bool _mirrorHorizontal = false, bool _mirrorVertical = false, float _handleSize = 0.08f )
		{
			Vector3 normPoint = _point;
			normPoint.x *= _mirrorHorizontal ? -1 : 1;
			normPoint.y *= _mirrorVertical ? -1 : 1;
			var offset = Vector3.Scale(normPoint, _rectSize);
			offset = Vector3.Scale(offset, _rt.lossyScale);
			
			Vector3 point = _pointInRect + offset;

			_handleSize *= HandleUtility.GetHandleSize(_pointInRect);

			Vector3 oldLeft = point;
			EditorGUI.BeginChangeCheck();
			Vector3 newLeft = Handles.FreeMoveHandle(oldLeft, _handleSize, Vector3.one, Handles.DotHandleCap);
			if (EditorGUI.EndChangeCheck())
			{
				Vector3 moveOffset = newLeft-oldLeft;
				moveOffset = Vector3.Scale(moveOffset, UiMathUtility.Inverse(_rt.lossyScale));

				// apply mirror
				moveOffset.x *= _mirrorHorizontal ? -1 : 1;
				moveOffset.y *= _mirrorVertical ? -1 : 1;

				_point += moveOffset.Xy() / _rectSize;

				return true;
			}
			return false;
		}
	}
}
#endif

