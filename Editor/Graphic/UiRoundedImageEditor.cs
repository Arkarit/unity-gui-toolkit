using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiRoundedImage))]
	public class UiRoundedImageEditor : UiShapeImageEditor
	{
		private const string FoldoutPrefKey = "GuiToolkit.UiRoundedImageEditor.GapsFoldout";

		protected SerializedProperty m_cornerSegmentsProp;
		protected SerializedProperty m_radiusProp;

		protected SerializedProperty m_gapUnitProp;
		protected SerializedProperty m_gapLeftProp;
		protected SerializedProperty m_gapRightProp;
		protected SerializedProperty m_gapTopProp;
		protected SerializedProperty m_gapBottomProp;
		protected SerializedProperty m_gapHorizontalProp;
		protected SerializedProperty m_gapVerticalProp;

		protected override void OnEnable()
		{
			base.OnEnable();
			m_cornerSegmentsProp = serializedObject.FindProperty("m_cornerSegments");
			m_radiusProp = serializedObject.FindProperty("m_radius");

			m_gapUnitProp = serializedObject.FindProperty("m_gapUnit");
			m_gapLeftProp = serializedObject.FindProperty("m_gapLeft");
			m_gapRightProp = serializedObject.FindProperty("m_gapRight");
			m_gapTopProp = serializedObject.FindProperty("m_gapTop");
			m_gapBottomProp = serializedObject.FindProperty("m_gapBottom");
			m_gapHorizontalProp = serializedObject.FindProperty("m_gapHorizontal");
			m_gapVerticalProp = serializedObject.FindProperty("m_gapVertical");
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

			DrawGaps();

			GUILayout.Space(10);
			DrawSizeAndEnabledness();

			DrawSimpleGradientInline(thisUiRoundedImage);

			serializedObject.ApplyModifiedProperties();
		}

		/// <summary>
		/// The gaps, behind a foldout that remembers its state per project.
		///
		/// Which gaps exist depends on what the shape IS. With a frame there is an outline, and a gap
		/// interrupts one of its four sides. Filled there is no outline, so a gap cuts a band right through
		/// instead — one horizontal, one vertical, and both together leave the corners standing. Showing the
		/// four sides in that case would offer settings that cannot do anything.
		/// </summary>
		private void DrawGaps()
		{
			GUILayout.Space(4);

			bool expanded = EditorPrefs.GetBool(FoldoutPrefKey, false);
			bool nowExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(expanded, GapSummary());
			if (nowExpanded != expanded)
				EditorPrefs.SetBool(FoldoutPrefKey, nowExpanded);

			if (nowExpanded)
			{
				EditorGUILayout.PropertyField(m_gapUnitProp, new GUIContent("Unit",
					"Whether the sizes and offsets below are fractions of the side, or pixels."));

				GUILayout.Space(2);

				if (m_frameSizeProp.floatValue > 0f)
				{
					EditorGUILayout.PropertyField(m_gapTopProp, new GUIContent("Top"));
					EditorGUILayout.PropertyField(m_gapBottomProp, new GUIContent("Bottom"));
					EditorGUILayout.PropertyField(m_gapLeftProp, new GUIContent("Left"));
					EditorGUILayout.PropertyField(m_gapRightProp, new GUIContent("Right"));
				}
				else
				{
					EditorGUILayout.PropertyField(m_gapHorizontalProp, new GUIContent("Horizontal",
						"Cuts a horizontal band through the shape."));
					EditorGUILayout.PropertyField(m_gapVerticalProp, new GUIContent("Vertical",
						"Cuts a vertical band through the shape."));
				}
			}

			EditorGUILayout.EndFoldoutHeaderGroup();
		}

		/// <summary>Names what is switched on, so the collapsed header need not be opened to see.</summary>
		private string GapSummary()
		{
			var image = (UiRoundedImage) target;
			var active = new List<string>();

			if (image.FrameSize > 0f)
			{
				if (image.GapTop.Active) active.Add("Top");
				if (image.GapBottom.Active) active.Add("Bottom");
				if (image.GapLeft.Active) active.Add("Left");
				if (image.GapRight.Active) active.Add("Right");
			}
			else
			{
				if (image.GapHorizontal.Active) active.Add("Horizontal");
				if (image.GapVertical.Active) active.Add("Vertical");
			}

			return active.Count == 0 ? "Gaps" : $"Gaps  ({string.Join(", ", active)})";
		}
	}
}
