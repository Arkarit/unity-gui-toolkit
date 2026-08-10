using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Draws one <see cref="UiRoundedImage.EdgeGap"/> as a single line while it is switched off, and unfolds
	/// its two measurements underneath once it is on.
	///
	/// A shape can carry up to four gaps, and three values each is a lot of inspector for something most
	/// shapes do not use, so the values only take space when they mean something. The gap's name goes
	/// through PrefixLabel, so these rows sit in the same label column as everything else.
	///
	/// How the two numbers are read depends on the shape's Gap Unit, which lives on the component rather
	/// than in the struct: normalized values get sliders with a meaningful range, pixels get plain fields
	/// because no range would be meaningful.
	/// </summary>
	[CustomPropertyDrawer(typeof(UiRoundedImage.EdgeGap))]
	public class EdgeGapDrawer : PropertyDrawer
	{
		private static readonly GUIContent SizeNormalizedLabel = new GUIContent(
			"Size", "Length of the gap, as a fraction of the side it sits on.");

		private static readonly GUIContent SizePixelLabel = new GUIContent(
			"Size", "Length of the gap, in pixels.");

		private static readonly GUIContent OffsetNormalizedLabel = new GUIContent(
			"Offset", "Position of the gap, measured from the centre, as a fraction of the side.");

		private static readonly GUIContent OffsetPixelLabel = new GUIContent(
			"Offset", "Position of the gap, measured from the centre, in pixels.");

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
			var sizeProp = _property.FindPropertyRelative("Size");
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
				bool normalized = IsNormalized(_property);
				var sub = new Rect(row.x + SubIndent, row.y + step, row.width - SubIndent, line);

				if (normalized)
				{
					sizeProp.floatValue = EditorGUI.Slider(sub, SizeNormalizedLabel, sizeProp.floatValue, 0f, 1f);
					sub.y += step;
					offsetProp.floatValue = EditorGUI.Slider(sub, OffsetNormalizedLabel, offsetProp.floatValue, -0.5f, 0.5f);
				}
				else
				{
					// No slider for pixels: there is no upper bound that would mean anything, since the rect
					// can be any size.
					sizeProp.floatValue = Mathf.Max(0f, EditorGUI.FloatField(sub, SizePixelLabel, sizeProp.floatValue));
					sub.y += step;
					offsetProp.floatValue = EditorGUI.FloatField(sub, OffsetPixelLabel, offsetProp.floatValue);
				}
			}

			EditorGUI.EndProperty();
		}

		private static bool IsActive( SerializedProperty _property )
		{
			var activeProp = _property.FindPropertyRelative("Active");
			return activeProp != null && activeProp.boolValue;
		}

		private static bool IsNormalized( SerializedProperty _property )
		{
			// The unit is a property of the shape, not of the individual gap, so it is read from the object
			// this gap belongs to.
			var unitProp = _property.serializedObject.FindProperty("m_gapUnit");
			return unitProp == null || unitProp.enumValueIndex == (int)UiRoundedImage.EGapUnit.Normalized;
		}
	}
}
