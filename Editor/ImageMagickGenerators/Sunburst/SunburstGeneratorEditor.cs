using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(SunburstGenerator))]
	public class SunburstGeneratorEditor : UnityEditor.Editor
	{
		private const double DebounceSeconds = 0.18;
		private static readonly Vector2Int PreviewSize = new(256, 256);
		private const int PreviewSupersampling = 2;

		private SerializedProperty m_rayCountProp;
		private SerializedProperty m_dutyCycleProp;
		private SerializedProperty m_innerRadiusProp;
		private SerializedProperty m_fillInnerCircleProp;
		private SerializedProperty m_outerRadiusProp;
		private SerializedProperty m_rayBaseWidthProp;
		private SerializedProperty m_rayBaseWidthOffsetProp;
		private SerializedProperty m_rayTipWidthProp;
		private SerializedProperty m_rotationProp;
		private SerializedProperty m_rayColorProp;
		private SerializedProperty m_backgroundColorProp;
		private SerializedProperty m_rayGradientProp;
		private SerializedProperty m_edgeSoftnessProp;
		private SerializedProperty m_supersamplingProp;
		private SerializedProperty m_outputSizeProp;
		private SerializedProperty m_livePreviewProp;
		private SerializedProperty m_outputPathProp;
		private SerializedProperty m_incrementalProp;
		private SerializedProperty m_randomnessProp;
		private SerializedProperty m_seedProp;

		private Texture2D m_previewTex;
		private string m_lastError;
		private double m_lastChangeTime = -1;
		private bool m_previewBusy;
		private bool m_needsInitialPreview;

		private void OnEnable()
		{
			m_rayCountProp = serializedObject.FindProperty(nameof(SunburstGenerator.RayCount));
			m_dutyCycleProp = serializedObject.FindProperty(nameof(SunburstGenerator.DutyCycle));
			m_innerRadiusProp = serializedObject.FindProperty(nameof(SunburstGenerator.InnerRadiusRatio));
			m_fillInnerCircleProp = serializedObject.FindProperty(nameof(SunburstGenerator.FillInnerCircle));
			m_outerRadiusProp = serializedObject.FindProperty(nameof(SunburstGenerator.OuterRadiusRatio));
			m_rayBaseWidthProp = serializedObject.FindProperty(nameof(SunburstGenerator.RayBaseWidth));
			m_rayBaseWidthOffsetProp = serializedObject.FindProperty(nameof(SunburstGenerator.RayBaseWidthOffset));
			m_rayTipWidthProp = serializedObject.FindProperty(nameof(SunburstGenerator.RayTipWidth));
			m_rotationProp = serializedObject.FindProperty(nameof(SunburstGenerator.Rotation));
			m_rayColorProp = serializedObject.FindProperty(nameof(SunburstGenerator.RayColor));
			m_backgroundColorProp = serializedObject.FindProperty(nameof(SunburstGenerator.BackgroundColor));
			m_rayGradientProp = serializedObject.FindProperty(nameof(SunburstGenerator.RayGradient));
			m_edgeSoftnessProp = serializedObject.FindProperty(nameof(SunburstGenerator.EdgeSoftness));
			m_supersamplingProp = serializedObject.FindProperty(nameof(SunburstGenerator.Supersampling));
			m_outputSizeProp = serializedObject.FindProperty(nameof(SunburstGenerator.OutputSize));
			m_livePreviewProp = serializedObject.FindProperty(nameof(SunburstGenerator.LivePreview));
			m_outputPathProp = serializedObject.FindProperty(nameof(SunburstGenerator.OutputPath));
			m_incrementalProp = serializedObject.FindProperty(nameof(SunburstGenerator.Incremental));
			m_randomnessProp = serializedObject.FindProperty(nameof(SunburstGenerator.Randomness));
			m_seedProp = serializedObject.FindProperty(nameof(SunburstGenerator.Seed));

			EditorApplication.update += OnEditorUpdate;
			m_needsInitialPreview = true;
		}

		private void OnDisable()
		{
			EditorApplication.update -= OnEditorUpdate;
			if (m_previewTex != null)
			{
				DestroyImmediate(m_previewTex);
				m_previewTex = null;
			}
		}

		private void OnEditorUpdate()
		{
			var generator = target as SunburstGenerator;
			if (generator == null)
				return;

			if (m_previewBusy)
				return;

			if (m_needsInitialPreview && generator.LivePreview && ImageMagickRunner.MagickExecutableFound)
			{
				m_needsInitialPreview = false;
				RegeneratePreview();
				return;
			}

			if (m_lastChangeTime > 0 && EditorApplication.timeSinceStartup - m_lastChangeTime > DebounceSeconds)
			{
				m_lastChangeTime = -1;
				if (generator.LivePreview)
					RegeneratePreview();
			}
		}

		public override void OnInspectorGUI()
		{
			var generator = (SunburstGenerator)target;
			serializedObject.Update();

			EditorUiUtility.WithHeadline("Sunburst Parameters", () =>
			{
				EditorGUI.BeginChangeCheck();

				EditorGUILayout.PropertyField(m_rayCountProp);
				EditorGUILayout.PropertyField(m_dutyCycleProp);
				EditorGUILayout.PropertyField(m_innerRadiusProp);
				using (new EditorGUI.DisabledScope(m_innerRadiusProp.floatValue <= 1e-5f))
				{
					EditorGUILayout.PropertyField(m_fillInnerCircleProp);
				}
				EditorGUILayout.PropertyField(m_outerRadiusProp);
				EditorGUILayout.PropertyField(m_rayBaseWidthProp);
				EditorGUILayout.PropertyField(m_rayBaseWidthOffsetProp);
				EditorGUILayout.PropertyField(m_rayTipWidthProp);
				EditorGUILayout.PropertyField(m_rotationProp);
				EditorGUILayout.PropertyField(m_rayColorProp);
				EditorGUILayout.PropertyField(m_backgroundColorProp);
				EditorGUILayout.PropertyField(m_rayGradientProp);
				EditorGUILayout.PropertyField(m_randomnessProp);
				using (new EditorGUI.DisabledScope(m_randomnessProp.floatValue <= 0.0001f))
				{
					EditorGUILayout.PropertyField(m_seedProp);
				}
				EditorGUILayout.PropertyField(m_edgeSoftnessProp);
				EditorGUILayout.PropertyField(m_supersamplingProp);
				EditorGUILayout.PropertyField(m_outputSizeProp);

				if (EditorGUI.EndChangeCheck())
				{
					serializedObject.ApplyModifiedProperties();
					m_lastChangeTime = EditorApplication.timeSinceStartup;
				}
			});

			EditorUiUtility.WithHeadline("Preview", () =>
			{
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(m_livePreviewProp);
				if (EditorGUI.EndChangeCheck())
				{
					serializedObject.ApplyModifiedProperties();
					if (m_livePreviewProp.boolValue)
						m_lastChangeTime = EditorApplication.timeSinceStartup;
				}

				GUILayout.Space(4);

				if (!ImageMagickRunner.MagickExecutableFound)
				{
					EditorGUILayout.HelpBox(
						"ImageMagick not found. Open 'Window/ImageMagick Bridge' to install or configure the executable path.",
						MessageType.Warning);
				}
				else
				{
					DrawPreview();
				}
			});

			EditorUiUtility.WithHeadline("Output", () =>
			{
				EditorGUILayout.PropertyField(m_outputPathProp);
				EditorGUILayout.PropertyField(m_incrementalProp);

				if (m_incrementalProp.boolValue)
				{
					string nextPath = generator.ResolveTargetPath();
					if (!string.IsNullOrEmpty(nextPath))
						EditorGUILayout.LabelField("Next file", nextPath, EditorStyles.miniLabel);
				}

				GUILayout.Space(6);

				using (new EditorGUI.DisabledScope(!ImageMagickRunner.MagickExecutableFound))
				{
					EditorUiUtility.Centered(() =>
					{
						if (GUILayout.Button(new GUIContent("   Generate   ",
							    "Render at full quality and write to the resolved target path."),
							    GUILayout.Height(40)))
						{
							GenerateFull();
						}
					});
				}

				if (!string.IsNullOrEmpty(m_lastError))
				{
					GUILayout.Space(6);
					EditorGUILayout.HelpBox(m_lastError, MessageType.Error);
				}
			});

			serializedObject.ApplyModifiedProperties();
		}

		private void DrawPreview()
		{
			float side = Mathf.Min(EditorGUIUtility.currentViewWidth - 60f, 300f);

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			var rect = GUILayoutUtility.GetRect(side, side, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));

			if (m_previewTex != null)
				GUI.DrawTexture(rect, m_previewTex, ScaleMode.ScaleToFit, true);
			else if (m_previewBusy)
				EditorGUI.LabelField(rect, "Rendering...", EditorStyles.centeredGreyMiniLabel);
			else
				EditorGUI.LabelField(rect, "(no preview yet)", EditorStyles.centeredGreyMiniLabel);
		}

		private void RegeneratePreview()
		{
			var generator = target as SunburstGenerator;
			if (generator == null)
				return;

			m_previewBusy = true;
			try
			{
				var bytes = generator.RenderPreviewBytes(PreviewSize, PreviewSupersampling, out string error);
				if (bytes == null)
				{
					m_lastError = error;
					return;
				}

				if (m_previewTex == null)
				{
					m_previewTex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
					{
						hideFlags = HideFlags.HideAndDontSave,
						filterMode = FilterMode.Bilinear,
					};
				}

				if (m_previewTex.LoadImage(bytes))
				{
					m_lastError = null;
					Repaint();
				}
				else
				{
					m_lastError = "Preview render produced data but Texture2D.LoadImage rejected it.";
				}
			}
			finally
			{
				m_previewBusy = false;
			}
		}

		private void GenerateFull()
		{
			var generator = target as SunburstGenerator;
			if (generator == null)
				return;

			if (generator.Generate(out string writtenPath, out string error))
			{
				m_lastError = null;
				UiLog.Log($"Sunburst written to {writtenPath}");
			}
			else
			{
				m_lastError = error;
				UiLog.LogError($"Sunburst generation failed: {error}");
			}
		}
	}
}
