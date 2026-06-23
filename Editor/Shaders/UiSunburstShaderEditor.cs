using UnityEditor;

namespace GuiToolkit.Editor
{
	public class UiSunburstShaderEditor : ShaderEditorBase
	{
		protected override void OnGUI()
		{
			EditorGUILayout.HelpBox(
				"Do not use sprites that are packed into a Sprite Atlas. " +
				"This shader rotates UVs around the sprite center, which assumes the UVs cover [0,1]. " +
				"Atlas-packed sprites have a sub-rect of the atlas as UVs, so the rotation pivot ends up wrong " +
				"and rays bleed into neighboring atlas sprites. Exclude affected sprites from any Sprite Atlas.",
				MessageType.Warning);

			DisplayProperty("_MainTex", "Primary sunburst texture. Note that this is overwritten by CanvasRenderer when used in UI context.");
			DisplayProperty("_Rotation", "Static rotation of texture 1 in degrees.");

			SmallSpace();
			KeywordToggle("_UseTex2", () =>
			{
				DisplayProperty("_Tex2", "Second texture, layered on top of the first via the Combine mode below.");
				DisplayProperty("_Rotation2", "Static rotation of texture 2 in degrees.");
				DisplayProperty("_Combine", "How texture 1 and texture 2 are combined.");
				DisplayProperty("_GlobalRotation", "Additional rotation applied to the combined result (i.e. on top of Rotation 1 and Rotation 2).");
			});

			bool tex2On = FindProperty("_UseTex2", MaterialProperties).floatValue != 0;

			SmallSpace();
			DisplayProperty("_Color", "Tint multiplied with the combined result. When 'Per-Layer Tint' is on, this is the tint for texture 1 only.");
			if (tex2On)
			{
				KeywordToggle("_UsePerLayerTint", () =>
				{
					DisplayProperty("_Color2", "Tint for texture 2 (applied before combine).");
				});
			}

			SmallSpace();
			KeywordToggle("_UseAnimRotation", () =>
			{
				DisplayProperty("_RotationSpeed", "Rotation speed for texture 1 in degrees per second.");
				if (tex2On)
				{
					DisplayProperty("_Rotation2Speed", "Rotation speed for texture 2 in degrees per second. Opposite sign to 'Speed 1' creates the classic counter-rotating sunburst effect.");
					DisplayProperty("_GlobalRotationSpeed", "Rotation speed of the combined result in degrees per second.");
				}
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
