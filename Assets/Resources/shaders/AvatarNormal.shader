Shader "AvatarNormals" {
Properties {
 NmlColor ("Base Normal Color", Color) = (0.500000,0.500000,1.000000,1.000000)
 NoseXOffset ("Nose Normal Offset X", Float) = 0.000000
 NoseYOffset ("Nose Normal Offset Y", Float) = 0.000000
 EyesXOffset ("Eyes Normal Offset X", Float) = 0.000000
 EyesYOffset ("Eyes Normal Offset Y", Float) = 0.000000
 BrowXOffset ("Brow Normal Offset X", Float) = 0.000000
 BrowYOffset ("Brow Normal Offset Y", Float) = 0.000000
 AccessoryXOffset ("Accessory Normal Offset X", Float) = 0.000000
 AccessoryYOffset ("Accessory Normal Offset Y", Float) = 0.000000
 HairXOffset ("Hair Normal Offset X", Float) = 0.000000
 HairYOffset ("Hair Normal Offset Y", Float) = 0.000000
 NoseBlendMode00 ("Nose Blend Mode", Float) = 4.000000
 EyesBlendMode00 ("Eyes Blend Mode", Float) = 4.000000
 BrowBlendMode00 ("Brow Blend Mode", Float) = 4.000000
 AccessoryBlendMode00 ("Accessory Blend Mode", Float) = 4.000000
 HairBlendMode00 ("Hair Blend Mode", Float) = 4.000000
 CostumeBlendMode00 ("Costume Blend Mode", Float) = 4.000000
 NoseTex00 ("Nose", 2D) = "white" { }
 EyesTex00 ("Eyes", 2D) = "white" { }
 BrowTex00 ("Eyebrows", 2D) = "white" { }
 AccessoryTex00 ("Accessory", 2D) = "white" { }
 HairTex00 ("Hair", 2D) = "white" { }
 CostumeTex00 ("Costume", 2D) = "white" { }
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
