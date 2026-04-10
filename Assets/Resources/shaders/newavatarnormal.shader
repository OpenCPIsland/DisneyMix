// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "NewAvatarNormal" {
Properties {
 NmlColor ("Base Normal Color", Color) = (0.500000,0.500000,1.000000,1.000000)
 AccessoryXOffset ("Accessory Normal Offset X", Float) = 0.000000
 AccessoryYOffset ("Accessory Normal Offset Y", Float) = 0.000000
 HairXOffset ("Hair Normal Offset X", Float) = 0.000000
 HairYOffset ("Hair Normal Offset Y", Float) = 0.000000
 AccessoryBlendMode00 ("Accessory Blend Mode", Float) = 4.000000
 HairBlendMode00 ("Hair Blend Mode", Float) = 4.000000
 AccessoryCrop ("Accessory Crop", Float) = 1.000000
 AccessoryTex00 ("Accessory", 2D) = "white" { }
 HairTex00 ("Hair", 2D) = "white" { }
 _MainTex ("Main Texture", 2D) = "white" { }
}
SubShader { 
 Pass {
  GpuProgramID 17671
CGPROGRAM
//#pragma target 4.0

#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"


#define CODE_BLOCK_VERTEX
//uniform float4x4 UNITY_MATRIX_MVP;
uniform float4 NmlColor;
uniform float AccessoryXOffset;
uniform float AccessoryYOffset;
uniform float HairXOffset;
uniform float HairYOffset;
uniform float AccessoryCrop;
uniform int AccessoryBlendMode00;
uniform int HairBlendMode00;
uniform sampler2D AccessoryTex00;
uniform sampler2D HairTex00;
struct appdata_t
{
    float4 vertex :POSITION;
    float4 texcoord :TEXCOORD0;
};

struct OUT_Data_Vert
{
    float4 xlv_TEXCOORD0 :TEXCOORD0;
    float4 vertex :SV_POSITION;
};

struct v2f
{
    float4 xlv_TEXCOORD0 :TEXCOORD0;
};

struct OUT_Data_Frag
{
    float4 color :SV_Target0;
};

OUT_Data_Vert vert(appdata_t in_v)
{
    OUT_Data_Vert out_v;
    out_v.vertex = UnityObjectToClipPos(in_v.vertex);
    out_v.xlv_TEXCOORD0 = in_v.texcoord;
    return out_v;
}

#define CODE_BLOCK_FRAGMENT
OUT_Data_Frag frag(v2f in_f)
{
    OUT_Data_Frag out_f;
    float2 tmpvar_1;
    tmpvar_1.x = HairXOffset;
    tmpvar_1.y = HairYOffset;
    float2 tmpvar_2;
    tmpvar_2 = (in_f.xlv_TEXCOORD0.xy + tmpvar_1);
    float4 texColor_3;
    texColor_3 = tex2D(HairTex00, tmpvar_2);
    float4 tmpvar_4;
    if((HairBlendMode00==0))
    {
        tmpvar_4 = lerp(NmlColor, texColor_3, texColor_3.wwww);
    }
    else
    {
        tmpvar_4 = NmlColor;
    }
    float2 tmpvar_5;
    tmpvar_5.x = AccessoryXOffset;
    tmpvar_5.y = AccessoryYOffset;
    float2 tmpvar_6;
    float2 tmpvar_7;
    tmpvar_7 = ((((in_f.xlv_TEXCOORD0.xy + tmpvar_5) * float2(AccessoryCrop, AccessoryCrop)) - float2(AccessoryCrop, AccessoryCrop)) + float2(1, 1));
    if(((tmpvar_7.x<0) || (tmpvar_7.y<0)))
    {
        tmpvar_6 = float2(0, 0);
    }
    else
    {
        tmpvar_6 = tmpvar_7;
    }
    float4 tmpvar_8;
    tmpvar_8 = tex2D(AccessoryTex00, tmpvar_6);
    float4 tmpvar_9;
    if((AccessoryBlendMode00==0))
    {
        tmpvar_9 = lerp(tmpvar_4, tmpvar_8, tmpvar_8.wwww);
    }
    else
    {
        tmpvar_9 = tmpvar_4;
    }
    out_f.color = tmpvar_9;
    return out_f;
}


ENDCG

}
}
}