Shader "Depth Mask" {

SubShader {
 Pass {
  Tags { "QUEUE"="Geometry-10" }
  ColorMask 0
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata { float4 vertex : POSITION; float2 texcoord : TEXCOORD0; fixed4 color : COLOR; };
 struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; };
 v2f vert(appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex);
 o.color = v.color; return o; }
 fixed4 frag(v2f i) : SV_Target {
fixed4 c = fixed4(1,1,1,1);
 return c; }
 ENDCG
 }
}
}
