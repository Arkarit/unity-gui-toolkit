using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Base inspector for UiShapeImage subclasses.
	///
	/// Holds all shared SerializedProperty references and provides helper methods
	/// to draw the common inspector sections in a consistent order. Concrete
	/// subclasses (UiRoundedImageEditor, UiStarEditor, ...) extend this, fetch their
	/// shape-specific properties in their own OnEnable, and compose the layout in
	/// OnInspectorGUI.
	/// </summary>
	public abstract class UiShapeImageEditor : UnityEditor.Editor
	{
		protected SerializedProperty m_SpriteProp;
		protected SerializedProperty m_MaterialProp;
		protected SerializedProperty m_ColorProp;
		protected SerializedProperty m_RaycastTargetProp;
		protected SerializedProperty m_RaycastPaddingProp;
		protected SerializedProperty m_MaskableProp;

		protected SerializedProperty m_frameSizeProp;
		protected SerializedProperty m_fadeSizeProp;
		protected SerializedProperty m_invertMaskProp;
		protected SerializedProperty m_usePaddingProp;
		protected SerializedProperty m_paddingProp;
		protected SerializedProperty m_useFixedSizeProp;
		protected SerializedProperty m_fixedSizeProp;
		protected SerializedProperty m_disabledMaterialProp;
		protected SerializedProperty m_enabledInHierarchyProp;
		protected SerializedProperty m_gradientSimpleProp;
		protected SerializedProperty m_fadeColorProp;
		protected SerializedProperty m_uniformSizeOffsetProp;
		protected SerializedProperty m_sizeOffsetProp;
		protected SerializedProperty m_positionOffsetProp;

		protected SerializedProperty m_typeProp;
		protected SerializedProperty m_fillMethodProp;
		protected SerializedProperty m_fillOriginProp;
		protected SerializedProperty m_fillAmountProp;
		protected SerializedProperty m_fillClockwiseProp;

		protected virtual void OnEnable()
		{
			m_SpriteProp = serializedObject.FindProperty("m_Sprite");
			m_MaterialProp = serializedObject.FindProperty("m_Material");
			m_ColorProp = serializedObject.FindProperty("m_Color");
			m_RaycastTargetProp = serializedObject.FindProperty("m_RaycastTarget");
			m_RaycastPaddingProp = serializedObject.FindProperty("m_RaycastPadding");
			m_MaskableProp = serializedObject.FindProperty("m_Maskable");

			m_frameSizeProp = serializedObject.FindProperty("m_frameSize");
			m_fadeSizeProp = serializedObject.FindProperty("m_fadeSize");
			m_invertMaskProp = serializedObject.FindProperty("m_invertMask");
			m_usePaddingProp = serializedObject.FindProperty("m_usePadding");
			m_paddingProp = serializedObject.FindProperty("m_padding");
			m_useFixedSizeProp = serializedObject.FindProperty("m_useFixedSize");
			m_fixedSizeProp = serializedObject.FindProperty("m_fixedSize");
			m_disabledMaterialProp = serializedObject.FindProperty("m_disabledMaterial");
			m_enabledInHierarchyProp = serializedObject.FindProperty("m_enabledInHierarchy");
			m_gradientSimpleProp = serializedObject.FindProperty("m_gradientSimple");
			m_fadeColorProp = serializedObject.FindProperty("m_fadeColor");
			m_uniformSizeOffsetProp = serializedObject.FindProperty("m_uniformSizeOffset");
			m_sizeOffsetProp = serializedObject.FindProperty("m_sizeOffset");
			m_positionOffsetProp = serializedObject.FindProperty("m_positionOffset");

			m_typeProp = serializedObject.FindProperty("m_Type");
			m_fillMethodProp = serializedObject.FindProperty("m_FillMethod");
			m_fillOriginProp = serializedObject.FindProperty("m_FillOrigin");
			m_fillAmountProp = serializedObject.FindProperty("m_FillAmount");
			m_fillClockwiseProp = serializedObject.FindProperty("m_FillClockwise");
		}

		protected void DrawImageProperties()
		{
			GUILayout.Label("Image Properties", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(m_SpriteProp);

			EditorGUILayout.PropertyField(m_MaterialProp);
			EditorGUILayout.PropertyField(m_disabledMaterialProp); // This is actually not an Image member, but it does not make sense to display it elsewhere

			EditorGUILayout.PropertyField(m_ColorProp);
			EditorGUILayout.PropertyField(m_RaycastTargetProp);
			EditorGUILayout.PropertyField(m_RaycastPaddingProp);
			EditorGUILayout.PropertyField(m_MaskableProp);

			DrawFillProperties();
		}

		/// <summary>
		/// Image Type, and the fill settings behind it.
		///
		/// Only Simple and Filled are offered. A shape builds its own mesh, so Sliced and Tiled have nothing
		/// to slice or tile - and a rounded rectangle drawn as nine patches would not be a rounded rectangle.
		/// Filled works because the shape's mesh is cut down afterwards (see UiShapeFill), which is also why
		/// Preserve Aspect and Use Sprite Mesh stay out of this inspector: those belong to Image's own
		/// geometry, which never runs here, and a control that provably does nothing is worse than a missing
		/// one.
		/// </summary>
		protected void DrawFillProperties()
		{
			var type = (Image.Type)m_typeProp.enumValueIndex;
			bool filled = type == Image.Type.Filled;

			// Not a plain enum popup: the field can hold Sliced or Tiled from a component that was an Image
			// before, and silently showing "Simple" for a stored Sliced would be a lie.
			int index = filled ? 1 : 0;
			int newIndex = EditorGUILayout.Popup(new GUIContent("Image Type",
				"Simple draws the whole shape. Filled cuts it down to Fill Amount."),
				index, s_typeNames);

			if (newIndex != index)
				m_typeProp.enumValueIndex = (int)(newIndex == 1 ? Image.Type.Filled : Image.Type.Simple);

			if (type != Image.Type.Simple && type != Image.Type.Filled)
			{
				EditorGUILayout.HelpBox($"This shape is set to {type}, which it cannot draw - it builds its "
					+ "own mesh. It behaves as Simple. Pick one of the two above to clear this.",
					MessageType.Warning);
			}

			if (!filled)
				return;

			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(m_fillMethodProp, new GUIContent("Fill Method"));

            var method = (Image.FillMethod)m_fillMethodProp.enumValueIndex;
			DrawFillOrigin(method);

			EditorGUILayout.PropertyField(m_fillAmountProp, new GUIContent("Fill Amount"));

			if (method > Image.FillMethod.Vertical)
				EditorGUILayout.PropertyField(m_fillClockwiseProp, new GUIContent("Clockwise"));

			EditorGUI.indentLevel--;
		}

		/// <summary>
		/// Fill Origin is an int whose MEANING depends on the fill method - Image names the same 0 "Left"
		/// for horizontal and "Bottom Left" for Radial90. Drawn as the right set of names, the way Image's
		/// own inspector does it, because "0" tells nobody anything.
		/// </summary>
		private void DrawFillOrigin( Image.FillMethod _method )
		{
			var names = _method switch
			{
				Image.FillMethod.Horizontal => s_originHorizontal,
				Image.FillMethod.Vertical => s_originVertical,
				Image.FillMethod.Radial90 => s_origin90,
				Image.FillMethod.Radial180 => s_origin180,
				_ => s_origin360,
			};

			int origin = Mathf.Clamp(m_fillOriginProp.intValue, 0, names.Length - 1);
			int newOrigin = EditorGUILayout.Popup(new GUIContent("Fill Origin"), origin, names);

			if (newOrigin != m_fillOriginProp.intValue)
				m_fillOriginProp.intValue = newOrigin;
		}

		private static readonly string[] s_typeNames = { "Simple", "Filled" };
		private static readonly string[] s_originHorizontal = { "Left", "Right" };
		private static readonly string[] s_originVertical = { "Bottom", "Top" };
		private static readonly string[] s_origin90 = { "Bottom Left", "Top Left", "Top Right", "Bottom Right" };
		private static readonly string[] s_origin180 = { "Bottom", "Left", "Top", "Right" };
		private static readonly string[] s_origin360 = { "Bottom", "Right", "Top", "Left" };

		protected void DrawSharedShapeProperties( UiShapeImage _shapeImage )
		{
			EditorGUILayout.PropertyField(m_frameSizeProp);
			EditorGUILayout.PropertyField(m_fadeSizeProp);
			EditorGUILayout.PropertyField(m_fadeColorProp);
			EditorGUILayout.PropertyField(m_gradientSimpleProp);

			using (new EditorGUI.DisabledScope(!_shapeImage.maskable))
				EditorGUILayout.PropertyField(m_invertMaskProp);
		}

		protected void DrawSizeAndEnabledness()
		{
			GUILayout.Label("Size", EditorStyles.boldLabel);
			EditorUiUtility.DisplayPropertyConditionally(m_useFixedSizeProp, m_fixedSizeProp);
			EditorUiUtility.DisplayPropertyConditionally(m_usePaddingProp, m_paddingProp);

			EditorGUILayout.PropertyField(m_uniformSizeOffsetProp);
			if (m_uniformSizeOffsetProp.boolValue)
			{
				var xProp = m_sizeOffsetProp.FindPropertyRelative("x");
				var yProp = m_sizeOffsetProp.FindPropertyRelative("y");
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(xProp, new GUIContent("Size Offset"));
				if (EditorGUI.EndChangeCheck())
					yProp.floatValue = xProp.floatValue;
			}
			else
			{
				EditorGUILayout.PropertyField(m_sizeOffsetProp, new GUIContent("Size Offset"));
			}

			// Right below Size Offset, because the two are read as a pair: one grows the shape around its
			// centre, the other moves it.
			EditorGUILayout.PropertyField(m_positionOffsetProp, new GUIContent("Position Offset"));

			GUILayout.Space(10);

			GUILayout.Label("Visual Enabledness", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(m_enabledInHierarchyProp);
		}

		protected void DrawSimpleGradientInline( UiShapeImage _shapeImage )
		{
			if (m_gradientSimpleProp.objectReferenceValue == null)
				return;

			var gradientSimple = (UiGradientSimple)m_gradientSimpleProp.objectReferenceValue;
			var colors = gradientSimple.GetColors();
			Color newColorLeftOrTop = EditorGUILayout.ColorField("Color left or top:", colors.leftOrTop);
			Color newColorRightOrBottom = EditorGUILayout.ColorField("Color right or bottom:", colors.rightOrBottom);
			if (newColorLeftOrTop != colors.leftOrTop || newColorRightOrBottom != colors.rightOrBottom)
			{
				Undo.RecordObject(gradientSimple, "Simple gradient colors change");
				_shapeImage.SetSimpleGradientColors(newColorLeftOrTop, newColorRightOrBottom);
			}
		}
	}
}
