Shader "UIToolkit/UI_Caustics"
{
	Properties
	{
		_MainTex ("Mask Texture (alpha)", 2D) = "white" {}

		_BaseColor ("Base Color", color) = (0, 0.04, 0.12, 0.7)
		[Toggle(UseGradient)] _UseGradient ("Use Base Gradient", Float) = 1
		_BaseColor2 ("Base Color 2 (deep)", color) = (0, 0.005, 0.04, 0.88)
		_GradientAngle ("Gradient Angle (deg)", Float) = 0
		_GradientPower ("Gradient Power", Range(0.1, 10)) = 1.4

		_HighlightColor ("Highlight Color", color) = (0.85, 0.97, 1.0, 0.7)

		_Scale ("Scale", Float) = 1
		_Speed ("Animation Speed", Float) = 0.5
		_Iterations ("Iterations", Range(1, 7)) = 5

		_Density ("Line Density", Range(0.0005, 0.05)) = 0.005
		_Brightness ("Brightness Offset", Range(0.5, 2)) = 1.17
		_Sharpness ("Sharpness Curve", Range(0.5, 4)) = 1.4
		_Contrast ("Contrast (final exponent)", Range(1, 30)) = 8
		_Intensity ("Intensity", Range(0, 3)) = 1.0

		[Toggle(UseTileBreak)] _UseTileBreak ("Use Tile Break", Float) = 1
		_TileBreak ("Tile Break Amount", Range(0, 20)) = 6
		_TileBreakFreq ("Tile Break Frequency", Range(0.1, 5)) = 1

		[Toggle(UseDistortion)] _UseDistortion ("Use Distortion", Float) = 0
		_Distortion ("Distortion Amount", Range(0, 0.3)) = 0.02
		_DistortionFreq ("Distortion Frequency", Float) = 3
		_DistortionSpeed ("Distortion Speed", Float) = 1

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
			#pragma shader_feature_local __ UseGradient
			#pragma shader_feature_local __ UseDistortion
			#pragma shader_feature_local __ UseTileBreak

			#define TAU 6.28318530718

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

			half4 _BaseColor;
			half4 _HighlightColor;
			float _Scale;
			float _Speed;
			float _Iterations;
			float _Density;
			float _Brightness;
			float _Sharpness;
			float _Contrast;
			float _Intensity;

			#ifdef UseGradient
				half4 _BaseColor2;
				float _GradientAngle;
				float _GradientPower;
			#endif

			#ifdef UseDistortion
				float _Distortion;
				float _DistortionFreq;
				float _DistortionSpeed;
			#endif

			#ifdef UseTileBreak
				float _TileBreak;
				float _TileBreakFreq;
			#endif

			float4 _ClipRect;

			#ifdef UseTileBreak
				// Scalar hash + bilinear value noise. Non-periodic (hash22-based) so it
				// breaks the intrinsic sin/cos periodicity of the iterative caustic.
				float hash21(float2 p)
				{
					return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
				}

				float valueNoise(float2 uv)
				{
					float2 ip = floor(uv);
					float2 fp = frac(uv);
					fp = fp * fp * (3.0 - 2.0 * fp);  // smoothstep curve
					return lerp(
						lerp(hash21(ip),                hash21(ip + float2(1, 0)), fp.x),
						lerp(hash21(ip + float2(0, 1)), hash21(ip + float2(1, 1)), fp.x),
						fp.y);
				}
			#endif

			// Drift-style iterative caustic — descendant of the famous ShaderToy
			// procedural caustic (vintage ~2014). The cos/sin self-feedback in the
			// loop produces organic dendrite-like patterns; 1/length(...) creates
			// the bright convergence lines where the iterated field collapses.
			// The hard-coded -250.0 shift puts p in a region where the trig
			// interplay yields the most visually interesting caustic behaviour
			// — it's not arbitrary, it's tuned for the look.
			float driftCaustic(float2 uv, float t)
			{
				// Original ShaderToy uses fmod here, but the wraparound creates visible
				// tiling at _Scale > 1. Trig precision is fine without it for the magnitudes
				// we deal with in UI (|p| stays well below 10⁴ even at large _Scale).
				float2 p = uv * TAU - 250.0;

				// Even without fmod, sin/cos has intrinsic 2π period, so the pattern
				// still tiles at _Scale > 1. A non-periodic value-noise warp on p breaks
				// that periodicity — adjacent "tiles" see different starting positions
				// and produce uncorrelated caustic patterns. Roughly 2π in p-space ≈
				// one full caustic period, so _TileBreak ~6 gives strong decorrelation.
				#ifdef UseTileBreak
					float2 noiseWarp = float2(
						valueNoise(uv * _TileBreakFreq + 1.7),
						valueNoise(uv * _TileBreakFreq + 9.1)
					) - 0.5;
					p += noiseWarp * _TileBreak;
				#endif
				float2 i = p;
				float  c = 1.0;
				float  inten = max(_Density, 0.0001);
				int    iters = clamp((int)_Iterations, 1, 7);

				[loop]
				for (int n = 0; n < 7; n++)
				{
					if (n >= iters)
						break;
					float t_ = t * (1.0 - (3.5 / (float(n) + 1.0)));
					i = p + float2(
						cos(t_ - i.x) + sin(t_ + i.y),
						sin(t_ - i.y) + cos(t_ + i.x)
					);
					c += 1.0 / length(float2(
						p.x / (sin(i.x + t_) / inten),
						p.y / (cos(i.y + t_) / inten)
					));
				}
				c /= (float)iters;
				c = _Brightness - pow(abs(c), _Sharpness);
				return pow(abs(c), _Contrast);
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

				float  t  = _Time.y * _Speed;
				float2 uv = i.texcoord * _Scale;

				#ifdef UseDistortion
					float dt = _Time.y * _DistortionSpeed;
					float2 swim = float2(
						sin(uv.y * _DistortionFreq + dt * 1.3),
						sin(uv.x * _DistortionFreq + dt)
					) * _Distortion;
					uv += swim;
				#endif

				float caustic = saturate(driftCaustic(uv, t) * _Intensity);

				#ifdef UseGradient
					float angleRad = _GradientAngle * UNITY_PI / 180.0;
					float2 gradDir = float2(sin(angleRad), -cos(angleRad));
					float  gradT   = saturate(dot(i.texcoord - 0.5, gradDir) + 0.5);
					gradT          = pow(gradT, _GradientPower);
					half4  baseCol = lerp(_BaseColor, _BaseColor2, gradT);
				#else
					half4 baseCol = _BaseColor;
				#endif

				half4 color;
				color.rgb = lerp(baseCol.rgb, _HighlightColor.rgb, caustic);
				color.a   = lerp(baseCol.a,   _HighlightColor.a,   caustic);

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

	CustomEditor "GuiToolkit.Editor.UiCausticsShaderEditor"
	FallBack "UI/Default"
}
