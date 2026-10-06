using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Draws a <see cref="PerCameraSetting{T}"/> in one line: enable toggle, label, value.
	/// The value stays visible (greyed out) while disabled, so a preset can be prepared before switching it on.
	/// </summary>
	[CustomPropertyDrawer(typeof(PerCameraSettingBase), true)]
	public class PerCameraSettingBaseDrawer : PropertyDrawer
	{
		private const int ToggleWidth = 18;

		public override void OnGUI( Rect _rect, SerializedProperty _property, GUIContent _label )
		{
			EditorGUI.BeginProperty(_rect, _label, _property);

			var enabledProp = _property.FindPropertyRelative(nameof(PerCameraSettingBase.Enabled));
			var valueProp = _property.FindPropertyRelative("Value");
			float lineHeight = EditorGUIUtility.singleLineHeight;
			int indent = EditorGUI.indentLevel;

			var toggleRect = EditorGUI.IndentedRect(new Rect(_rect.x, _rect.y, _rect.width, lineHeight));
			toggleRect.width = ToggleWidth;
			EditorGUI.indentLevel = 0;
			enabledProp.boolValue = EditorGUI.Toggle(toggleRect, enabledProp.boolValue);
			EditorGUI.indentLevel = indent;

			var labelRect = new Rect(_rect.x, _rect.y, EditorGUIUtility.labelWidth, lineHeight);
			labelRect = EditorGUI.IndentedRect(labelRect);
			labelRect.xMin += ToggleWidth;
			EditorGUI.indentLevel = 0;
			EditorGUI.LabelField(labelRect, _label);

			if (valueProp != null)
			{
				var valueRect = new Rect(_rect.x + EditorGUIUtility.labelWidth + 2, _rect.y,
					_rect.width - EditorGUIUtility.labelWidth - 2, lineHeight);

				using (new EditorGUI.DisabledScope(!enabledProp.boolValue))
					EditorGUI.PropertyField(valueRect, valueProp, GUIContent.none);
			}

			EditorGUI.indentLevel = indent;
			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight( SerializedProperty _property, GUIContent _label ) => EditorGUIUtility.singleLineHeight;
	}
}
