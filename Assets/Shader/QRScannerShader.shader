Shader "Custom/MIXQRScannerShader" {
Properties {
 _MainTex ("Texture", 2D) = "white" { }
 _ScannerTex ("Scanner", 2D) = "white" { }
 _ScanOffset ("Scan Offset", Range(0.000000,1.000000)) = 0.500000
 _BandSize ("Band Size", Range(0.000000,1.000000)) = 0.100000
 _ScanColor ("Scan Color", Color) = (1.000000,1.000000,1.000000,1.000000)
}
SubShader {
 Pass {
  Tags { "RenderType"="Opaque" }
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
