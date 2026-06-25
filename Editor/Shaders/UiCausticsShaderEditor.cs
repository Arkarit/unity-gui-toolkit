using UnityEditor;

namespace GuiToolkit.Editor
{
	public class UiCausticsShaderEditor : ShaderEditorBase
	{
		protected override void OnGUI()
		{
			EditorGUILayout.HelpBox(
				"Procedural caustics for UI — Drift-style iterative self-feedback algorithm. " +
				"Produces organic, dendritic light patterns with variable per-line brightness " +
				"and continuous flow. Performance scales with Iterations: 3 is cheap, 5 is standard, " +
				"7 is high-detail. Each iteration costs ~6 sin/cos per pixel.",
				MessageType.Info);

			DisplayProperty("_MainTex", "Optional mask. The alpha channel modulates the caustic output (white = fully visible). Note that this is overwritten by CanvasRenderer when used in UI context.");

			SmallSpace();
			DisplayProperty("_BaseColor", "Color used in the dark areas between caustic lines. For a click-blocker this is your dim layer — keep alpha around 0.6–0.8 to actually block the scene. With Gradient on, this is the 'shallow' endpoint.");
			KeywordToggle("_UseGradient", () =>
			{
				DisplayProperty("_BaseColor2", "The 'deep' endpoint of the gradient. Typically darker and a bit more opaque than Base — that's what gives the depth feel.");
				DisplayProperty("_GradientAngle", "Direction of the gradient in degrees. 0 = top→bottom (Base at top, Base 2 at bottom). 90 = left→right. Rotates counter-clockwise.");
				DisplayProperty("_GradientPower", "Curve bias on the gradient. 1 = linear. <1 = stays shallow most of the way, then darkens fast at the end. >1 = darkens quickly, then stays deep.");
			});
			DisplayProperty("_HighlightColor", "Color used on the bright caustic peaks. Set alpha equal to Base for uniform dimming; set lower than Base to make caustic peaks 'punch through' (light shines through more on the bright lines).");

			SmallSpace();
			DisplayProperty("_Scale", "How many caustic cells across the UV. Larger = finer, denser pattern; smaller = bigger, flowier shapes.");
			DisplayProperty("_Speed", "Animation rate. Real pool caustics drift slowly — 0.3–0.6 is naturalistic, >2 looks frantic.");
			DisplayProperty("_Iterations", "How many self-feedback iterations to run. 3 = soft, smooth, cheap. 5 = standard balance. 7 = sharp, intricate, slowest. Each iteration adds ~6 sin/cos per pixel.");

			SmallSpace();
			DisplayProperty("_Density", "The inner per-iteration intensity baseline. Smaller = sharper, thinner caustic lines; larger = softer, more diffuse lighting. The classic ShaderToy value is 0.005.");
			DisplayProperty("_Brightness", "Brightness offset applied after iteration averaging. Higher = larger lit area, fewer dark gaps; lower = darker overall with only the brightest peaks surviving.");
			DisplayProperty("_Sharpness", "Inner exponent on the averaged field. ~1.4 (classic) gives the typical caustic shape. Lower = washed-out / hazy; higher = harsher transitions.");
			DisplayProperty("_Contrast", "Final exponent — the punchiness control. 8 (classic) makes only the brightest peaks fully visible. Lower = wider, glowy lines; higher = sharp, isolated bright lines.");
			DisplayProperty("_Intensity", "Overall caustic brightness multiplier. Above 1 saturates more pixels to the Highlight Color.");

			SmallSpace();
			KeywordToggle("_UseTileBreak", () =>
			{
				DisplayProperty("_TileBreak", "How strongly to break the intrinsic sin/cos periodicity. The iterative caustic naturally tiles every 1/Scale UV units; a value-noise warp on the internal position p shifts adjacent 'tiles' into different parts of the trig field so they look uncorrelated. ~6 (≈ one caustic period in p-space) is the typical sweet spot. 0 = visible tiling at Scale > 1; >10 = warp dominates the pattern.");
				DisplayProperty("_TileBreakFreq", "How quickly the warp varies across the image. Low = large patches of similar warp (smooth, broad variation). High = warp changes rapidly (more chaotic, hides any large-scale tile structure).");
			});

			SmallSpace();
			KeywordToggle("_UseDistortion", () =>
			{
				DisplayProperty("_Distortion", "Amount of sinusoidal UV swim applied before the caustic computation. Adds extra warble on top of the iterative flow. Usually not needed — keep small (0.01–0.05).");
				DisplayProperty("_DistortionFreq", "Spatial frequency of the swim (waves per UV unit).");
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
