Shader "Custom/gradientlightalpha" {
Properties {
 _MainTex ("Base (RGB)", 2D) = "white" { }
 _RampTex ("Lighting Ramp", 2D) = "white" { }
 _RimColor ("Rim Color", Color) = (1.000000,1.000000,1.000000,1.000000)
 _RimPower ("Rim Power", Float) = 2.000000
 _LightDir ("Lighting Direction", Vector) = (0.000000,0.000000,-1.000000,0.000000)
}
SubShader {
 Tags { "RenderType"="Transparent" "Queue"="Transparent" }
 Pass {
  Tags { "QUEUE"="Transparent" "RenderType"="Transparent" }
  ZWrite Off
  Blend SrcAlpha OneMinusSrcAlpha, One One
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
