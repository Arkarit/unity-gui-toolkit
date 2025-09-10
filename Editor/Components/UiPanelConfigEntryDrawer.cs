#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.AssetHandling
{
	[CustomPropertyDrawer(typeof(UiPanelConfig.PanelEntry))]
	public class UiPanelConfigEntryDrawer : PropertyDrawer
	{
		public override void OnGUI( Rect _position, SerializedProperty _prop, GUIContent _label )
		{
			var typeProp  = _prop.FindPropertyRelative("Type");
			var idProp    = _prop.FindPropertyRelative("PanelId");

			float half = _position.width * 0.5f;
			var left   = new Rect(_position.x, _position.y, half - 2, EditorGUIUtility.singleLineHeight);
			var right  = new Rect(_position.x + half + 2, _position.y, half - 2, EditorGUIUtility.singleLineHeight);

			EditorGUI.PropertyField(left,  idProp,   new GUIContent(string.IsNullOrEmpty(typeProp.stringValue) ? "Id" : typeProp.stringValue));
			EditorGUI.PropertyField(right, typeProp, new GUIContent("Type"));
		}

		public override float GetPropertyHeight( SerializedProperty _property, GUIContent _label )
		{
			return EditorGUIUtility.singleLineHeight;
		}
	}
}
#endif
