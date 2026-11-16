Shader "AvatarDiffuseGlow" {
Properties {
 CostumeBlendMode00 ("Costume Blend Mode", Float) = 0.000000
 GlowBlendMode00 ("Glow Blend Mode", Float) = 0.000000
 AvatarDiffuseBase ("Avatar Diffuse Base", 2D) = "white" { }
 CostumeTex00 ("Costume", 2D) = "alpha" { }
 GlowTex00 ("Glow", 2D) = "alpha" { }
 _MainTex ("Main Texture", 2D) = "white" { }
}
SubShader {
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;
 float4 _MainTex_ST;
 struct appdata { float4 vertex : POSITION; float2 texcoord : TEXCOORD0; fixed4 color : COLOR; };
 struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0;
 fixed4 color : COLOR; };
 v2f vert(appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex);
 o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
 o.color = v.color; return o; }
 fixed4 frag(v2f i) : SV_Target {
fixed4 c = tex2D(_MainTex, i.uv);
 return c; }
 ENDCG
 }
}
}
