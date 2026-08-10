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
		/// The four gaps live behind a foldout, collapsed by default and remembered per project. Each row
		/// draws itself through EdgeGapDrawer, which keeps an unused side down to a single line.
		/// </summary>
		private void DrawEdgeGaps()
		{
			GUILayout.Space(4);

			bool expanded = EditorPrefs.GetBool(FoldoutPrefKey, false);
			bool nowExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(expanded, GapSummary());
			if (nowExpanded != expanded)
				EditorPrefs.SetBool(FoldoutPrefKey, nowExpanded);

			if (nowExpanded)
			{
				// The gaps interrupt the outline, and a filled shape has none. Saying so beats leaving
				// someone to wonder why the sliders do nothing.
				if (m_frameSizeProp.floatValue <= 0f)
					EditorGUILayout.HelpBox("Edge gaps interrupt the frame. With Frame Size 0 the shape is "
						+ "filled and has no outline to interrupt, so they have no effect.", MessageType.Info);

				EditorGUILayout.PropertyField(m_gapTopProp, new GUIContent("Top"));
				EditorGUILayout.PropertyField(m_gapBottomProp, new GUIContent("Bottom"));
				EditorGUILayout.PropertyField(m_gapLeftProp, new GUIContent("Left"));
				EditorGUILayout.PropertyField(m_gapRightProp, new GUIContent("Right"));
			}

			EditorGUILayout.EndFoldoutHeaderGroup();
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
