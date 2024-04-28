using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Internal;

namespace GuiToolkit.Style.Editor
{
	/// <summary>
	/// Abstract base class for PropertyDrawer implementations.
	/// Should simplify creating custom property drawers; no more dealing with awkward rects,
	/// instead more like a custom Editor
	/// </summary>
	public abstract class AbstractPropertyDrawer : PropertyDrawer
	{
		private static readonly List<SerializedProperty> s_childPropertes = new();
		protected Rect m_Rect;
		protected Rect m_CurrentRect;
		private bool m_collectHeightMode;
		private float m_height;

		protected virtual void OnEnable(SerializedProperty _baseProp) {}

		protected virtual void OnInspectorGUI() {}

		protected void PropertyField(SerializedProperty _property, float _gap = 0)
		{
			var propertyHeight = EditorGUI.GetPropertyHeight(_property) + _gap;
			if (m_collectHeightMode)
			{
				m_height += propertyHeight;
				return;
			}

			var drawRect = new Rect(m_CurrentRect.x, m_CurrentRect.y, m_CurrentRect.width, propertyHeight);
			EditorGUI.PropertyField(drawRect, _property);
			NextRect(propertyHeight);
		}

		protected void LabelField(string _label, [DefaultValue("EditorStyles.label")] GUIStyle _style, float _gap = 0)
		{
			var propertyHeight = EditorGUIUtility.singleLineHeight + _gap;
			if (m_collectHeightMode)
			{
				m_height += propertyHeight;
				return;
			}

			var drawRect = new Rect(m_CurrentRect.x, m_CurrentRect.y, m_CurrentRect.width, propertyHeight);
			EditorGUI.LabelField(drawRect, _label, _style);
			NextRect(propertyHeight);
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

		private void NextRect(float _propertyHeight)
		{
			m_CurrentRect.y += _propertyHeight;
			m_CurrentRect.height -= _propertyHeight;
		}

// Don't override these
		public override void OnGUI(Rect _rect, SerializedProperty _property, GUIContent _label)
		{
			EditorGUI.BeginProperty(_rect, _label, _property);
			m_CurrentRect = m_Rect = _rect;
			OnEnable(_property);
			OnInspectorGUI();
			
			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty _property, GUIContent _)
		{
			m_collectHeightMode = true;
			m_height = 0;
			OnEnable(_property);
			OnInspectorGUI();
			m_collectHeightMode = false;
			return m_height;
		}

		private void CollectChildProperties(SerializedProperty _property)
		{
			s_childPropertes.Clear();
			var enumerator = _property.GetEnumerator();
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