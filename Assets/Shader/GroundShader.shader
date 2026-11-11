Shader "Custom/Drop/GroundShader" {
	// Reconstructed: the original source was lost (only a stub with the property list survived).
	// Behavior is inferred from the property names:
	//   _MaskTex white = snow, black = bare ground (_MainTex). The default black mask shows _MainTex only,
	//   which matches the old stub's output.
	//   _RippleMaskTex roughens the snow/ground edge so the transition ripples instead of fading evenly.
	//   _Min / _Max are the mask thresholds (below Min = ground, above Max = snow).
	//   _Mid is the neutral value of the ripple texture (ripple pixels brighter than Mid push the edge
	//   toward snow, darker ones toward ground).
	Properties {
		_MainTex ("Main", 2D) = "white" { }
		_SnowTex ("Snow", 2D) = "white" { }
		_RippleMaskTex ("Snow Ripple", 2D) = "white" { }
		_MaskTex ("Mask", 2D) = "black" { }
		_Min ("Min", Range(0.000000,1.000000)) = 0.000000
		_Mid ("Mid", Range(0.000000,1.000000)) = 0.500000
		_Max ("Max", Range(0.000000,1.000000)) = 1.000000
	}

	SubShader {
		Tags { "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
		#pragma surface surf Lambert
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _SnowTex;
		sampler2D _RippleMaskTex;
		sampler2D _MaskTex;
		float _Min;
		float _Mid;
		float _Max;

		struct Input
		{
			float2 uv_MainTex;
			float2 uv_SnowTex;
			float2 uv_RippleMaskTex;
			float2 uv_MaskTex;
		};

		void surf(Input IN, inout SurfaceOutput o)
		{
			float4 ground = tex2D(_MainTex, IN.uv_MainTex);
			float4 snow = tex2D(_SnowTex, IN.uv_SnowTex);
			float mask = tex2D(_MaskTex, IN.uv_MaskTex).r;
			float ripple = tex2D(_RippleMaskTex, IN.uv_RippleMaskTex).r;

			// Apply the ripple only near the transition (full strength at mask = 0.5, none at 0 or 1)
			// so fully bare or fully snowy areas stay clean.
			float edge = 1.0 - abs(mask * 2.0 - 1.0);
			mask = saturate(mask + (ripple - _Mid) * edge);

			float snowAmount = smoothstep(_Min, max(_Max, _Min + 0.0001), mask);
			o.Albedo = lerp(ground.rgb, snow.rgb, snowAmount);
			o.Emission = o.Albedo;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
