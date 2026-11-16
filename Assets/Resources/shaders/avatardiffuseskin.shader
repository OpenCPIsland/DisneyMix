Shader "AvatarDiffuseSkin" {
Properties {
 SkinColor ("Skin Color", Color) = (1.000000,1.000000,1.000000,1.000000)
 EyesColor ("Eye Color", Color) = (1.000000,1.000000,1.000000,1.000000)
 SkinXOffset ("Skin Augment Offset X", Float) = 0.000000
 SkinYOffset ("Skin Augment Offset Y", Float) = 0.000000
 MouthXOffset ("Mouth Offset X", Float) = 0.000000
 MouthYOffset ("Mouth Offset Y", Float) = 0.000000
 NoseXOffset ("Nose Offset X", Float) = 0.000000
 NoseYOffset ("Nose Offset Y", Float) = 0.000000
 EyesXOffset ("Eye Offset X", Float) = 0.000000
 EyesYOffset ("Eye Offset Y", Float) = 0.000000
 SkinBlendMode00 ("Skin Augment Blend Mode", Float) = 0.000000
 MouthBlendMode00 ("Mouth Blend Mode", Float) = 0.000000
 MouthBlendMode01 ("Mouth Dimple Blend Mode", Float) = 0.000000
 NoseBlendMode00 ("Nose Blend Mode", Float) = 0.000000
 NoseBlendMode01 ("Nose Highlight Blend Mode", Float) = 0.000000
 EyesBlendMode00 ("Eyes Blend Mode", Float) = 0.000000
 EyesBlendMode01 ("Eye Shadow Blend Mode", Float) = 0.000000
 EyesBlendMode02 ("Iris Blend Mode", Float) = 0.000000
 SkinTex00 ("Skin Augment", 2D) = "alpha" { }
 MouthTex00 ("Mouth", 2D) = "alpha" { }
 MouthTex01 ("Cheek", 2D) = "alpha" { }
 NoseTex00 ("Nose", 2D) = "alpha" { }
 NoseTex01 ("Nose Highlight", 2D) = "alpha" { }
 EyesTex00 ("Eyes", 2D) = "alpha" { }
 EyesTex01 ("Eye Shadow", 2D) = "alpha" { }
 EyesTex02 ("Iris", 2D) = "alpha" { }
 EyesMask ("Eye Mask", 2D) = "alpha" { }
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
