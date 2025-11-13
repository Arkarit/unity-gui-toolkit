Shader "UIToolkit/UI_MultiPurpose"
{
	Properties
	{
		_FoldoutTextures ("Textures", Float) = 1
		_MainTex ("Base (RGB), Alpha (A)", 2D) = "white" {}
		_Color ("Tint", color) = (1,1,1,1)
		[KeywordEnum(Transparent,Additive,Multiplicative,TransparentPremultiplied)] _TextureBlendMode("Mask Blend Mode", float) = 0

		_FoldoutStencil ("Stencil", Float) = 0
		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255
		_ColorMask ("Color Mask", Float) = 15

		_FoldoutFeatures ("Features", Float) = 1
		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

		[Toggle(UseAdditionalTransformation)] _UseAdditionalTransformation("Additional Transformation", Float) = 0
		_AdditionalTransformation("x,y = Pos, z,w = Scale", Vector) = (0,0,1,1)
		
		[Toggle(SecondaryUseAdditionalTransformation)] _SecondaryUseAdditionalTransformation("Secondary Additional Transformation", Float) = 0
		_SecondaryAdditionalTransformation("x,y = Pos, z,w = Scale", Vector) = (0,0,1,1)


		[Toggle(UnscaledTime)] _UnscaledTime("Time Unscaled", Float) = 0

		[Toggle(NoVertexColor)] _NoVertexColor("No Vertex Color", Float) = 0

		[Toggle(Disabled)] _Disabled("Disabled", Float) = 0
		_DisabledAlpha("Disabled Alpha", Range(0,1)) = 0.7
		_DisabledDesaturateStrength("Disabled Desaturate Strength", Range(0,1)) = 0.8
		_DisabledBrightness("Disabled Brightness", Range(0,1)) = 0.7

		[Toggle(SpriteSheetAnimation)] _SpriteSheetAnimation("Sprite Sheet Animation", Float) = 0
		_NumSpritesX("Num sprites X", Float) = 1
		_NumSpritesY("Num sprites Y", Float) = 1
		_FrameRate("Frame rate", Float) = 20

		[Toggle(UVRotation)] _UVRotation("UV Rotation", Float) = 0
		_RotationSpeed("Rotation Speed", Range(-10,10)) = 1

		[Toggle(Scrolling)] _Scrolling("Scrolling", Float) = 0
		_ScrollingSpeedU("Speed U", Range(-1,1)) = .2
		_ScrollingSpeedV("Speed V", Range(-1,1)) = .2

		[Toggle(Adjustments)] _Adjustments("Adjustments", Float) = 0
		_Brightness("Brightness", Range(-1,1)) = 0
		_Contrast("Contrast", Range(0,5)) = 1

		[Toggle(SecondaryTexture)] _SecondaryTexture("Secondary Texture", float) = 0
		_SecondaryTex ("Base (RGB), Alpha (A)", 2D) = "white" {}
		_SecondaryColor ("Color", color) = (1,1,1,1)
		[KeywordEnum(Transparent,Additive,Multiplicative)] _SecondaryTextureBlendMode("Secondary Texture Blend Mode", float) = 0
		[Toggle(SecondaryTextureBlendModeSeparateAlpha)] _SecondaryTextureBlendModeSeparateAlpha("Secondary Texture Blend Mode separate alpha", float) = 0
		[Toggle(SecondaryTextureScrolling)] _SecondaryTextureScrolling("Secondary Texture Scrolling", float) = 0
		_SecondaryTextureScrollingSpeedU("Speed U", Range(-1,1)) = .2
		_SecondaryTextureScrollingSpeedV("Speed V", Range(-1,1)) = .2
		[Toggle(SecondaryAdjustments)] _SecondaryAdjustments("Adjustments", Float) = 0
		_SecondaryBrightness("Brightness", Range(-1,1)) = 0
		_SecondaryContrast("Contrast", Range(0,5)) = 1

		_FoldoutBlendMode ("Blend Mode, Culling, ZTest, ZWrite, Render Queue", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("BlendSource", Float) = 5
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("BlendDestination", Float) = 10
		[Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4 // less or equal -> default
		[Toggle] _ZWrite ("ZWrite", Float) = 0
	}
	SubShader
	{
		LOD 100

		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PreviewType"="Plane"
		}

		Stencil
		{
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp]
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}

		Cull [_Cull]
		Lighting Off
		ZWrite [_ZWrite]
		ZTest [_ZTest]
		Blend [_SrcBlend] [_DstBlend]
		ColorMask [_ColorMask]

		Pass
		{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			#pragma multi_compile_local __ UNITY_UI_CLIP_RECT
			#pragma multi_compile_local __ UNITY_UI_ALPHACLIP
			#pragma multi_compile_local __ Disabled
			#pragma multi_compile_local __ NoVertexColor
			#pragma multi_compile_local __ SpriteSheetAnimation Scrolling
			#pragma multi_compile_local __ UVRotation
			#pragma shader_feature_local __ SecondaryTexture
			#pragma multi_compile __ CSharpHandledFeatures
			#pragma shader_feature_local __ _TEXTUREBLENDMODE_TRANSPARENT _TEXTUREBLENDMODE_TRANSPARENTPREMULTIPLIED _TEXTUREBLENDMODE_ADDITIVE _TEXTUREBLENDMODE_MULTIPLICATIVE
			#pragma shader_feature_local __ Adjustments
			#pragma shader_feature_local __ UseAdditionalTransformation
			#pragma shader_feature_local __ SecondaryUseAdditionalTransformation
			#pragma shader_feature_local __ UnscaledTime

			#ifdef SecondaryTexture
				#pragma shader_feature_local __ _SECONDARYTEXTUREBLENDMODE_TRANSPARENT _SECONDARYTEXTUREBLENDMODE_ADDITIVE _SECONDARYTEXTUREBLENDMODE_MULTIPLICATIVE
				#pragma shader_feature_local __ SecondaryTextureBlendModeSeparateAlpha
				#pragma shader_feature_local __ SecondaryTextureScrolling
				#pragma shader_feature_local __ SecondaryAdjustments
			#endif

			struct appdata_t
			{
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
				half4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;
				float4 worldPosition : TEXCOORD1;
				#ifdef SecondaryTexture
					float2 texcoordSecondary : TEXCOORD2;
				#endif

				half4 color : COLOR;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _MainTex_AdditionalTransformation;
			half4 _Color;

			bool _UseClipRect;
			float4 _ClipRect;

			bool _UseAlphaClip;

			#ifdef Disabled
				float _DisabledAlpha;
				float _DisabledDesaturateStrength;
				float _DisabledBrightness;
			#endif

			#ifdef SpriteSheetAnimation
				float _NumSpritesX;
				float _NumSpritesY;
				float _FrameRate;
			#endif

			float _ScrollingSpeedU;
			float _ScrollingSpeedV;

			#ifdef UVRotation
				float _RotationSpeed;
			#endif

			#ifdef Adjustments
				float _Brightness;
				float _Contrast;
			#endif

			#ifdef SecondaryTexture
				sampler2D _SecondaryTex;
				float4 _SecondaryTex_ST;
				half4 _SecondaryColor;
				half _SecondaryTextureBlendModeSeparateAlpha;
			#endif

			#ifdef SecondaryTextureScrolling
				float _SecondaryTextureScrollingSpeedU;
				float _SecondaryTextureScrollingSpeedV;
			#endif

			#ifdef SecondaryAdjustments
				float _SecondaryBrightness;
				float _SecondaryContrast;
			#endif

			#ifdef CSharpHandledFeatures
				float2 _UvOffsetMain;
				float _AlphaMain;
				#ifdef SecondaryTexture
					float2 _UvOffsetSecondary;
					float _AlphaSecondary;
				#endif
			#endif

			#ifdef UseAdditionalTransformation
				float4 _AdditionalTransformation;
			#endif
			
			#ifdef SecondaryUseAdditionalTransformation
				float4 _SecondaryAdditionalTransformation;
			#endif
			
			#ifdef UnscaledTime
				// x: Time
				// y: Delta
				// z: sin Time
				// w: cos Time
				float4 TV_UnscaledTime;
			#endif

			float GetTime()
			{
				#ifdef UnscaledTime
					return TV_UnscaledTime.x;
				#else
					return _Time.y;
				#endif
			}

			v2f vert (appdata_t v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.worldPosition = v.vertex;
				o.vertex = UnityObjectToClipPos(o.worldPosition);
				float2 texcoord;

				#ifdef SpriteSheetAnimation
					float frame = floor((GetTime() * _FrameRate)) % (_NumSpritesX * _NumSpritesY);
					float frameX = frame % _NumSpritesX;
					float frameY = _NumSpritesY - floor(frame / _NumSpritesX) - 1;
					texcoord.x = (v.texcoord.x + frameX) / _NumSpritesX;
					texcoord.y = (v.texcoord.y + frameY) / _NumSpritesY;
					o.texcoord = TRANSFORM_TEX(texcoord, _MainTex);
				#elif defined(Scrolling)
					texcoord.x = v.texcoord.x + GetTime() * _ScrollingSpeedU;
					texcoord.y = v.texcoord.y + GetTime() * _ScrollingSpeedV;
					o.texcoord = TRANSFORM_TEX(texcoord, _MainTex);
				#else
					o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				#endif

				#ifdef UVRotation
					float sinX = sin(_RotationSpeed * GetTime());
					float cosX = cos(_RotationSpeed * GetTime());
					float sinY = sinX;
					float2x2 rotationMatrix = float2x2( cosX, -sinX, sinY, cosX);
					float2 tcRot = o.texcoord.xy - float2(.5,.5);
					o.texcoord.xy = mul(tcRot, rotationMatrix) + float2(.5,.5);
				#endif

				#ifdef SecondaryTexture
					#ifdef SecondaryTextureScrolling
						texcoord.x = v.texcoord.x + GetTime() * _SecondaryTextureScrollingSpeedU;
						texcoord.y = v.texcoord.y + GetTime() * _SecondaryTextureScrollingSpeedV;
						o.texcoordSecondary = TRANSFORM_TEX(texcoord, _SecondaryTex);
					#else
						o.texcoordSecondary = TRANSFORM_TEX(v.texcoord, _SecondaryTex);
					#endif
				#endif

				#ifdef CSharpHandledFeatures
					o.texcoord += _UvOffsetMain;
					#ifdef SecondaryTexture
						o.texcoordSecondary += _UvOffsetSecondary;
					#endif
				#endif

				#ifdef UseAdditionalTransformation
					// Main texture atlas transform
					o.texcoord *= _AdditionalTransformation.zw;
					o.texcoord += _AdditionalTransformation.xy;
				#endif
				
				#ifdef SecondaryTexture
					#ifdef SecondaryUseAdditionalTransformation
						// Own Atlas-Transformation for Secondary
						o.texcoordSecondary *= _SecondaryAdditionalTransformation.zw;
						o.texcoordSecondary += _SecondaryAdditionalTransformation.xy;
					#elif defined(UseAdditionalTransformation)
						// fallback: if secoondary not set, use primary,
						o.texcoordSecondary *= _AdditionalTransformation.zw;
						o.texcoordSecondary += _AdditionalTransformation.xy;
					#endif
				#endif

				#ifdef NoVertexColor
					o.color = _Color;
				#else
					o.color = v.color * _Color;
				#endif

				return o;
			}

			half4 frag (v2f i) : COLOR
			{
				half4 color = tex2D(_MainTex, i.texcoord) * i.color;
				#ifdef Adjustments
					color.rgb += _Brightness;
					color.rgb -= 0.5f;
					color.rgb *= _Contrast;
					color.rgb += 0.5f;
					color.rgb = saturate(color.rgb);
				#endif

				#ifdef SecondaryTexture
					half4 colorSecondary = tex2D(_SecondaryTex, i.texcoordSecondary) * _SecondaryColor;

					#ifdef SecondaryAdjustments
						colorSecondary.rgb += _SecondaryBrightness;
						colorSecondary.rgb -= 0.5f;
						colorSecondary.rgb *= _SecondaryContrast;
						colorSecondary.rgb += 0.5f;
						colorSecondary.rgb = saturate(colorSecondary.rgb);
					#endif

					#ifdef CSharpHandledFeatures
						float alphaSecondary = _AlphaSecondary;
					#else
						float alphaSecondary = 1;
					#endif

					#ifdef _SECONDARYTEXTUREBLENDMODE_TRANSPARENT
						{
							float a = colorSecondary.a * alphaSecondary;
							color.rgb = lerp(color.rgb, colorSecondary.rgb, a);
							#ifdef SecondaryTextureBlendModeSeparateAlpha
								color.a = saturate(color.a + a);
							#endif
						}
					#endif
					#ifdef _SECONDARYTEXTUREBLENDMODE_ADDITIVE
						color.rgb += colorSecondary.rgb * alphaSecondary;
					#endif
					#ifdef _SECONDARYTEXTUREBLENDMODE_MULTIPLICATIVE
						color.rgb *= lerp(fixed3(1,1,1), colorSecondary.rgb, alphaSecondary);
					#endif
				#endif

				#ifdef Disabled
					float3 luminance = (0.22 * color.r) + (0.72 * color.g) + (0.06 * color.b) ;
					color.rgb = lerp(color.rgb, luminance * _DisabledBrightness, _DisabledDesaturateStrength);
					color.a = color.a * _DisabledAlpha; 
				#endif

				// For additive and subtractive, we also need to take incoming alpha into account; necessary for canvas group etc
				#if defined(_TEXTUREBLENDMODE_ADDITIVE) || defined(_TEXTUREBLENDMODE_TRANSPARENTPREMULTIPLIED)
					color.rgb *= i.color.a * color.a;
				#elif defined(_TEXTUREBLENDMODE_MULTIPLICATIVE)
					color.rgb = lerp(fixed3(1,1,1), color.rgb, i.color.a);
				#endif

				#ifdef UNITY_UI_CLIP_RECT
					#if defined(_TEXTUREBLENDMODE_TRANSPARENT)
						color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
					#elif defined(_TEXTUREBLENDMODE_TRANSPARENTPREMULTIPLIED)
						color *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
					#elif defined(_TEXTUREBLENDMODE_ADDITIVE)
						color.rgb *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
					#elif defined(_TEXTUREBLENDMODE_MULTIPLICATIVE)
						color.rgb = lerp(fixed3(1,1,1), color.rgb, UnityGet2DClipping(i.worldPosition.xy, _ClipRect));
					#endif
				#endif

				#ifdef UNITY_UI_ALPHACLIP
					clip (color.a - 0.001);
				#endif

				#ifdef CSharpHandledFeatures
					#if defined(_TEXTUREBLENDMODE_TRANSPARENT)
						color.a *= _AlphaMain;
					#elif defined(_TEXTUREBLENDMODE_TRANSPARENTPREMULTIPLIED)
						color *= _AlphaMain;
					#elif defined(_TEXTUREBLENDMODE_ADDITIVE)
						color.rgb *= _AlphaMain;
					#elif defined(_TEXTUREBLENDMODE_MULTIPLICATIVE)
						color.rgb *= lerp(fixed3(1,1,1), color.rgb, _AlphaMain);
					#endif
				#endif

				return color;
			}
			ENDCG

		}
	}

	CustomEditor "GuiToolkit.Editor.UiMultiPurposeShaderEditor"
	FallBack "UI/Default"
}
