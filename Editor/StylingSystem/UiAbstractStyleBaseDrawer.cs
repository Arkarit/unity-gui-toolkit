using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Style.Editor
{
	[CustomPropertyDrawer(typeof(UiAbstractStyleBase), true)]
	public class UiAbstractStyleBaseDrawer : PropertyDrawer
	{
		private const float GapAfterTitle = 8;
		private const float GapAfterDisplay = 8;

		private static readonly List<SerializedProperty> s_childPropertes = new();

		public override void OnGUI( Rect _position, SerializedProperty _property, GUIContent _label )
		{
			EditorGUI.BeginProperty(_position, _label, _property);
			Rect currentRect = new Rect(_position.x, _position.y, _position.width, EditorGUIUtility.singleLineHeight);

			var currentStyle = _property.boxedValue as UiAbstractStyleBase;
			if (currentStyle != null)
			{
				EditorGUI.LabelField(currentRect, UiStyleUtility.GetName(currentStyle.SupportedMonoBehaviourType, currentStyle.Name));
				NextRect(ref currentRect, GapAfterTitle);
				var lineRect = new Rect(
					currentRect.x, 
					currentRect.y - 7,
					currentRect.width,
					1
				);

				EditorGUI.DrawRect(lineRect, new Color ( 0.5f,0.5f,0.5f, 1 ) );
			}

			CollectChildProperties(_property);
			foreach (var childProperty in s_childPropertes)
			{
				if (childProperty.name == "m_key")
					continue;

				EditorGUI.PropertyField(currentRect, childProperty);
				NextRect(ref currentRect);
			}

			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty _property, GUIContent _label)
		{
			CollectChildProperties(_property);
			return s_childPropertes.Count * EditorGUIUtility.singleLineHeight + GapAfterTitle + GapAfterDisplay;
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

		private void NextRect(ref Rect rect, float gap = 0)
		{
			rect.y += EditorGUIUtility.singleLineHeight + gap;
		}
	}
}