Shader "Custom/UnlitCubemap" {
Properties {
 _MainTex ("Texture", 2D) = "white" { }
 _Cube ("Cubemap", CUBE) = "" { }
 _Intensity ("Intensity", Range(0.000000,1.000000)) = 1.000000
}
SubShader {
 Tags { "RenderType"="Opaque" }
 Pass {
  CGPROGRAM
  #pragma vertex vert
  #pragma fragment frag
  #include "UnityCG.cginc"
  sampler2D _MainTex;
  float4 _MainTex_ST;
  samplerCUBE _Cube;
  float _Intensity;
  struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
  struct v2f { float4 vertex : SV_POSITION; float3 direction : TEXCOORD0; float2 uv : TEXCOORD1; };
  v2f vert(appdata v) {
   v2f o;
   float3 worldPosition = mul(unity_ObjectToWorld, v.vertex).xyz;
   float3 worldNormal = UnityObjectToWorldNormal(v.normal);
   o.vertex = UnityObjectToClipPos(v.vertex);
   o.direction = reflect(worldPosition - _WorldSpaceCameraPos, worldNormal);
   o.uv = TRANSFORM_TEX(v.uv, _MainTex);
   return o;
  }
  fixed4 frag(v2f i) : SV_Target {
   fixed4 baseColor = tex2D(_MainTex, i.uv);
   fixed4 cubeColor = texCUBE(_Cube, normalize(i.direction));
   fixed blend = saturate(_Intensity * 0.35);
   fixed4 result = lerp(baseColor, cubeColor, blend);
   result.a = baseColor.a;
   return result;
  }
  ENDCG
 }
}
}
