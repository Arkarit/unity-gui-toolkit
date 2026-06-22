Shader "UIToolkit/UI_Glitter"
{
	Properties
	{
		_MainTex ("Mask Texture (alpha)", 2D) = "white" {}

		[KeywordEnum(Off, Rainbow, TwoColor, ThreeColor, DualRender)] _ColorMode("Color Mode", Float) = 0
		_Color ("Tint / Color 1", color) = (1,1,1,1)
		_Color2 ("Color 2", color) = (1,1,1,1)
		_Color3 ("Color 3", color) = (1,1,1,1)
		_ColorVariation ("Rainbow Variation Amount", Range(0,1)) = 0.5
		_RainbowFloor ("Rainbow Brightness Floor", Range(0,1)) = 0.4
		_DualScale ("Dual Render Inner Scale", Range(0.05, 1.0)) = 0.5

		_Density ("Density (cells per UV)", Float) = 8
		_Coverage ("Coverage (0..1)", Range(0,1)) = 0.5
		_Speed ("Twinkle Speed", Float) = 1
		_ScrollSpeedX ("Scroll Speed X (UV/s)", Float) = 0
		_ScrollSpeedY ("Scroll Speed Y (UV/s)", Float) = 0
		_SizeMin ("Sparkle Size Min", Range(0.05, 2.0)) = 0.3
		_SizeMax ("Sparkle Size Max", Range(0.05, 2.0)) = 0.6
		_SpikeSharpness ("Spike Sharpness", Range(1, 32)) = 8
		_Brightness ("Brightness", Float) = 1

		_FoldoutStencil ("Stencil", Float) = 0
		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255
		_ColorMask ("Color Mask", Float) = 15
		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

		_FoldoutBlendMode ("Blend Mode, Culling, ZTest, ZWrite, Render Queue", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("BlendSource", Float) = 5
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("BlendDestination", Float) = 10
		[Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4
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
			"PreviewType" = "Plane"
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
			#pragma shader_feature_local __ _COLORMODE_OFF _COLORMODE_RAINBOW _COLORMODE_TWOCOLOR _COLORMODE_THREECOLOR _COLORMODE_DUALRENDER

			struct appdata_t
			{
				float4 vertex   : POSITION;
				float2 texcoord : TEXCOORD0;
				half4  color    : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex        : SV_POSITION;
				float2 texcoord      : TEXCOORD0;
				float4 worldPosition : TEXCOORD1;
				half4  color         : COLOR;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _MainTex;
			float4    _MainTex_ST;
			half4     _Color;
			float     _Density;
			float     _Coverage;
			float     _Speed;
			float     _ScrollSpeedX;
			float     _ScrollSpeedY;
			float     _SizeMin;
			float     _SizeMax;
			float     _SpikeSharpness;
			float     _Brightness;

			#if defined(_COLORMODE_RAINBOW)
				float _ColorVariation;
				float _RainbowFloor;
			#endif
			#if defined(_COLORMODE_TWOCOLOR) || defined(_COLORMODE_THREECOLOR) || defined(_COLORMODE_DUALRENDER)
				half4 _Color2;
			#endif
			#if defined(_COLORMODE_THREECOLOR)
				half4 _Color3;
			#endif
			#if defined(_COLORMODE_DUALRENDER)
				float _DualScale;
			#endif

			float4 _ClipRect;

			// Pseudo-random hash, returns 0..1 from a 2D seed.
			float hash21(float2 p)
			{
				p = frac(p * float2(123.34, 456.21));
				p += dot(p, p + 45.32);
				return frac(p.x * p.y);
			}

			// 4-pointed star: horizontal spike + vertical spike + center bulb.
			// p is the cell-local sample position, scaled so |p|<=1 is visible.
			float starShape(float2 p, float spikeSharp)
			{
				float2 ap = abs(p);
				float horiz = pow(max(0.0, 1.0 - ap.x), 2.0) * pow(max(0.0, 1.0 - ap.y * spikeSharp), 2.0);
				float vert  = pow(max(0.0, 1.0 - ap.y), 2.0) * pow(max(0.0, 1.0 - ap.x * spikeSharp), 2.0);
				float bulb  = pow(max(0.0, 1.0 - length(p)), 4.0);
				return horiz + vert + bulb;
			}

			v2f vert(appdata_t v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.worldPosition = v.vertex;
				o.vertex = UnityObjectToClipPos(o.worldPosition);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.color = v.color;
				return o;
			}

			half4 frag(v2f i) : COLOR
			{
				half maskA = tex2D(_MainTex, i.texcoord).a;

				// Scroll the cell grid; the mask sample below stays on the unscrolled UV.
				float2 scrollOffset = float2(_ScrollSpeedX, _ScrollSpeedY) * _Time.y;
				float2 gridUV  = (i.texcoord + scrollOffset) * _Density;
				float2 cellBase = floor(gridUV);
				float2 localUV  = frac(gridUV);

				half3 colorAcc = half3(0,0,0);
				float intensityAcc = 0.0;

				// 3x3 neighborhood so spikes can cross cell borders cleanly.
				[unroll]
				for (int dy = -1; dy <= 1; dy++)
				{
					[unroll]
					for (int dx = -1; dx <= 1; dx++)
					{
						float2 cellId = cellBase + float2(dx, dy);
						float h1 = hash21(cellId);
						float h2 = hash21(cellId + float2(17.3,  9.7));
						float h3 = hash21(cellId + float2(31.7, 53.1));
						float h4 = hash21(cellId + float2(91.7,  7.3));
						float h5 = hash21(cellId + float2(73.1, 41.9));

						// Branchless cell activation.
						float active = step(h1, _Coverage);

						// Per-cell randomized size.
						float size = lerp(_SizeMin, _SizeMax, h4);
						float invSize = 1.0 / max(size, 0.01);

						// Jitter inside the cell so sparkles aren't on a regular grid.
						float2 cellOffset = float2(dx, dy) + 0.2 + float2(h2, h3) * 0.6;
						float2 p = (localUV - cellOffset) * invSize;

						// Phase + variable period per cell.
						float lifeT = frac(_Time.y * _Speed * (0.5 + h2) + h1);
						// Sine pulse, shifted so each cycle has a dark gap.
						float pulse = sin(lifeT * UNITY_PI);
						float fade  = saturate(pulse * 1.5 - 0.5);

						float shape = starShape(p, _SpikeSharpness);
						float contrib = saturate(shape) * fade * active;

						half3 tinted;
						#if defined(_COLORMODE_RAINBOW)
							float angle = h5 * 6.2831853;
							half3 rawHue = half3(
								0.5 + 0.5 * sin(angle),
								0.5 + 0.5 * sin(angle + 2.0944),
								0.5 + 0.5 * sin(angle + 4.1888)
							);
							// Lift dark channels so no hue drops below _RainbowFloor.
							half3 rainbow = _RainbowFloor + (1.0 - _RainbowFloor) * rawHue;
							tinted = lerp(_Color.rgb, rainbow, _ColorVariation);
						#elif defined(_COLORMODE_TWOCOLOR)
							tinted = lerp(_Color.rgb, _Color2.rgb, step(0.5, h5));
						#elif defined(_COLORMODE_THREECOLOR)
							float t3 = h5 * 3.0;
							if (t3 < 1.0)
							{
								tinted = _Color.rgb;
							}
							else if (t3 < 2.0)
							{
								tinted = _Color2.rgb;
							}
							else
							{
								tinted = _Color3.rgb;
							}
						#elif defined(_COLORMODE_DUALRENDER)
							// Inner star uses the same shape at a smaller absolute radius.
							// Its falloff naturally produces the two-color gradient inside the outer star.
							float2 pInner = p / max(_DualScale, 0.01);
							float innerShape = starShape(pInner, _SpikeSharpness);
							tinted = lerp(_Color.rgb, _Color2.rgb, saturate(innerShape));
						#else
							tinted = _Color.rgb;
						#endif
						colorAcc += tinted * contrib;

						intensityAcc += contrib;
					}
				}

				half4 color;
				color.rgb = colorAcc * _Brightness;
				color.a   = saturate(intensityAcc * _Brightness) * _Color.a;

				color *= i.color;
				color.a *= maskA;

				#ifdef UNITY_UI_CLIP_RECT
					color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
				#endif

				#ifdef UNITY_UI_ALPHACLIP
					clip(color.a - 0.001);
				#endif

				return color;
			}
		ENDCG

		}
	}

	CustomEditor "GuiToolkit.Editor.UiGlitterShaderEditor"
	FallBack "UI/Default"
}
