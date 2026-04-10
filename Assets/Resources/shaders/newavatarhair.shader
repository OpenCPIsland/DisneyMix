// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "NewAvatarHair" {
Properties {
 HairColor ("Hair Color", Color) = (1.000000,1.000000,1.000000,1.000000)
 SkinColor ("Skin Color", Color) = (1.000000,1.000000,1.000000,1.000000)
 SkinXOffset ("Skin Augment Offset X", Float) = 0.000000
 SkinYOffset ("Skin Augment Offset Y", Float) = 0.000000
 NoseXOffset ("Nose Offset X", Float) = 0.000000
 NoseYOffset ("Nose Offset Y", Float) = 0.000000
 HairXOffset ("Hair X Offset", Float) = 0.000000
 HairYOffset ("Hair Y Offset", Float) = 0.000000
 SkinBlendMode00 ("Skin Augment Blend Mode", Float) = 0.000000
 CostumeBlendMode00 ("Costume Blend Mode", Float) = 5.000000
 NoseBlendMode00 ("Nose Blend Mode", Float) = 0.000000
 NoseBlendMode01 ("Nose Screen Blend Mode", Float) = 0.000000
 HairBlendMode00 ("Hair Blend Mode", Float) = 0.000000
 HairBlendMode01 ("Hair Highlight Blend Mode", Float) = 0.000000
 HairBlendMode02 ("Hair Shadow Blend Mode", Float) = 0.000000
 NoseCrop ("Nose crop", Float) = 1.000000
 SkinTex00 ("Skin Augment", 2D) = "alpha" { }
 CostumeTex00 ("Costume", 2D) = "alpha" { }
 NoseTex00 ("Nose", 2D) = "alpha" { }
 NoseTex01 ("Nose Screen", 2D) = "alpha" { }
 HairTex00 ("Hair", 2D) = "alpha" { }
 HairTex01 ("Hair Highlights", 2D) = "alpha" { }
 HairTex02 ("Hair Shadow", 2D) = "alpha" { }
 _MainTex ("Main Texture", 2D) = "alpha" { }
}
SubShader { 
 Pass {
  GpuProgramID 60101
CGPROGRAM
//#pragma target 4.0

#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"


#define CODE_BLOCK_VERTEX
//uniform float4x4 UNITY_MATRIX_MVP;
uniform float NoseCrop;
uniform float SkinCrop;
uniform float SkinXOffset;
uniform float SkinYOffset;
uniform float NoseXOffset;
uniform float NoseYOffset;
uniform float HairXOffset;
uniform float HairYOffset;
uniform float4 SkinColor;
uniform float4 HairColor;
uniform int SkinBlendMode00;
uniform int CostumeBlendMode00;
uniform int NoseBlendMode00;
uniform int HairBlendMode00;
uniform int HairBlendMode01;
uniform int HairBlendMode02;
uniform sampler2D SkinTex00;
uniform sampler2D CostumeTex00;
uniform sampler2D NoseTex00;
uniform sampler2D NoseTex01;
uniform sampler2D HairTex00;
uniform sampler2D HairTex01;
uniform sampler2D HairTex02;
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
    outputColor_1 = SkinColor;
    if((CostumeBlendMode00!=5))
    {
        outputColor_1 = tex2D(CostumeTex00, in_f.xlv_TEXCOORD0.xy);
    }
    else
    {
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
            tmpvar_6 = lerp(outputColor_1, texColor_5, texColor_5.wwww);
        }
        else
        {
            if((SkinBlendMode00==1))
            {
                float4 tmpvar_7;
                if((texColor_5.w==0))
                {
                    tmpvar_7 = outputColor_1;
                }
                else
                {
                    tmpvar_7 = (texColor_5 * outputColor_1);
                }
                tmpvar_6 = lerp(outputColor_1, tmpvar_7, texColor_5.wwww);
            }
            else
            {
                if((SkinBlendMode00==2))
                {
                    tmpvar_6 = lerp(outputColor_1, (1 - ((1 - texColor_5) * (1 - outputColor_1))), texColor_5.wwww);
                }
                else
                {
                    tmpvar_6 = outputColor_1;
                }
            }
        }
        outputColor_1 = tmpvar_6;
        if((NoseBlendMode00!=5))
        {
            float2 tmpvar_8;
            tmpvar_8.x = NoseXOffset;
            tmpvar_8.y = NoseYOffset;
            float2 tmpvar_9;
            float2 tmpvar_10;
            tmpvar_10 = ((((in_f.xlv_TEXCOORD0.xy + tmpvar_8) * float2(NoseCrop, NoseCrop)) - float2(NoseCrop, NoseCrop)) + float2(1, 1));
            if(((tmpvar_10.x<0) || (tmpvar_10.y<0)))
            {
                tmpvar_9 = float2(0, 0);
            }
            else
            {
                tmpvar_9 = tmpvar_10;
            }
            float4 tmpvar_11;
            tmpvar_11 = tex2D(NoseTex00, tmpvar_9);
            float4 tmpvar_12;
            if((NoseBlendMode00==0))
            {
                tmpvar_12 = lerp(tmpvar_6, tmpvar_11, tmpvar_11.wwww);
            }
            else
            {
                if((NoseBlendMode00==1))
                {
                    float4 tmpvar_13;
                    if((tmpvar_11.w==0))
                    {
                        tmpvar_13 = tmpvar_6;
                    }
                    else
                    {
                        tmpvar_13 = (tmpvar_11 * tmpvar_6);
                    }
                    tmpvar_12 = lerp(tmpvar_6, tmpvar_13, tmpvar_11.wwww);
                }
                else
                {
                    tmpvar_12 = tmpvar_6;
                }
            }
            float4 tmpvar_14;
            tmpvar_14 = tex2D(NoseTex01, tmpvar_9);
            outputColor_1 = lerp(tmpvar_12, (1 - ((1 - tmpvar_14) * (1 - tmpvar_12))), tmpvar_14.wwww);
        }
        float2 tmpvar_15;
        tmpvar_15.x = HairXOffset;
        tmpvar_15.y = HairYOffset;
        float2 tmpvar_16;
        tmpvar_16 = (in_f.xlv_TEXCOORD0.xy + tmpvar_15);
        float4 tmpvar_17;
        tmpvar_17 = tex2D(HairTex00, tmpvar_16);
        if((HairBlendMode01!=5))
        {
            float4 tmpvar_18;
            if((HairBlendMode00==0))
            {
                float4 texColor_19;
                texColor_19.w = tmpvar_17.w;
                texColor_19.xyz = (tmpvar_17.xyz * HairColor.xyz);
                tmpvar_18 = lerp(outputColor_1, texColor_19, tmpvar_17.wwww);
            }
            else
            {
                tmpvar_18 = outputColor_1;
            }
            outputColor_1 = tmpvar_18;
            float4 tmpvar_20;
            tmpvar_20 = tex2D(HairTex01, tmpvar_16);
            float4 tmpvar_21;
            if((HairBlendMode01==2))
            {
                tmpvar_21 = lerp(tmpvar_18, (1 - ((1 - tmpvar_20) * (1 - tmpvar_18))), tmpvar_20.wwww);
            }
            else
            {
                tmpvar_21 = tmpvar_18;
            }
            outputColor_1 = tmpvar_21;
            float4 tmpvar_22;
            tmpvar_22 = tex2D(HairTex02, tmpvar_16);
            float4 tmpvar_23;
            if((HairBlendMode02==1))
            {
                float4 tmpvar_24;
                if((tmpvar_22.w==0))
                {
                    tmpvar_24 = tmpvar_21;
                }
                else
                {
                    tmpvar_24 = (tmpvar_22 * tmpvar_21);
                }
                tmpvar_23 = lerp(tmpvar_21, tmpvar_24, tmpvar_22.wwww);
            }
            else
            {
                tmpvar_23 = tmpvar_21;
            }
            outputColor_1 = tmpvar_23;
        }
        else
        {
            if((HairBlendMode00!=5))
            {
                outputColor_1 = tex2D(HairTex00, tmpvar_16);
            }
        }
    }
    out_f.color = outputColor_1;
    return out_f;
}


ENDCG

}
}
}