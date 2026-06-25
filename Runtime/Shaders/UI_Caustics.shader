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

		_HighlightColor ("Highlight Color", color) = (0.7, 0.95, 1.0, 0.7)

		_Scale ("Scale (cells per UV)", Float) = 4
		_Speed ("Speed", Float) = 0.4

		[Toggle(UseSecondOctave)] _UseSecondOctave ("Use Second Octave", Float) = 1
		_Scale2 ("Scale 2 (cells per UV)", Float) = 7
		_Speed2 ("Speed 2", Float) = -0.3
		[KeywordEnum(Multiply,Screen,Add,Max)] _Combine ("Combine Mode", Float) = 1

		_Threshold ("Line Core Thickness", Range(0, 0.3)) = 0.0
		_EdgeWidth ("Line Halo Width", Range(0.005, 0.3)) = 0.09
		_Contrast ("Contrast", Range(0.5, 4)) = 1.8
		_Intensity ("Intensity", Range(0, 3)) = 1.4

		[Toggle(UseEdgeVariation)] _UseEdgeVariation ("Use Edge Variation", Float) = 1
		_EdgeVariation ("Edge Variation Amount", Range(0, 1)) = 0.7

		[Toggle(UseDistortion)] _UseDistortion ("Use Distortion", Float) = 1
		_Distortion ("Distortion Amount", Range(0, 0.3)) = 0.04
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
			#pragma shader_feature_local __ UseSecondOctave
			#pragma shader_feature_local __ UseDistortion
			#pragma shader_feature_local __ UseGradient
			#pragma shader_feature_local __ UseEdgeVariation
			#pragma shader_feature_local __ _COMBINE_MULTIPLY _COMBINE_SCREEN _COMBINE_ADD _COMBINE_MAX

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

			#ifdef UseGradient
				half4 _BaseColor2;
				float _GradientAngle;
				float _GradientPower;
			#endif

			#ifdef UseEdgeVariation
				float _EdgeVariation;
			#endif

			float _Scale;
			float _Speed;
			float _Threshold;
			float _EdgeWidth;
			float _Contrast;
			float _Intensity;

			#ifdef UseSecondOctave
				float _Scale2;
				float _Speed2;
			#endif

			#ifdef UseDistortion
				float _Distortion;
				float _DistortionFreq;
				float _DistortionSpeed;
			#endif

			float4 _ClipRect;

			// Same sine-based hash family used elsewhere in the toolkit (see UI_Glitter).
			// Decorrelates well along both axes; cheap on mobile.
			float2 hash22(float2 p)
			{
				p = float2(dot(p, float2(127.1, 311.7)),
				           dot(p, float2(269.5, 183.3)));
				return frac(sin(p) * 43758.5453);
			}

			// Worley/Voronoi F1+F2: distance to the nearest and second-nearest jittered
			// cell point in a 3x3 neighborhood. Returns x=sqrt(F1²), y=sqrt(F2²).
			// When UseEdgeVariation is on, z carries a per-edge variation factor — a hash
			// of (cellId1 + cellId2), which is commutative so both sides of any cell-cell
			// edge see the SAME value (no abrupt step at the line peak).
			float3 voronoiF1F2(float2 uv)
			{
				float2 i_uv = floor(uv);
				float2 f_uv = frac(uv);
				float  f1 = 1.0;
				float  f2 = 1.0;
				#ifdef UseEdgeVariation
					float2 id1 = float2(0, 0);
					float2 id2 = float2(0, 0);
				#endif

				[unroll]
				for (int dy = -1; dy <= 1; dy++)
				{
					[unroll]
					for (int dx = -1; dx <= 1; dx++)
					{
						float2 neighbor  = float2(dx, dy);
						float2 cellId    = i_uv + neighbor;
						float2 cellPoint = hash22(cellId);
						float2 diff      = neighbor + cellPoint - f_uv;
						float  dSq       = dot(diff, diff);

						#ifdef UseEdgeVariation
							// Branchless top-2 with ID tracking.
							bool   beatF1   = dSq < f1;
							bool   beatF2   = dSq < f2;
							float  prevF1   = f1;
							float2 prevId1  = id1;
							f1 = beatF1 ? dSq : f1;
							id1 = beatF1 ? cellId : id1;
							float  candF2  = beatF1 ? prevF1 : dSq;
							float2 candId2 = beatF1 ? prevId1 : cellId;
							f2  = beatF2 ? candF2  : f2;
							id2 = beatF2 ? candId2 : id2;
						#else
							float newF1 = min(f1, dSq);
							float newF2 = min(f2, max(f1, dSq));
							f1 = newF1;
							f2 = newF2;
						#endif
					}
				}

				#ifdef UseEdgeVariation
					float edgeVar = hash22(id1 + id2).x;
				#else
					float edgeVar = 1.0;
				#endif
				return float3(sqrt(f1), sqrt(f2), edgeVar);
			}

			// F2-F1 is the perpendicular-bisector distance — exactly 0 along the boundary
			// between two cells, growing toward cell interiors. Combined with a Gaussian-
			// style falloff this gives a peaked-not-plateaued bell shape: every pixel on
			// the line has its own intensity, so the lines themselves have soft inner
			// gradients (no "flat top"). _Threshold sets a small flat core (set 0 for
			// pure gaussian); _EdgeWidth is the σ of the halo falloff.
			float causticShape(float2 f1f2)
			{
				float d = f1f2.y - f1f2.x;
				float beyondCore = max(0.0, d - _Threshold);
				float sigma = max(_EdgeWidth, 0.0001);
				return exp(-(beyondCore * beyondCore) / (sigma * sigma));
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

				float t = _Time.y;
				float2 uv = i.texcoord;

				// Optional sinusoidal UV swim — gives the wavering refraction feel.
				// Two perpendicular sines so the field warps in both directions.
				#ifdef UseDistortion
					float dt = t * _DistortionSpeed;
					float2 swim = float2(
						sin(uv.y * _DistortionFreq * 6.2831853 + dt * 1.3),
						sin(uv.x * _DistortionFreq * 6.2831853 + dt)
					) * _Distortion;
					uv += swim;
				#endif

				// First octave: drifts diagonally with _Speed.
				float2 uv1 = uv * _Scale + float2(t * _Speed * 0.5, t * _Speed * 0.25);
				float3 v1  = voronoiF1F2(uv1);
				float  c1  = causticShape(v1.xy);
				#ifdef UseEdgeVariation
					c1 *= lerp(1.0 - _EdgeVariation, 1.0, v1.z);
				#endif

				// Second octave: typically opposite direction + higher frequency.
				// Combined via the selected blend keyword to get interference patterns.
				#ifdef UseSecondOctave
					float2 uv2 = uv * _Scale2 + float2(-t * _Speed2 * 0.4, t * _Speed2 * 0.3);
					float3 v2  = voronoiF1F2(uv2);
					float  c2  = causticShape(v2.xy);
					#ifdef UseEdgeVariation
						c2 *= lerp(1.0 - _EdgeVariation, 1.0, v2.z);
					#endif

					float caustic;
					#if defined(_COMBINE_MULTIPLY)
						caustic = c1 * c2;
					#elif defined(_COMBINE_ADD)
						caustic = saturate(c1 + c2);
					#elif defined(_COMBINE_MAX)
						caustic = max(c1, c2);
					#else
						// _COMBINE_SCREEN (default, also the fallback when no keyword set)
						caustic = 1.0 - (1.0 - c1) * (1.0 - c2);
					#endif
				#else
					float caustic = c1;
				#endif

				caustic = pow(saturate(caustic), _Contrast);
				caustic = saturate(caustic * _Intensity);

				// Optional linear base-color gradient — simulates water depth (shallow at the
				// start of the gradient axis, deep at the end). Angle 0 = top→bottom; gradient
				// runs in the same UV space as the caustics so material UV tiling affects it too.
				#ifdef UseGradient
					float angleRad = _GradientAngle * UNITY_PI / 180.0;
					float2 gradDir = float2(sin(angleRad), -cos(angleRad));
					float  gradT   = saturate(dot(i.texcoord - 0.5, gradDir) + 0.5);
					gradT          = pow(gradT, _GradientPower);
					half4  baseCol = lerp(_BaseColor, _BaseColor2, gradT);
				#else
					half4 baseCol = _BaseColor;
				#endif

				// Blend base→highlight per-channel and per-alpha so the user can choose
				// uniform dimming (matching alphas) or "punch-through" (highlight alpha higher).
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
