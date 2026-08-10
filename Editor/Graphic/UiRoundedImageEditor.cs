using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiRoundedImage))]
	public class UiRoundedImageEditor : UiShapeImageEditor
	{
		private const string FoldoutPrefKey = "GuiToolkit.UiRoundedImageEditor.EdgeGapsFoldout";

		protected SerializedProperty m_cornerSegmentsProp;
		protected SerializedProperty m_radiusProp;

		protected SerializedProperty m_gapLeftProp;
		protected SerializedProperty m_gapRightProp;
		protected SerializedProperty m_gapTopProp;
		protected SerializedProperty m_gapBottomProp;

		protected override void OnEnable()
		{
			base.OnEnable();
			m_cornerSegmentsProp = serializedObject.FindProperty("m_cornerSegments");
			m_radiusProp = serializedObject.FindProperty("m_radius");

			m_gapLeftProp = serializedObject.FindProperty("m_gapLeft");
			m_gapRightProp = serializedObject.FindProperty("m_gapRight");
			m_gapTopProp = serializedObject.FindProperty("m_gapTop");
			m_gapBottomProp = serializedObject.FindProperty("m_gapBottom");
		}

		public override void OnInspectorGUI()
		{
			var thisUiRoundedImage = (UiRoundedImage) target;

			DrawImageProperties();
			GUILayout.Space(10);

			GUILayout.Label("Rounded Image Properties", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(m_cornerSegmentsProp);
			EditorGUILayout.PropertyField(m_radiusProp);
			DrawSharedShapeProperties(thisUiRoundedImage);

			DrawEdgeGaps();

			GUILayout.Space(10);
			DrawSizeAndEnabledness();

			DrawSimpleGradientInline(thisUiRoundedImage);

			serializedObject.ApplyModifiedProperties();
		}

		/// <summary>
		/// Four gaps with three values each is twelve controls, so they live behind a foldout — collapsed by
		/// default, and the state is remembered per project rather than per selection.
		/// </summary>
		private void DrawEdgeGaps()
		{
			bool expanded = EditorPrefs.GetBool(FoldoutPrefKey, false);
			bool nowExpanded = EditorGUILayout.Foldout(expanded, GapSummary(), true, EditorStyles.foldoutHeader);
			if (nowExpanded != expanded)
				EditorPrefs.SetBool(FoldoutPrefKey, nowExpanded);

			if (!nowExpanded)
				return;

			using (new EditorGUI.IndentLevelScope())
			{
				// The gaps interrupt the outline, and a filled shape has none. Saying so beats leaving
				// someone to wonder why the sliders do nothing.
				if (m_frameSizeProp.floatValue <= 0f)
					EditorGUILayout.HelpBox("Edge gaps interrupt the frame. With Frame Size 0 the shape is "
						+ "filled and has no outline to interrupt, so they have no effect.", MessageType.Info);

				DrawGapRow(m_gapTopProp, "Top");
				DrawGapRow(m_gapBottomProp, "Bottom");
				DrawGapRow(m_gapLeftProp, "Left");
				DrawGapRow(m_gapRightProp, "Right");
			}
		}

		private static void DrawGapRow( SerializedProperty _gapProp, string _label )
		{
			var activeProp = _gapProp.FindPropertyRelative("Active");
			var widthProp = _gapProp.FindPropertyRelative("Width");
			var offsetProp = _gapProp.FindPropertyRelative("Offset");

			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.LabelField(_label, GUILayout.Width(52));
				activeProp.boolValue = EditorGUILayout.Toggle(activeProp.boolValue, GUILayout.Width(16));

				using (new EditorGUI.DisabledScope(!activeProp.boolValue))
				{
					// Zero indent inside the row: the outer indent already applies to the whole line, and
					// applying it again to each field would push the numbers off the right edge.
					int savedIndent = EditorGUI.indentLevel;
					EditorGUI.indentLevel = 0;

					EditorGUILayout.LabelField(
						new GUIContent("W", "Width of the gap, as a fraction of this side's length"),
						GUILayout.Width(14));
					widthProp.floatValue = Mathf.Clamp01(
						EditorGUILayout.FloatField(widthProp.floatValue, GUILayout.Width(46)));

					EditorGUILayout.LabelField(
						new GUIContent("O", "Offset from the side's centre, as a fraction of the side's length"),
						GUILayout.Width(14));
					offsetProp.floatValue = Mathf.Clamp(
						EditorGUILayout.FloatField(offsetProp.floatValue, GUILayout.Width(46)), -0.5f, 0.5f);

					EditorGUI.indentLevel = savedIndent;
				}
			}
		}

		/// <summary>Names the interrupted sides in the collapsed header, so it does not have to be opened to see.</summary>
		private string GapSummary()
		{
			var image = (UiRoundedImage) target;
			if (!image.HasAnyGap)
				return "Edge Gaps";

			var sides = new System.Collections.Generic.List<string>();
			if (image.GapTop.Active) sides.Add("Top");
			if (image.GapBottom.Active) sides.Add("Bottom");
			if (image.GapLeft.Active) sides.Add("Left");
			if (image.GapRight.Active) sides.Add("Right");

			return $"Edge Gaps  ({string.Join(", ", sides)})";
		}
	}
}
