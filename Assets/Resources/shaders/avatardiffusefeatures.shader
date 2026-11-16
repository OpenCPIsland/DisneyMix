Shader "AvatarDiffuseFeatures" {
Properties {
 HairColor ("Hair Color", Color) = (1.000000,1.000000,1.000000,1.000000)
 AccessoryColor ("Accessory Color", Color) = (1.000000,1.000000,1.000000,1.000000)
 BrowXOffset ("Eyebrows X Offset", Float) = 0.000000
 BrowYOffset ("Eyebrows Y Offset", Float) = 0.000000
 AccessoryXOffset ("Accessories X Offset", Float) = 0.000000
 AccessoryYOffset ("Accessories Y Offset", Float) = 0.000000
 HairXOffset ("Hair X Offset", Float) = 0.000000
 HairYOffset ("Hair Y Offset", Float) = 0.000000
 BrowBlendMode00 ("Brow Blend Mode", Float) = 0.000000
 AccessoryBlendMode00 ("Accessory Blend Mode", Float) = 0.000000
 AccessoryBlendMode01 ("Accessory Blend Mode", Float) = 0.000000
 HairBlendMode00 ("Hair Blend Mode", Float) = 0.000000
 HairBlendMode01 ("Hair Highlight Blend Mode", Float) = 0.000000
 HairBlendMode02 ("Hair Shadow Blend Mode", Float) = 0.000000
 AvatarDiffuseBase ("Avatar Diffuse Base", 2D) = "white" { }
 BrowTex00 ("Eyebrows", 2D) = "alpha" { }
 AccessoryTex00 ("Accessory", 2D) = "alpha" { }
 AccessoryMask ("Accessory Mask", 2D) = "alpha" { }
 HairTex00 ("Hair", 2D) = "alpha" { }
 HairTex01 ("Hair Highlights", 2D) = "alpha" { }
 HairTex02 ("Hair Shadow", 2D) = "alpha" { }
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
