Shader "Surface/Masked Alpha" {
Properties {
[PerRendererData]  _MainTex ("Sprite Texture", 2D) = "white" { }
 _MaskTex ("Mask", 2D) = "white" { }
 _Color ("Tint", Color) = (1.000000,1.000000,1.000000,1.000000)
[MaterialToggle]  PixelSnap ("Pixel snap", Float) = 0.000000
}
SubShader {
 Tags { "RenderType"="Transparent" "Queue"="Transparent" }
 Pass {
  Tags { "LIGHTMODE"="ForwardBase" "QUEUE"="Transparent" "IGNOREPROJECTOR"="true" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="true" }
  ZWrite Off
  Cull Off
  Blend SrcAlpha OneMinusSrcAlpha
  ColorMask RGB
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MaskTex;
 float4 _MaskTex_ST;
 fixed4 _Color;
 struct appdata { float4 vertex : POSITION; float2 texcoord : TEXCOORD0; fixed4 color : COLOR; };
 struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0;
 fixed4 color : COLOR; };
 v2f vert(appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex);
 o.uv = TRANSFORM_TEX(v.texcoord, _MaskTex);
 o.color = v.color; return o; }
 fixed4 frag(v2f i) : SV_Target {
fixed4 c = tex2D(_MaskTex, i.uv);
 c *= _Color;
 return c; }
 ENDCG
 }
}
}
