using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Style.Editor
{
	[CustomPropertyDrawer(typeof(UiAbstractStyleBase), true)]
	public class UiAbstractStyleBaseDrawer : PropertyDrawer
	{
		private static readonly List<SerializedProperty> s_childPropertes = new();

		public override void OnGUI( Rect _position, SerializedProperty _property, GUIContent _label )
		{
			EditorGUI.BeginProperty(_position, _label, _property);
Debug.Log("----------");

			Rect currentRect = new Rect(_position.x, _position.y, _position.width, EditorGUIUtility.singleLineHeight);
			CollectChildProperties(_property);
			foreach (var childProperty in s_childPropertes)
			{
//Debug.Log($"property.propertyPath:{childProperty.propertyPath} property.type:{childProperty.type}");
				EditorGUI.PropertyField(currentRect, childProperty);
				currentRect.y += EditorGUIUtility.singleLineHeight;
			}

			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty _property, GUIContent _label)
		{
			CollectChildProperties(_property);
			return s_childPropertes.Count * EditorGUIUtility.singleLineHeight;
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

Debug.Log($"property.propertyPath:{property.propertyPath} property.type:{property.type}");
				s_childPropertes.Add(property.Copy());
			}
		}
	}
}