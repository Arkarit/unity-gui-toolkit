using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiThing), true)]
	public class UiThingEditor : UnityEditor.Editor
	{
		protected SerializedProperty m_enabledInHierarchyProp;

		private static readonly HashSet<string> s_emptyHashSetString = new HashSet<string>();

		private readonly List<(Type type, List<SerializedProperty> properties)> m_properties = new();
		private readonly List<(Type type, List<SerializedProperty> properties)> m_eventProperties = new();
		private readonly List<Type> m_typeHierarchyList = new();
		private bool m_eventsFoldout;
		private bool m_hasEvents;

		protected virtual HashSet<string> excludedProperties => s_emptyHashSetString;

		protected virtual void OnEnable()
		{
			m_enabledInHierarchyProp = serializedObject.FindProperty("m_enabledInHierarchy");

			m_properties.Clear();
			m_eventProperties.Clear();
			m_typeHierarchyList.Clear();
			m_hasEvents = false;

			var _type = serializedObject.targetObject.GetType();
			for (;;)
			{
				m_typeHierarchyList.Add(_type);
				if (_type == typeof(UiThing) || _type == typeof(object))
					break;

				_type = _type.BaseType;
			}

			for (int i=m_typeHierarchyList.Count-1; i >= 0; --i)
			{
				var type = m_typeHierarchyList[i];
				var list = new List<SerializedProperty>();
				m_properties.Add(new (type, list));

				list = new List<SerializedProperty>();
				m_eventProperties.Add(new (type, list));
			}

			EditorGeneralUtility.ForeachPropertySerObj(serializedObject, prop =>
			{
				if (excludedProperties.Contains(prop.name))
					return;

				if (IsCEvent(prop))
				{
					AddToList(prop, m_eventProperties);
					m_hasEvents = true;
					return;
				}

				AddToList(prop, m_properties);
			}, false);
		}

		private void AddToList(SerializedProperty _serializedProperty, List<(Type type, List<SerializedProperty> properties)> _list)
		{
			var parentClassType = GetParentClassType(_serializedProperty);
			foreach (var tuple in _list)
			{
				if (tuple.type == parentClassType)
				{
					tuple.properties.Add(_serializedProperty);
					break;
				}
			}
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			UiThing thisUiThing = (UiThing)target;

			if (thisUiThing.IsEnableableInHierarchy)
			{
				EditorGUILayout.LabelField($"{nameof(UiThing)} Members:", EditorStyles.boldLabel);
				EditorGUILayout.Space(2);
				EditorGUILayout.PropertyField(m_enabledInHierarchyProp);
				thisUiThing.EnabledInHierarchy = m_enabledInHierarchyProp.boolValue;
				EditorGUILayout.Space(7);
			}

			DrawList(m_properties, " Members:");

			if (m_hasEvents)
			{
				m_eventsFoldout = EditorGUILayout.Foldout(m_eventsFoldout, "Events");
				if (m_eventsFoldout)
					DrawList(m_eventProperties, " Events:");
			}

			serializedObject.ApplyModifiedProperties();
		}

		private void DrawList(List<(Type type, List<SerializedProperty> properties)> _list, string _postfix)
		{
			foreach (var tuple in _list)
			{
				if (tuple.properties.Count == 0)
					continue;

				EditorGUILayout.LabelField($"{tuple.type.Name}{_postfix}", EditorStyles.boldLabel);
				EditorGUILayout.Space(2);
				foreach (var prop in tuple.properties)
				{
					EditorGUILayout.PropertyField(prop);
				}

				EditorGUILayout.Space(7);
			}
		}

		private static bool IsCEvent(SerializedProperty _serializedProperty)
		{
			if (_serializedProperty.propertyType != SerializedPropertyType.Generic)
				return false;

			return _serializedProperty.type.Contains("CEvent");
		}

		private Type GetParentClassType(SerializedProperty _serializedProperty)
		{
			var targetType = _serializedProperty.serializedObject.targetObject.GetType();
			return GetParentClassType(targetType, _serializedProperty.name, typeof(UiThing));
		}

		private Type GetParentClassType(Type _type, string _name, Type _maxBaseClass)
		{
			for (int i = m_typeHierarchyList.Count - 1; i >= 0; --i)
			{
				var type = m_typeHierarchyList[i];
				FieldInfo fi = type.GetField(_name,
					BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
				if (fi != null)
					return type;
			}

			return null;
		}
	}
}