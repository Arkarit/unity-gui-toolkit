using System.Collections.Generic;

namespace GuiToolkit.Editor
{
	public class UiMultiPurposeShaderEditor : ShaderEditorBase
	{
		private readonly List<string> m_RadioList = new List<string>() { "_SpriteSheetAnimation", "_Scrolling" };

		protected override void OnGUI()
		{
			// We don't display the main texture, because it's set by graphics component (sprite)
			DisplayProperty("_MainTex", "Main texture. Note that this is overwritten by CanvasRenderer when used in UI context.");
			DisplayProperty("_Color", "Tint color. This is multiplied with the vertex colors.");
			
			DisplayProperty("_TextureBlendMode");

			GeneralClampWarning();

			LargeSpace();
			Foldout("_FoldoutFeatures", () =>
			{
				KeywordToggle("_UseUIAlphaClip");
				KeywordToggle("_UnscaledTime");

				SmallSpace();
				KeywordToggle("_UseAdditionalTransformation", () =>
				{
					DisplayProperty("_AdditionalTransformation");
				});

				SmallSpace();
				KeywordToggle("_Adjustments", () =>
				{
					DisplayProperty("_Brightness");
					DisplayProperty("_Contrast");
				});

				SmallSpace();
				KeywordToggle("_Disabled", () =>
				{
					DisplayProperty("_DisabledAlpha");
					DisplayProperty("_DisabledDesaturateStrength");
					DisplayProperty("_DisabledBrightness");
				});

				SmallSpace();
				KeywordToggle("_SpriteSheetAnimation", m_RadioList, () =>
				{
					DisplayProperty("_NumSpritesX");
					DisplayProperty("_NumSpritesY");
					DisplayProperty("_FrameRate");
				});
				
				SmallSpace();
				KeywordToggle("_UVRotation", () =>
				{
					DisplayProperty("_RotationSpeed");
					DisplayProperty("_ManualRotationPhaseOffset");
				});

				SmallSpace();
				KeywordToggle("_NoVertexColor");
				
				SmallSpace();
				KeywordToggle("_Scrolling", m_RadioList, () =>
				{
					WarnIfImageClamped("_MainTex");
					DisplayProperty("_ScrollingSpeedU");
					DisplayProperty("_ScrollingSpeedV");
				});

				SmallSpace();
				KeywordToggle("_SecondaryTexture", () =>
				{
					DisplayProperty("_SecondaryTex");
					DisplayProperty("_SecondaryColor");
					KeywordToggle("_SecondaryUseAdditionalTransformation", () =>
					{
						DisplayProperty("_SecondaryAdditionalTransformation");
					});
					var secondaryTextureBlendMode = DisplayProperty("_SecondaryTextureBlendMode");
					var secondaryTextureBlendModeVal = secondaryTextureBlendMode.floatValue;
					if (secondaryTextureBlendModeVal == 0)
						KeywordToggle("_SecondaryTextureBlendModeSeparateAlpha");
					
					KeywordToggle("_SecondaryTextureScrolling", () =>
					{
						WarnIfImageClamped("_SecondaryTex");
						DisplayProperty("_SecondaryTextureScrollingSpeedU");
						DisplayProperty("_SecondaryTextureScrollingSpeedV");
					});
					KeywordToggle("_SecondaryAdjustments", () =>
					{
						DisplayProperty("_SecondaryBrightness");
						DisplayProperty("_SecondaryContrast");
					});
				});
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
