Shader "Custom/gradientlight" {
	// Reconstructed: the original source was lost (only a stub with the property list survived).
	// Inferred behavior: a texture lit by a fixed, shader-defined light direction through a ramp
	// texture (toon/gradient shading), plus a rim highlight. It does not use scene lights, so it
	// looks the same whether or not the scene has a light.
	//   _LightDir  direction TOWARD the light, in world space (flip the sign if the shading looks inverted)
	//   _RampTex   left edge = shadow colour, right edge = lit colour
	//   _RimColor / _RimPower  edge glow colour and falloff (higher power = thinner rim)
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" { }
		_RampTex ("Lighting Ramp", 2D) = "white" { }
		_RimColor ("Rim Color", Color) = (1.000000,1.000000,1.000000,1.000000)
		_RimPower ("Rim Power", Float) = 2.000000
		_LightDir ("Lighting Direction", Vector) = (0.000000,0.000000,-1.000000,0.000000)
	}

	SubShader {
		Tags { "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
		#pragma surface surf Unlit noforwardadd
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _RampTex;
		fixed4 _RimColor;
		float _RimPower;
		float4 _LightDir;

		struct Input
		{
			float2 uv_MainTex;
			float3 viewDir;
			float3 worldNormal;
		};

		// All shading is done in surf() and written to Emission, so scene lights add nothing.
		half4 LightingUnlit(SurfaceOutput s, half3 lightDir, half atten)
		{
			return half4(0, 0, 0, s.Alpha);
		}

		void surf(Input IN, inout SurfaceOutput o)
		{
			fixed4 baseCol = tex2D(_MainTex, IN.uv_MainTex);

			float3 n = normalize(IN.worldNormal);
			float3 l = normalize(_LightDir.xyz);
			float ndl = dot(n, l) * 0.5 + 0.5; // wrap to 0..1 so the full ramp is used
			fixed3 ramp = tex2D(_RampTex, float2(saturate(ndl), 0.5)).rgb;

			float rim = pow(1.0 - saturate(dot(normalize(IN.viewDir), n)), _RimPower);

			o.Albedo = 0;
			o.Emission = baseCol.rgb * ramp + _RimColor.rgb * rim;
			o.Alpha = baseCol.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
