using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace GuiToolkit
{
	[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
	public class ReadOnlyDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight( SerializedProperty property, GUIContent label )
		{
			return EditorGUI.GetPropertyHeight(property, label, true);
		}

		public override void OnGUI( Rect position, SerializedProperty property, GUIContent label )
		{
			if (property.isArray && property.propertyType != SerializedPropertyType.String)
			{
				DrawArrayReadOnly(position, property, label);
				return;
			}

			bool prevState = GUI.enabled;
			GUI.enabled = false;
			EditorGUI.PropertyField(position, property, label, true);
			GUI.enabled = prevState;
		}

		private void DrawArrayReadOnly( Rect position, SerializedProperty property, GUIContent label )
		{
			property.isExpanded = EditorGUI.Foldout(
				new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
				property.isExpanded, label, true);

			if (!property.isExpanded)
				return;

			EditorGUI.indentLevel++;
			int size = property.arraySize;
			for (int i = 0; i < size; i++)
			{
				SerializedProperty element = property.GetArrayElementAtIndex(i);

				Rect elementRect = EditorGUI.IndentedRect(
					new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight * (i + 1),
					position.width, EditorGUIUtility.singleLineHeight));

				bool prev = GUI.enabled;
				GUI.enabled = false;
				EditorGUI.PropertyField(elementRect, element, new GUIContent($"Element {i}"), true);
				GUI.enabled = prev;
			}
			EditorGUI.indentLevel--;
		}
	}
#endif

	public class ReadOnlyAttribute : PropertyAttribute { }
}