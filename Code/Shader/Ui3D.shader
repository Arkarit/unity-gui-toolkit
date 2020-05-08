Shader "UIToolkit/Ui3D"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _ColorSelfIllumination ("Color Self illumination", Color) = (0,0,0,0)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
		[Toggle(FlipNormals)] _FlipNormals("Flip Normals", Float) = 0
 		[Toggle(USE_UI3D)] _UseUi3D ("Use UI3D", Float) = 0
		_Offset ("Offset", Vector) = (0,0,0,0)
		_Scale ("Scale", Vector) = (1,1,1,1)
   }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard vertex:vert alpha:blend

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

		#pragma multi_compile __ FlipNormals
		#pragma multi_compile __ USE_UI3D

        sampler2D _MainTex;

		struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
		fixed4 _ColorSelfIllumination;
		#ifdef USE_UI3D
			float4 _Offset;
			float4 _Scale;
		#endif

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

		void vert (inout appdata_full v) {
			#ifdef USE_UI3D
				v.vertex *= _Scale;
				v.vertex += _Offset;
			#endif
			#ifdef FlipNormals
				v.normal = -v.normal;
			#endif
		}
		void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
			o.Emission = _ColorSelfIllumination.rgb * _ColorSelfIllumination.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
