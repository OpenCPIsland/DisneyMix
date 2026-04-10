// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "NewAvatarSkin" {
Properties {
 SkinColor ("Skin Color", Color) = (1.000000,1.000000,1.000000,1.000000)
 SkinXOffset ("Skin Augment Offset X", Float) = 0.000000
 SkinYOffset ("Skin Augment Offset Y", Float) = 0.000000
 SkinBlendMode00 ("Skin Augment Blend Mode", Float) = 0.000000
 CostumeBlendMode00 ("Costume Blend Mode", Float) = 5.000000
 SkinTex00 ("Skin Augment", 2D) = "alpha" { }
 CostumeTex00 ("Costume", 2D) = "alpha" { }
 _MainTex ("Main Texture", 2D) = "white" { }
}
SubShader { 
 Pass {
  GpuProgramID 64999
CGPROGRAM
//#pragma target 4.0

#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"


#define CODE_BLOCK_VERTEX
//uniform float4x4 UNITY_MATRIX_MVP;
uniform float4 SkinColor;
uniform float SkinCrop;
uniform float SkinXOffset;
uniform float SkinYOffset;
uniform int SkinBlendMode00;
uniform int CostumeBlendMode00;
uniform sampler2D SkinTex00;
uniform sampler2D CostumeTex00;
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
    float4 outputColor_1;
    if((CostumeBlendMode00!=5))
    {
        outputColor_1 = tex2D(CostumeTex00, in_f.xlv_TEXCOORD0.xy);
    }
    else
    {
        outputColor_1 = SkinColor;
        float2 tmpvar_2;
        tmpvar_2.x = SkinXOffset;
        tmpvar_2.y = SkinYOffset;
        float2 tmpvar_3;
        float2 tmpvar_4;
        tmpvar_4 = ((((in_f.xlv_TEXCOORD0.xy + tmpvar_2) * float2(SkinCrop, SkinCrop)) - float2(SkinCrop, SkinCrop)) + float2(1, 1));
        if(((tmpvar_4.x<0) || (tmpvar_4.y<0)))
        {
            tmpvar_3 = float2(0, 0);
        }
        else
        {
            tmpvar_3 = tmpvar_4;
        }
        float4 texColor_5;
        texColor_5 = tex2D(SkinTex00, tmpvar_3);
        float4 tmpvar_6;
        if((SkinBlendMode00==0))
        {
            tmpvar_6 = lerp(SkinColor, texColor_5, texColor_5.wwww);
        }
        else
        {
            if((SkinBlendMode00==1))
            {
                float4 tmpvar_7;
                if((texColor_5.w==0))
                {
                    tmpvar_7 = SkinColor;
                }
                else
                {
                    tmpvar_7 = (texColor_5 * SkinColor);
                }
                tmpvar_6 = lerp(SkinColor, tmpvar_7, texColor_5.wwww);
            }
            else
            {
                if((SkinBlendMode00==2))
                {
                    tmpvar_6 = lerp(SkinColor, (1 - ((1 - texColor_5) * (1 - SkinColor))), texColor_5.wwww);
                }
                else
                {
                    tmpvar_6 = SkinColor;
                }
            }
        }
        outputColor_1 = tmpvar_6;
    }
    out_f.color = outputColor_1;
    return out_f;
}


ENDCG

}
}
}