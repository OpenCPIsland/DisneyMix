Shader "Projector/Multiply" {
Properties {
 _ShadowTex ("Cookie", 2D) = "gray" { }
 _FalloffTex ("FallOff", 2D) = "white" { }
}
SubShader {
 Tags { "RenderType"="Transparent" "Queue"="Transparent" }
 Pass {
  Tags { "QUEUE"="Transparent" }
  ZWrite Off
  Blend DstColor Zero
  ColorMask RGB
  Offset -1.000000, -1.000000
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _ShadowTex;
 float4 _ShadowTex_ST;
 struct appdata { float4 vertex : POSITION; float2 texcoord : TEXCOORD0; fixed4 color : COLOR; };
 struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0;
 fixed4 color : COLOR; };
 v2f vert(appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex);
 o.uv = TRANSFORM_TEX(v.texcoord, _ShadowTex);
 o.color = v.color; return o; }
 fixed4 frag(v2f i) : SV_Target {
fixed4 c = tex2D(_ShadowTex, i.uv);
 return c; }
 ENDCG
 }
}
}
