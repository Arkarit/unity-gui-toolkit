using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Draws one <see cref="UiRoundedImage.EdgeGap"/> as a single line while the side is not interrupted,
	/// and unfolds its two measurements underneath once it is.
	///
	/// Four gaps with three values each is a lot of inspector for something most shapes do not use, so the
	/// values only occupy space when they mean something. The side's name goes through PrefixLabel so these
	/// rows line up with every other field in the inspector instead of forming their own ragged column.
	/// </summary>
	[CustomPropertyDrawer(typeof(UiRoundedImage.EdgeGap))]
	public class EdgeGapDrawer : PropertyDrawer
	{
		private static readonly GUIContent WidthLabel = new GUIContent(
			"Width", "Length of the interruption, as a fraction of this side's length.");

		private static readonly GUIContent OffsetLabel = new GUIContent(
			"Offset", "Position along the side, measured from its centre, as a fraction of the side's length.");

		private const float SubIndent = 15f;

		public override float GetPropertyHeight( SerializedProperty _property, GUIContent _label )
		{
			float line = EditorGUIUtility.singleLineHeight;
			if (!IsActive(_property))
				return line;

			return line * 3 + EditorGUIUtility.standardVerticalSpacing * 2;
		}

		public override void OnGUI( Rect _position, SerializedProperty _property, GUIContent _label )
		{
			var activeProp = _property.FindPropertyRelative("Active");
			var widthProp = _property.FindPropertyRelative("Width");
			var offsetProp = _property.FindPropertyRelative("Offset");

			float line = EditorGUIUtility.singleLineHeight;
			float step = line + EditorGUIUtility.standardVerticalSpacing;

			EditorGUI.BeginProperty(_position, _label, _property);

			var row = new Rect(_position.x, _position.y, _position.width, line);
			var afterLabel = EditorGUI.PrefixLabel(row, _label);
			activeProp.boolValue = EditorGUI.Toggle(
				new Rect(afterLabel.x, afterLabel.y, line, line), activeProp.boolValue);

			if (activeProp.boolValue)
			{
				var sub = new Rect(row.x + SubIndent, row.y + step, row.width - SubIndent, line);
				widthProp.floatValue = EditorGUI.Slider(sub, WidthLabel, widthProp.floatValue, 0f, 1f);

				sub.y += step;
				offsetProp.floatValue = EditorGUI.Slider(sub, OffsetLabel, offsetProp.floatValue, -0.5f, 0.5f);
			}

			EditorGUI.EndProperty();
		}

		private static bool IsActive( SerializedProperty _property )
		{
			var activeProp = _property.FindPropertyRelative("Active");
			return activeProp != null && activeProp.boolValue;
		}
	}
}
