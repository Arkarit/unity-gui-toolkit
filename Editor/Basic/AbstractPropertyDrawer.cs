using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Style.Editor
{
	/// <summary>
	/// Abstract base class for PropertyDrawer implementations.
	/// Should simplify creating custom property drawers; no more dealing with awkward rects,
	/// instead more like a custom Editor
	/// </summary>
	public abstract class AbstractPropertyDrawer<T> : PropertyDrawer where T : class
	{
		private const float FoldoutHeight = 15;
		private const float IndentWidth = 20;

		private static readonly List<SerializedProperty> s_childPropertes = new();
		protected Rect m_Rect;
		protected Rect m_currentRect;
		private bool m_collectHeightMode;
		private float m_height;
		private SerializedProperty m_property;
		private static readonly Dictionary<object, bool> s_foldouts = new ();

		protected virtual void OnEnable() {}

		protected virtual void OnInspectorGUI() {}

		protected List<SerializedProperty> ChildProperties => s_childPropertes;
		protected SerializedProperty Property => m_property;
		protected float SingleLineHeight => EditorGUIUtility.singleLineHeight;
		protected T EditedClass => Property.boxedValue as T;

		protected void PropertyField(SerializedProperty _property, float _gap = 0)
		{
			var propertyHeight = EditorGUI.GetPropertyHeight(_property) + _gap;
			if (m_collectHeightMode)
			{
				m_height += propertyHeight;
				return;
			}

			var drawRect = new Rect(m_currentRect.x, m_currentRect.y, m_currentRect.width, propertyHeight);
			EditorGUI.PropertyField(drawRect, _property);
			NextRect(propertyHeight);
		}

		protected void LabelField(string _label, float _gap = 0, GUIStyle _style = null)
		{
			var propertyHeight = SingleLineHeight + _gap;
			if (m_collectHeightMode)
			{
				m_height += propertyHeight;
				return;
			}

			var drawRect = new Rect(m_currentRect.x, m_currentRect.y, m_currentRect.width, propertyHeight);
			if (_style != null)
				EditorGUI.LabelField(drawRect, _label, _style);
			else
				EditorGUI.LabelField(drawRect, _label);

			NextRect(propertyHeight);
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
				m_height += propertyHeight;
				return false;
			}

			var drawRect = new Rect(m_currentRect.x, m_currentRect.y, m_currentRect.width, propertyHeight);
			var result = EditorUiUtility.StringPopup(drawRect, _labelText, _strings, _current, out _newSelection,
				_labelText2, showRemove, _addItemHeadline, _addItemDescription);

			NextRect(propertyHeight);
			return result;
		}

		protected void Space(float _gap)
		{
			var propertyHeight = _gap;
			if (m_collectHeightMode)
			{
				m_height += propertyHeight;
				return;
			}

			NextRect(propertyHeight);
		}

		protected void Line(Color _color, float _gap = 0, float _width = 0, float _height = 1)
		{
			var propertyHeight = _gap + _height;
			if (m_collectHeightMode)
			{
				m_height += propertyHeight;
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

		protected void Foldout(object _id, string _title, Action _onFoldout) => Foldout(_id, _title, true, _onFoldout);

		protected void Foldout(object _id, string _title, bool _default, Action _onFoldout)
		{
			var foldoutRect = new Rect(m_currentRect.x, m_currentRect.y, m_currentRect.width, FoldoutHeight);
			if (!s_foldouts.ContainsKey(_id))
				s_foldouts.Add(_id, _default);

			var active = s_foldouts[_id];

			if (!m_collectHeightMode)
				active = EditorGUI.Foldout(foldoutRect, active, _title, true);

			m_currentRect.y += FoldoutHeight;
			if (active)
				_onFoldout();

			s_foldouts[_id] = active;
		}

		protected void Indent(Action _onIndent)
		{
			m_currentRect.x += IndentWidth;
			_onIndent();
			m_currentRect.x -= IndentWidth;
		}

		private void NextRect(float _propertyHeight)
		{
			m_currentRect.y += _propertyHeight;
			m_currentRect.height -= _propertyHeight;
		}

// Don't override as of here (unless you've got a real cause)

		public override void OnGUI(Rect _rect, SerializedProperty _property, GUIContent _label)
		{
			EditorGUI.BeginProperty(_rect, _label, _property);
			m_property = _property;
			CollectChildProperties(_property);
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
			CollectChildProperties(_property);
			OnEnable();
			OnInspectorGUI();
			m_collectHeightMode = false;
			return m_height;
		}

		private void CollectChildProperties(SerializedProperty _property)
		{
			s_childPropertes.Clear();
			var enumerator = _property.Copy().GetEnumerator();
			int depth = _property.depth;

			while (enumerator.MoveNext())
			{
				var property = enumerator.Current as SerializedProperty;
				if (property == null || property.depth > depth + 1)
					continue;

				s_childPropertes.Add(property.Copy());
			}
		}
	}
}