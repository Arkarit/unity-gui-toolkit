Shader "UIToolkit/UI_Sunburst"
{
	Properties
	{
		_MainTex ("Texture 1 (RGB,A)", 2D) = "white" {}
		_Rotation ("Rotation 1 (deg)", Float) = 0

		[Toggle(UseTex2)] _UseTex2("Use Texture 2", Float) = 0
		_Tex2 ("Texture 2 (RGB,A)", 2D) = "white" {}
		_Rotation2 ("Rotation 2 (deg)", Float) = 0
		[KeywordEnum(Multiply,Add,Screen,Min,Max)] _Combine ("Combine Mode", Float) = 0

		_Color ("Tint", color) = (1,1,1,1)
		[Toggle(PerLayerTint)] _UsePerLayerTint("Per-Layer Tint", Float) = 0
		_Color2 ("Tint 2", color) = (1,1,1,1)

		[Toggle(AnimRotation)] _UseAnimRotation("Animate Rotation", Float) = 0
		_RotationSpeed ("Rotation Speed 1 (deg/s)", Float) = 30
		_Rotation2Speed ("Rotation Speed 2 (deg/s)", Float) = -30

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
			#pragma shader_feature_local __ UseTex2
			#pragma shader_feature_local __ PerLayerTint
			#pragma shader_feature_local __ AnimRotation
			#pragma shader_feature_local __ _COMBINE_MULTIPLY _COMBINE_ADD _COMBINE_SCREEN _COMBINE_MIN _COMBINE_MAX

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
				#ifdef UseTex2
					float2 texcoord2 : TEXCOORD2;
				#endif
				half4  color         : COLOR;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _MainTex;
			float4    _MainTex_ST;
			half4     _Color;
			float     _Rotation;

			#ifdef UseTex2
				sampler2D _Tex2;
				float4    _Tex2_ST;
				half4     _Color2;
				float     _Rotation2;
			#endif

			#ifdef AnimRotation
				float _RotationSpeed;
				#ifdef UseTex2
					float _Rotation2Speed;
				#endif
			#endif

			float4 _ClipRect;

			// Rotates uv around (0.5, 0.5) by angleRad radians.
			float2 RotateUV(float2 uv, float angleRad)
			{
				float s = sin(angleRad);
				float c = cos(angleRad);
				uv -= 0.5;
				uv = float2(c * uv.x - s * uv.y, s * uv.x + c * uv.y);
				return uv + 0.5;
			}

			v2f vert(appdata_t v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.worldPosition = v.vertex;
				o.vertex = UnityObjectToClipPos(o.worldPosition);

				float angle1 = _Rotation * UNITY_PI / 180.0;
				#ifdef AnimRotation
					angle1 += _RotationSpeed * _Time.y * UNITY_PI / 180.0;
				#endif
				float2 uv1 = RotateUV(v.texcoord, angle1);
				o.texcoord = TRANSFORM_TEX(uv1, _MainTex);

				#ifdef UseTex2
					float angle2 = _Rotation2 * UNITY_PI / 180.0;
					#ifdef AnimRotation
						angle2 += _Rotation2Speed * _Time.y * UNITY_PI / 180.0;
					#endif
					float2 uv2 = RotateUV(v.texcoord, angle2);
					o.texcoord2 = TRANSFORM_TEX(uv2, _Tex2);
				#endif

				o.color = v.color;
				return o;
			}

			half4 frag(v2f i) : COLOR
			{
				half4 c1 = tex2D(_MainTex, i.texcoord);
				#ifdef PerLayerTint
					c1 *= _Color;
				#endif

				half4 color;

				#ifdef UseTex2
					half4 c2 = tex2D(_Tex2, i.texcoord2);
					#ifdef PerLayerTint
						c2 *= _Color2;
					#endif

					#if defined(_COMBINE_ADD)
						color = saturate(c1 + c2);
					#elif defined(_COMBINE_SCREEN)
						color = half4(1.0, 1.0, 1.0, 1.0) - (half4(1.0, 1.0, 1.0, 1.0) - c1) * (half4(1.0, 1.0, 1.0, 1.0) - c2);
					#elif defined(_COMBINE_MIN)
						color = min(c1, c2);
					#elif defined(_COMBINE_MAX)
						color = max(c1, c2);
					#else
						// _COMBINE_MULTIPLY (also default when no keyword set)
						color = c1 * c2;
					#endif
				#else
					color = c1;
				#endif

				#ifndef PerLayerTint
					color *= _Color;
				#endif

				color *= i.color;

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

	CustomEditor "GuiToolkit.Editor.UiSunburstShaderEditor"
	FallBack "UI/Default"
}
