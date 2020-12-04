sampler2D _MainTex;

struct Input
{
    float2 uv_MainTex;
};

half _Glossiness;
half _Metallic;
fixed4 _Color;
fixed4 _ColorSelfIllumination;

// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
// #pragma instancing_options assumeuniformscaling
UNITY_INSTANCING_BUFFER_START(Props)
	#ifdef USE_UI3D
		float4 _Offset;
		float4 _Scale;
	#endif
UNITY_INSTANCING_BUFFER_END(Props)

void vert (inout appdata_full v) {
	#ifdef USE_UI3D
		v.vertex *= UNITY_ACCESS_INSTANCED_PROP(Props, _Scale);
		v.vertex += UNITY_ACCESS_INSTANCED_PROP(Props, _Offset);
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
