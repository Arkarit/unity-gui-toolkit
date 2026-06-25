using UnityEditor;

namespace GuiToolkit.Editor
{
	public class UiCausticsShaderEditor : ShaderEditorBase
	{
		protected override void OnGUI()
		{
			EditorGUILayout.HelpBox(
				"Procedural Worley/Voronoi caustics for UI. Designed as a parametric replacement " +
				"for dark click-blockers when you want a 'light through water' feel. Performance " +
				"is roughly twice a single Voronoi octave — disable 'Use Second Octave' on low-end " +
				"devices if needed.",
				MessageType.Info);

			DisplayProperty("_MainTex", "Optional mask. The alpha channel modulates the caustic output (white = fully visible). Note that this is overwritten by CanvasRenderer when used in UI context.");

			SmallSpace();
			DisplayProperty("_BaseColor", "Color used outside the bright caustic lines. For a click-blocker this is your dim layer — keep alpha around 0.6–0.8 to actually block the scene. With Gradient on, this is the 'shallow' endpoint.");
			KeywordToggle("_UseGradient", () =>
			{
				DisplayProperty("_BaseColor2", "The 'deep' endpoint of the gradient. Typically darker and a bit more opaque than Base — that's what gives the depth feel.");
				DisplayProperty("_GradientAngle", "Direction of the gradient in degrees. 0 = top→bottom (Base at top, Base 2 at bottom). 90 = left→right. Rotates counter-clockwise.");
				DisplayProperty("_GradientPower", "Curve bias on the gradient. 1 = linear. <1 = stays shallow most of the way, then darkens fast at the end. >1 = darkens quickly, then stays deep.");
			});
			DisplayProperty("_HighlightColor", "Color used on the bright caustic lines. Set its alpha equal to Base for uniform dimming; set higher to make caustic spots 'punch through' the dimmer.");

			SmallSpace();
			DisplayProperty("_Scale", "Octave 1 cell density. Higher = smaller / more cells.");
			DisplayProperty("_Speed", "Octave 1 animation speed (cells flow through the field over time).");

			SmallSpace();
			KeywordToggle("_UseSecondOctave", () =>
			{
				DisplayProperty("_Scale2", "Octave 2 cell density. A different (typically higher) value than Scale 1 produces the interference pattern that reads as caustics.");
				DisplayProperty("_Speed2", "Octave 2 animation speed. Opposite sign to Speed 1 makes the two layers cross over each other — the classic 'wavering' look.");
				DisplayProperty("_Combine", "How octave 1 and octave 2 are combined into the final caustic shape. Screen (default) gives soft additive crossings; Multiply gives thinner intersection-only lines; Add is the brightest; Max is the most stable.");
			});

			SmallSpace();
			DisplayProperty("_Threshold", "Width of the FULLY bright core of the caustic line. 0 = no flat core, pure Gaussian peak (softest, most natural). >0 = adds a flat-top section in the middle of each line (sharper, more 'painted' look).");
			DisplayProperty("_EdgeWidth", "σ of the Gaussian halo around each line — controls how wide and soft the fall-off is. Larger = wider, softer glow that fades smoothly into the base color (every pixel has its own intensity); smaller = thin, sharper line.");
			DisplayProperty("_Contrast", "Exponent applied to the caustic shape. >1 = sharper, more contrasty network. <1 = washed-out / hazy.");
			DisplayProperty("_Intensity", "Overall caustic brightness multiplier. Driving above 1 saturates more pixels to the Highlight Color.");

			SmallSpace();
			KeywordToggle("_UseEdgeVariation", () =>
			{
				DisplayProperty("_EdgeVariation", "How much brightness varies between individual cell-cell edges. 0 = all lines exactly the same intensity (uniform mesh). 1 = some lines fully dark, others fully bright. Real pool caustics typically sit around 0.6–0.8. Adds a few ALU per pixel.");
			});

			SmallSpace();
			KeywordToggle("_UseDistortion", () =>
			{
				DisplayProperty("_Distortion", "Amount of sinusoidal UV swim applied before sampling. Adds the 'refracting through water' feel. Even 0.02–0.05 makes a big difference.");
				DisplayProperty("_DistortionFreq", "Spatial frequency of the swim (in waves per UV unit).");
				DisplayProperty("_DistortionSpeed", "Time multiplier for the swim animation.");
			});

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
				DisplayProperty("_ZTest");
				DisplayProperty("_ZWrite");
				MaterialEditor.RenderQueueField();
			});
		}
	}
}
