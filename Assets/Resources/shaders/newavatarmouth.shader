// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "NewAvatarMouth" {
Properties {
 MouthColor ("Mouth Color", Color) = (1.000000,1.000000,1.000000,1.000000)
 MouthXOffset ("Mouth Offset X", Float) = 0.000000
 MouthYOffset ("Mouth Offset Y", Float) = 0.000000
 MouthBlendMode00 ("Mouth Blend Mode", Float) = 0.000000
 MouthCrop ("Mouth crop", Float) = 1.000000
 MouthTex00 ("Mouth", 2D) = "alpha" { }
 _MainTex ("Main Texture", 2D) = "alpha" { }
}
SubShader { 
 Pass {
  GpuProgramID 57756
CGPROGRAM
//#pragma target 4.0

#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"


#define CODE_BLOCK_VERTEX
//uniform float4x4 UNITY_MATRIX_MVP;
uniform float4 MouthColor;
uniform float MouthCrop;
uniform float MouthXOffset;
uniform float MouthYOffset;
uniform int MouthBlendMode00;
uniform int MouthBlendMode01;
uniform sampler2D MouthTex00;
uniform sampler2D MouthTex01;
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
    outputColor_1 = float4(0, 0, 0, 0);
    float2 tmpvar_2;
    tmpvar_2.x = MouthXOffset;
    tmpvar_2.y = MouthYOffset;
    float2 tmpvar_3;
    float2 tmpvar_4;
    tmpvar_4 = ((((in_f.xlv_TEXCOORD0.xy + tmpvar_2) * float2(MouthCrop, MouthCrop)) - float2(MouthCrop, MouthCrop)) + float2(1, 1));
    if(((tmpvar_4.x<0) || (tmpvar_4.y<0)))
    {
        tmpvar_3 = float2(0, 0);
    }
    else
    {
        tmpvar_3 = tmpvar_4;
    }
    if((MouthBlendMode00<4))
    {
        if((MouthBlendMode01==5))
        {
            outputColor_1 = tex2D(MouthTex00, tmpvar_3);
        }
        else
        {
            outputColor_1 = MouthColor;
            float4 tmpvar_5;
            tmpvar_5 = tex2D(MouthTex00, tmpvar_3);
            float4 tmpvar_6;
            tmpvar_6 = tex2D(MouthTex01, tmpvar_3);
            if(((tmpvar_6.w>0.01) || (tmpvar_5.w>0.01)))
            {
                if(((tmpvar_6.w>0.01) && (tmpvar_5.w<0.01)))
                {
                    outputColor_1.w = tmpvar_6.w;
                }
                float4 tmpvar_7;
                if((MouthBlendMode00==0))
                {
                    tmpvar_7 = lerp(outputColor_1, tmpvar_5, tmpvar_5.wwww);
                }
                else
                {
                    if((MouthBlendMode00==1))
                    {
                        float4 tmpvar_8;
                        if((tmpvar_5.w==0))
                        {
                            tmpvar_8 = outputColor_1;
                        }
                        else
                        {
                            tmpvar_8 = (tmpvar_5 * outputColor_1);
                        }
                        tmpvar_7 = lerp(outputColor_1, tmpvar_8, tmpvar_5.wwww);
                    }
                    else
                    {
                        tmpvar_7 = outputColor_1;
                    }
                }
                outputColor_1 = tmpvar_7;
                float4 tmpvar_9;
                if((MouthBlendMode01==1))
                {
                    float4 tmpvar_10;
                    if((tmpvar_6.w==0))
                    {
                        tmpvar_10 = tmpvar_7;
                    }
                    else
                    {
                        tmpvar_10 = (tmpvar_6 * tmpvar_7);
                    }
                    tmpvar_9 = lerp(tmpvar_7, tmpvar_10, tmpvar_6.wwww);
                }
                else
                {
                    tmpvar_9 = tmpvar_7;
                }
                outputColor_1 = tmpvar_9;
            }
            else
            {
                outputColor_1 = tmpvar_5;
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