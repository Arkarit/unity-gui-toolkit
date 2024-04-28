using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Style.Editor
{
	[CustomPropertyDrawer(typeof(UiSkin), true)]
	public class UiSkinDrawer : PropertyDrawer
	{
		SerializedProperty 	m_nameProp;
		SerializedProperty m_stylesProp;

		public override void OnGUI(Rect _position, SerializedProperty _property, GUIContent _label)
		{
			EditorGUI.BeginProperty(_position, _label, _property);
			SetProps(_property);
			Rect currentRect = new Rect(_position.x, _position.y, _position.width, EditorGUIUtility.singleLineHeight);

			EditorGUI.LabelField(currentRect, $"Style: {m_nameProp.stringValue}", EditorStyles.boldLabel);
			NextRect(ref currentRect);
			EditorGUI.PropertyField(currentRect, m_nameProp);
			NextRect(ref currentRect);
			for (int i = 0; i < m_stylesProp.arraySize; i++)
			{
				SerializedProperty styleProp = m_stylesProp.GetArrayElementAtIndex(i);
				EditorGUI.PropertyField(currentRect, styleProp);
				NextRect(ref currentRect, styleProp);
			}
			
			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty _property, GUIContent _label)
		{
			SetProps(_property);
			float result = EditorGUIUtility.singleLineHeight * 2;
			for (int i=0; i<m_stylesProp.arraySize; i++)
				result += EditorGUI.GetPropertyHeight(m_stylesProp.GetArrayElementAtIndex(i));

			return result;
		}

		private void NextRect(ref Rect _rect, float _gap = 0)
		{
			_rect.y += EditorGUIUtility.singleLineHeight + _gap;
		}

		private void NextRect(ref Rect _rect, SerializedProperty _property, float _gap = 0)
		{
			_rect.y += EditorGUI.GetPropertyHeight(_property) + _gap;
		}

		private void SetProps(SerializedProperty _property)
		{
			m_nameProp = _property.FindPropertyRelative("m_name");
			m_stylesProp = _property.FindPropertyRelative("m_styles");
		}
	}
}