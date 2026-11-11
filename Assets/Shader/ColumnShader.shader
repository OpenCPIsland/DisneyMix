Shader "Custom/Drop/ColumnShader" {
	// Reconstructed: the original source was lost (only a stub with the property list survived).
	// The property names and labels ("Base (RGB) Trans (A)", "Main Color") are the ones Unity's built-in
	// Transparent/Diffuse shader uses, so this is a lit, alpha-blended diffuse shader:
	// colour = _MainTex.rgb * _Color.rgb, alpha = _MainTex.a * _Color.a.
	Properties {
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" { }
		_Color ("Main Color", Color) = (1.000000,1.000000,1.000000,1.000000)
	}

	SubShader {
		Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		LOD 200

		CGPROGRAM
		#pragma surface surf Lambert alpha:fade
		#pragma target 3.0

		sampler2D _MainTex;
		fixed4 _Color;

		struct Input
		{
			float2 uv_MainTex;
		};

		void surf(Input IN, inout SurfaceOutput o)
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Transparent/VertexLit"
}
