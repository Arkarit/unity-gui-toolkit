using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Inspector for UI_AngleGradient materials: a live preview of the frame, the colours, the shape and the
	/// animation, with stencil and blending folded away.
	/// </summary>
	public class UiAngleGradientShaderEditor : ShaderEditorBase
	{
		private const string AnimatePreviewKey = "GuiToolkit.UiAngleGradientShaderEditor.AnimatePreview";
		private const float PreviewHeight = 96;
		private const float PreviewFrame = 10;

		protected override void OnGUI()
		{
			EditorGUILayout.HelpBox(
				"Meant for a frame: a UiRoundedImage with a Frame Size and no sprite (or any graphic whose UVs span its rect). " +
				"Every graphic with this material looks the same; for values per frame or per style, use the UiAngleGradient component instead.",
				MessageType.Info);

			SmallSpace();
			DrawPreview();

			LargeSpace();
			Header("Colours");
			DisplayProperty("_ColorBase", "The colour between the highlights.");
			DisplayProperty("_ColorHighlight", "The colour at the highlights.");
			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.Space(EditorGUIUtility.labelWidth);
				if (GUILayout.Button("Swap Colours", EditorStyles.miniButton))
					SwapColours();
			}

			LargeSpace();
			Header("Shape");
			var count = DisplayProperty("_Count", "How many highlights per full turn. 4 puts one in the middle of each side of a rectangle.");
			if (count != null && !count.hasMixedValue && !Mathf.Approximately(count.floatValue, Mathf.Round(count.floatValue)))
				EditorGUILayout.HelpBox("A count that is not a whole number leaves a seam on the left of the frame.", MessageType.Warning);

			DisplayProperty("_Sharpness", "1 is a soft wave; higher values make the highlights narrower and leave more of the base colour.");
			var rotation = DisplayProperty("_Rotation", "Turns the pattern, in degrees.");
			if (rotation != null && count != null && count.floatValue > 0)
			{
				using (new EditorGUILayout.HorizontalScope())
				{
					GUILayout.Space(EditorGUIUtility.labelWidth);
					if (GUILayout.Button("Highlights on Sides", EditorStyles.miniButtonLeft))
						rotation.floatValue = 0;
					if (GUILayout.Button("Highlights between", EditorStyles.miniButtonRight))
						rotation.floatValue = 180f / count.floatValue;
				}
			}

			LargeSpace();
			Header("Animation");
			var speed = DisplayProperty("_Speed", "Turns the pattern over time, in degrees per second. 0 stands still; negative turns clockwise.");
			if (speed != null && !speed.hasMixedValue)
			{
				var text = Mathf.Approximately(speed.floatValue, 0)
					? "Stands still."
					: $"One full turn every {360f / Mathf.Abs(speed.floatValue):0.##} s; a highlight passes a point every {360f / Mathf.Abs(speed.floatValue) / Mathf.Max(1, count?.floatValue ?? 1):0.##} s.";
				EditorGUILayout.LabelField(" ", text, EditorStyles.miniLabel);
			}

			LargeSpace();
			Foldout("_FoldoutStencil", () =>
			{
				DisplayProperty("_StencilComp");
				DisplayProperty("_Stencil");
				DisplayProperty("_StencilOp");
				DisplayProperty("_StencilWriteMask");
				DisplayProperty("_StencilReadMask");
				DisplayProperty("_ColorMask");
				SmallSpace();
				DisplayProperty("_UseUIAlphaClip");
			});

			LargeSpace();
			Foldout("_FoldoutBlendMode", () =>
			{
				DisplayProperty("_SrcBlend");
				DisplayProperty("_DstBlend");
				DisplayBlendingHelper();
				SmallSpace();
				DisplayProperty("_Cull");
				MaterialEditor.RenderQueueField();
			});
		}

		private void DrawPreview()
		{
			var animate = SessionState.GetBool(AnimatePreviewKey, true);
			var rect = EditorGUILayout.GetControlRect(false, PreviewHeight);

			if (Event.current.type == EventType.Repaint && Material != null)
			{
				// The material draws the whole rect; the inner part is covered again, so what stays is the frame.
				EditorGUI.DrawPreviewTexture(rect, Texture2D.whiteTexture, Material, ScaleMode.StretchToFill);
				var inner = new Rect(rect.x + PreviewFrame, rect.y + PreviewFrame, rect.width - 2 * PreviewFrame, rect.height - 2 * PreviewFrame);
				EditorGUI.DrawRect(inner, EditorGUIUtility.isProSkin ? new Color(0.16f, 0.16f, 0.16f) : new Color(0.8f, 0.8f, 0.8f));
			}

			var toggleRect = new Rect(rect.x + PreviewFrame + 6, rect.y + PreviewFrame + 4, 140, EditorGUIUtility.singleLineHeight);
			var newAnimate = GUI.Toggle(toggleRect, animate, " Animate Preview");
			if (newAnimate != animate)
				SessionState.SetBool(AnimatePreviewKey, newAnimate);

			// The shader turns with _Time; the inspector only redraws when asked.
			if (newAnimate)
				MaterialEditor.Repaint();
		}

		private void SwapColours()
		{
			var colorBase = FindProperty("_ColorBase", MaterialProperties);
			var colorHighlight = FindProperty("_ColorHighlight", MaterialProperties);
			(colorBase.colorValue, colorHighlight.colorValue) = (colorHighlight.colorValue, colorBase.colorValue);
		}

		private static void Header( string _label ) => EditorGUILayout.LabelField(_label, EditorStyles.boldLabel);
	}
}
