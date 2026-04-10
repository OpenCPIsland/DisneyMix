// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "NewAvatarEyes" {
Properties {
 EyesColor ("Eye Color", Color) = (1.000000,1.000000,1.000000,1.000000)
 EyesXOffset ("Eye Offset X", Float) = 0.000000
 EyesYOffset ("Eye Offset Y", Float) = 0.000000
 EyesBlendMode00 ("Eyes Blend Mode", Float) = 0.000000
 EyesBlendMode02 ("Iris Blend Mode", Float) = 0.000000
 EyesCrop ("Eyes crop", Float) = 1.000000
 EyesTex00 ("Eyes", 2D) = "alpha" { }
 EyesMask ("Eye Mask", 2D) = "alpha" { }
 _MainTex ("Main Texture", 2D) = "alpha" { }
}
SubShader { 
 Pass {
  GpuProgramID 40352
CGPROGRAM
//#pragma target 4.0

#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"


#define CODE_BLOCK_VERTEX
//uniform float4x4 UNITY_MATRIX_MVP;
uniform float4 EyesColor;
uniform float EyesCrop;
uniform float EyesXOffset;
uniform float EyesYOffset;
uniform sampler2D EyesTex00;
uniform sampler2D EyesMask;
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
    tmpvar_1.x = EyesXOffset;
    tmpvar_1.y = EyesYOffset;
    float2 tmpvar_2;
    float2 tmpvar_3;
    tmpvar_3 = ((((in_f.xlv_TEXCOORD0.xy + tmpvar_1) * float2(EyesCrop, EyesCrop)) - float2(EyesCrop, EyesCrop)) + float2(1, 1));
    if(((tmpvar_3.x<0) || (tmpvar_3.y<0)))
    {
        tmpvar_2 = float2(0, 0);
    }
    else
    {
        tmpvar_2 = tmpvar_3;
    }
    float4 tmpvar_4;
    tmpvar_4 = tex2D(EyesTex00, tmpvar_2);
    float4 tmpvar_5;
    tmpvar_5 = (tmpvar_4 * tmpvar_4.wwww);
    float4 tmpvar_6;
    tmpvar_6 = tex2D(EyesMask, tmpvar_2);
    float4 finalColor_7;
    finalColor_7 = tmpvar_4;
    if(((tmpvar_4.w>0) && (tmpvar_6.w>0.001)))
    {
        float value_8;
        float3 finalHsv_9;
        float3 tintHsv_10;
        float3 rgbColor_11;
        rgbColor_11 = EyesColor.xyz;
        float4 hsv_12;
        hsv_12.z = max(max(rgbColor_11.x, rgbColor_11.y), rgbColor_11.z);
        float tmpvar_13;
        tmpvar_13 = (hsv_12.z - min(min(rgbColor_11.x, rgbColor_11.y), rgbColor_11.z));
        if((tmpvar_13!=0))
        {
            float3 delta2_14;
            hsv_12.y = (tmpvar_13 / hsv_12.z);
            float3 tmpvar_15;
            tmpvar_15 = ((hsv_12.z - rgbColor_11) / tmpvar_13);
            delta2_14 = (tmpvar_15 - tmpvar_15.zxy);
            delta2_14.xy = (delta2_14.xy + float2(2, 4));
            if((rgbColor_11.x>=hsv_12.z))
            {
                hsv_12.x = delta2_14.z;
            }
            else
            {
                if((rgbColor_11.y>=hsv_12.z))
                {
                    hsv_12.x = delta2_14.x;
                }
                else
                {
                    hsv_12.x = delta2_14.y;
                }
            }
            hsv_12.x = frac((hsv_12.x / 6));
        }
        else
        {
            hsv_12.xy = float2(0, 0);
        }
        float3 tmpvar_16;
        tmpvar_16 = hsv_12.xyz;
        tintHsv_10 = tmpvar_16;
        float3 tmpvar_17;
        tmpvar_17 = tmpvar_4.xyz;
        float3 rgbColor_18;
        rgbColor_18 = tmpvar_17;
        float tmpvar_19;
        tmpvar_19 = max(max(rgbColor_18.x, rgbColor_18.y), rgbColor_18.z);
        value_8 = tmpvar_19;
        if((value_8>0.5))
        {
            float3 tmpvar_20;
            tmpvar_20.x = tintHsv_10.x;
            tmpvar_20.y = ((1 - ((value_8 * 2) - 1)) * tintHsv_10.y);
            tmpvar_20.z = lerp(tintHsv_10.z, 1, ((value_8 * 2) - 1));
            finalHsv_9 = tmpvar_20;
        }
        else
        {
            float3 tmpvar_21;
            tmpvar_21.xy = tintHsv_10.xy;
            tmpvar_21.z = ((value_8 * 2) * tintHsv_10.z);
            finalHsv_9 = tmpvar_21;
        }
        float3 tmpvar_22;
        float3 hsv_23;
        hsv_23 = finalHsv_9;
        float3 tmpvar_24;
        tmpvar_24.x = (abs(((hsv_23.x * 6) - 3)) - 1);
        tmpvar_24.y = (2 - abs(((hsv_23.x * 6) - 2)));
        tmpvar_24.z = (2 - abs(((hsv_23.x * 6) - 4)));
        tmpvar_22 = ((((clamp(tmpvar_24, 0, 1) - 1) * hsv_23.y) + 1) * hsv_23.z);
        finalColor_7.xyz = float3(tmpvar_22);
        finalColor_7 = lerp(tmpvar_4, finalColor_7, tmpvar_6.wwww);
    }
    float4 tmpvar_25;
    tmpvar_25 = lerp(tmpvar_5, finalColor_7, finalColor_7.wwww);
    out_f.color = tmpvar_25;
    return out_f;
}


ENDCG

}
}
}