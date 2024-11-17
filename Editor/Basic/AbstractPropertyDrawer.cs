using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Abstract base class for PropertyDrawer implementations.
	/// Should simplify creating custom property drawers; no more dealing with awkward rects,
	/// instead more like a custom Editor
	/// </summary>
	public abstract class AbstractPropertyDrawer<T> : PropertyDrawer where T : class
	{
		private const float FoldoutHeight = 16;
		private const float IndentWidth = 20;

		protected Rect m_Rect;
		protected Rect m_currentRect;
		private bool m_collectHeightMode;
		private int m_horizontalMode;
		private float m_savedX;
		private float m_savedWidth;
		private float m_height;
		private SerializedProperty m_property;
		private static readonly Dictionary<object, bool> s_foldouts = new ();
		private static readonly Dictionary<string, float> s_heightCache = new();
		private bool m_heightCacheEnabled;
		private static readonly List<SerializedProperty> s_tempProperties = new();


		protected virtual void OnEnable() {}

		protected virtual void OnInspectorGUI() {}

		protected SerializedProperty Property => m_property;
		protected float SingleLineHeight => EditorGUIUtility.singleLineHeight;
		protected T EditedClassInstance => Property.boxedValue as T;
		protected bool IsHorizontal => m_horizontalMode > 0;
		protected Rect CurrentRect => m_currentRect;
		protected bool CollectHeightMode => m_collectHeightMode;

		private void IncreaseHeight(float _height)
		{
			if (IsHorizontal)
				return;

			m_height += _height;
		}

		protected virtual float GetPropertyHeight(SerializedProperty _property)
		{
			if (!HeightCacheEnabled)
				return EditorGUI.GetPropertyHeight(_property);
			
			string key = $"{_property.propertyPath}~{_property.isExpanded}";
			if (s_heightCache.TryGetValue(key, out float result))
				return result;
			
			result = EditorGUI.GetPropertyHeight(_property);
			if (result == 0)
				return result;
			
			s_heightCache.Add(key, result);
			
			return result;
		}
		
		protected bool HeightCacheEnabled
		{
			get => m_heightCacheEnabled;
			set => m_heightCacheEnabled = value;
		}
		protected void InvalidateHeightCache() => s_heightCache.Clear();
		
		protected void PropertyField(SerializedProperty _property, bool _withChildren = true, float _gap = 0)
		{
			var propertyHeight = GetPropertyHeight(_property) + _gap;
			if (propertyHeight == 0)
				return;

			if (m_collectHeightMode)
			{
				IncreaseHeight(propertyHeight);
				return;
			}

			var drawRect = new Rect(m_currentRect.x, m_currentRect.y, m_currentRect.width, propertyHeight);
			EditorGUI.PropertyField(drawRect, _property, _withChildren);
			NextRect(propertyHeight);
		}

		protected void LabelField(string _label, float _gap = 0, GUIStyle _style = null)
		{
			var propertyHeight = SingleLineHeight + _gap;
			if (m_collectHeightMode)
			{
				IncreaseHeight(propertyHeight);
				return;
			}

			var drawRect = new Rect(m_currentRect.x, m_currentRect.y, m_currentRect.width, propertyHeight);
			if (_style != null)
				EditorGUI.LabelField(drawRect, _label, _style);
			else
				EditorGUI.LabelField(drawRect, _label);

			NextRect(propertyHeight);
		}

		protected bool Toggle(string _label, bool _currentValue, float _gap = 0, GUIStyle _style = null)
		{
			var propertyHeight = SingleLineHeight + _gap;
			if (m_collectHeightMode)
			{
				IncreaseHeight(propertyHeight);
				return _currentValue;
			}

			var drawRect = new Rect(m_currentRect.x, m_currentRect.y, m_currentRect.width, propertyHeight);
			bool result;
			if (_style != null)
				result = EditorGUI.Toggle(drawRect, _label, _currentValue, _style);
			else
				result = EditorGUI.Toggle(drawRect, _label, _currentValue);

			NextRect(propertyHeight);
			return result;
		}

		protected bool StringPopupField(
			string _labelText, 
			List<string> _strings, 
			string _current,
			out string _newSelection,
			string _labelText2 = null, 
			bool showRemove = false, 
			string _addItemHeadline = null,
			string _addItemDescription = null
		)
		{
			_newSelection = string.Empty;
			var propertyHeight = SingleLineHeight;
			if (m_collectHeightMode)
			{
				IncreaseHeight(propertyHeight);
				return false;
			}

			var drawRect = new Rect(m_currentRect.x, m_currentRect.y, m_currentRect.width, propertyHeight);
			var result = EditorUiUtility.StringPopup(drawRect, _labelText, _strings, _current, out _newSelection,
				_labelText2, showRemove, _addItemHeadline, _addItemDescription);

			NextRect(propertyHeight);
			return result;
		}

		protected T EnumPopupField<T>( string _labelText, T _current) where T:Enum
		{
			var propertyHeight = SingleLineHeight;
			if (m_collectHeightMode)
			{
				IncreaseHeight(propertyHeight);
				return _current;
			}

			var drawRect = new Rect(m_currentRect.x, m_currentRect.y, m_currentRect.width, propertyHeight);
			var result = (T) EditorGUI.EnumPopup(drawRect, _labelText, _current);

			NextRect(propertyHeight);
			return result;
		}

		protected void EnumPopupField<T>( string _labelText, SerializedProperty _serializedProperty) where T:Enum
		{
			var propertyHeight = SingleLineHeight;
			if (m_collectHeightMode)
			{
				IncreaseHeight(propertyHeight);
				return;
			}

			T val = (T)(object) _serializedProperty.intValue;
			var drawRect = new Rect(m_currentRect.x, m_currentRect.y, m_currentRect.width, propertyHeight);
			val = (T) EditorGUI.EnumPopup(drawRect, _labelText, val);
			_serializedProperty.intValue = (int)(object) val;

			NextRect(propertyHeight);
		}

		protected void Space(float _gap)
		{
			var propertyHeight = _gap;
			if (m_collectHeightMode)
			{
				IncreaseHeight(propertyHeight);
				return;
			}

			NextRect(propertyHeight);
		}

		protected void Line(Color _color, float _gap = 0, float _width = 0, float _height = 1)
		{
			var propertyHeight = _gap + _height;
			if (m_collectHeightMode)
			{
				IncreaseHeight(propertyHeight);
				return;
			}

			var width = _width == 0 ? m_currentRect.width : _width;
			var lineRect = new Rect(
				m_currentRect.x, 
				m_currentRect.y,
				width,
				_height
			);

			EditorGUI.DrawRect(lineRect, _color );
			NextRect(propertyHeight);
		}

		protected void Line(float _gap = 0, float _width = 0, float _height = 1) =>
			Line(Color.gray, _gap, _width, _height);

		protected bool Foldout(object _id, string _title, Action _onFoldout) => Foldout(_id, _title, true, _onFoldout);

		protected bool Foldout(object _id, string _title, bool _default, Action _onFoldout)
		{
			var foldoutRect = new Rect(m_currentRect.x, m_currentRect.y, m_currentRect.width *.5f, FoldoutHeight);
			if (!s_foldouts.ContainsKey(_id))
				s_foldouts.Add(_id, _default);

			var active = s_foldouts[_id];

			if (!m_collectHeightMode)
				active = EditorGUI.Foldout(foldoutRect, active, _title, true);

			m_currentRect.y += FoldoutHeight;
			IncreaseHeight(FoldoutHeight);

			if (active)
				Indent(() => _onFoldout());

			s_foldouts[_id] = active;
			return active;
		}

		protected bool Button(GUIContent _content, float _width = -1)
		{
			if (m_collectHeightMode)
			{
				IncreaseHeight(SingleLineHeight);
				return false;
			}

			if (_width == -1)
				_width = m_currentRect.width;

			var buttonRect = new Rect
			(
				m_currentRect.x,
				m_currentRect.y,
				_width,
				SingleLineHeight
			);

			bool result = GUI.Button(buttonRect, _content);
			NextRect(SingleLineHeight);
			return result;
		}

		protected bool Button(string _s, float _width = -1) => Button(new GUIContent(_s), _width);

		protected void IncreaseX(float _width)
		{
			if (_width < 0)
			{
				_width = m_currentRect.width + _width;
			}

			m_currentRect.x += _width;
			m_currentRect.width -= _width;
		}

		protected void Horizontal(float _height, Action _onHorizontal)
		{
			if (!IsHorizontal)
			{
				if (m_collectHeightMode)
					IncreaseHeight(SingleLineHeight);

				m_savedX = m_currentRect.x;
				m_savedWidth = m_currentRect.width;
			}

			m_horizontalMode++;
			_onHorizontal();
			m_horizontalMode--;

			if (!IsHorizontal)
			{
				m_currentRect.x = m_savedX;
				m_currentRect.width = m_savedWidth;
			}

			NextRect(_height);
		}

		protected void Background(float _xOffs = 0, float _yOffs = 0, float _plusWidth = 0, float _plusHeight = 0) =>
			Background(
				EditorUiUtility.ColorPerSkin(new Color(0, 0, 0, .05f),new Color(1,1,1,0.05f)), 
				_xOffs, 
				_yOffs, 
				_plusWidth, 
				_plusHeight
			);

		protected void Background(Color _lightSkin, Color _darkSkin, float _xOffs = 0, float _yOffs = 0, float _plusWidth = 0, float _plusHeight = 0) =>
			Background(
				EditorUiUtility.ColorPerSkin(_lightSkin, _darkSkin), 
				_xOffs, 
				_yOffs, 
				_plusWidth, 
				_plusHeight
			);

		protected void Background(Color _color, float _xOffs = 0, float _yOffs = 0, float _plusWidth = 0, float _plusHeight = 0)
		{
			if (m_collectHeightMode)
				return;

			var rect = new Rect
			(
				m_currentRect.x + _xOffs, 
				m_currentRect.y + _yOffs, 
				m_currentRect.width + _plusWidth, 
				m_currentRect.height + _plusHeight
			);

			EditorGUI.DrawRect(rect, _color);
		}

		protected void BackgroundBox(float _xOffs, float _yOffs, float _plusWidth, float _height) =>
			BackgroundBox(
				EditorUiUtility.ColorPerSkin(new Color(0, 0, 0, .05f),new Color(1,1,1,0.05f)), 
				_xOffs, 
				_yOffs, 
				_plusWidth, 
				_height
			);

		protected void BackgroundBox(Color _lightSkin, Color _darkSkin, float _xOffs, float _yOffs, float _plusWidth, float _height) =>
			BackgroundBox(
				EditorUiUtility.ColorPerSkin(_lightSkin, _darkSkin), 
				_xOffs, 
				_yOffs, 
				_plusWidth, 
				_height
			);

		protected void BackgroundBox(Color _color, float _xOffs, float _yOffs, float _plusWidth, float _height)
		{
			if (m_collectHeightMode)
				return;

			var rect = new Rect
			(
				m_currentRect.x + _xOffs, 
				m_currentRect.y + _yOffs, 
				m_currentRect.width + _plusWidth, 
				_height
			);

			EditorGUI.DrawRect(rect, _color);
		}

		protected void Indent(Action _onIndent)
		{
			m_currentRect.x += IndentWidth;
			m_currentRect.width -= IndentWidth;
			_onIndent();
			m_currentRect.x -= IndentWidth;
			m_currentRect.width += IndentWidth;
		}

		protected void Outdent(Action _onIndent)
		{
			m_currentRect.x -= IndentWidth;
			m_currentRect.width += IndentWidth;
			_onIndent();
			m_currentRect.x += IndentWidth;
			m_currentRect.width -= IndentWidth;
		}

		private void NextRect(float _propertyHeight)
		{
			if (IsHorizontal)
				return;

			m_currentRect.y += _propertyHeight;
			m_currentRect.height -= _propertyHeight;
		}

// Don't override as of here (unless you've got a real cause)

		public override void OnGUI(Rect _rect, SerializedProperty _property, GUIContent _label)
		{
			var screenPos = GUIUtility.GUIToScreenPoint(_rect.position);
			if (screenPos.y > Screen.height || screenPos.y + _rect.height < 0)
				return;

			EditorGUI.BeginProperty(_rect, _label, _property);
			m_property = _property;
			m_currentRect = m_Rect = _rect;
			OnEnable();
			OnInspectorGUI();
			
			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty _property, GUIContent _)
		{
			m_collectHeightMode = true;
			m_height = 0;
			m_property = _property;
			OnEnable();
			OnInspectorGUI();
			m_collectHeightMode = false;
			return m_height;
		}

		protected delegate bool ChildPropertyDelegate(SerializedProperty childProperty);

		protected void ForEachChildProperty(SerializedProperty _property, ChildPropertyDelegate callback)
		{
			int depth = _property.depth;

			int propertyCount = 0;
			foreach (var obj in _property.Copy())
			{
				var property = obj as SerializedProperty;
				if (property == null || property.depth > depth + 1)
					continue;

				propertyCount++;
			}

			int currentPropIdx = 0;
			foreach (var obj in _property.Copy())
			{
				var property = obj as SerializedProperty;
				if (property == null || property.depth > depth + 1)
					continue;

				s_tempProperties.Add(property.Copy());
				if (currentPropIdx == propertyCount - 1)
				{
					s_tempProperties.Sort((a, b) => a.displayName.CompareTo(b.displayName));
					foreach (var p in s_tempProperties)
					{
						if (!callback(p))
						{
							s_tempProperties.Clear();
							return;
						}
					}
				}

				currentPropIdx++;		
			}

			s_tempProperties.Clear();
		}
	}
}
