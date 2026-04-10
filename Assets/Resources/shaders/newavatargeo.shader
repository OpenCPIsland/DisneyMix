// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "NewAvatarGeo" {
Properties {
 AccessoryColor ("Accessory Color", Color) = (1.000000,1.000000,1.000000,1.000000)
 AccessoryXOffset ("Accessory X Offset", Float) = 0.000000
 AccessoryYOffset ("Accessory Y Offset", Float) = 0.000000
 AccessoryBlendMode00 ("Prop Blend Mode", Float) = 0.000000
 AccessoryBlendMode01 ("Prop Mask Mode", Float) = 0.000000
 AccessoryCrop ("Accessory crop", Float) = 1.000000
 AccessoryTex00 ("Prop", 2D) = "alpha" { }
 AccessoryTex01 ("Prop Diffuse Layer", 2D) = "alpha" { }
 AccessoryMask ("Prop Mask", 2D) = "alpha" { }
 _MainTex ("Main Texture", 2D) = "alpha" { }
}
SubShader { 
 Pass {
  GpuProgramID 41879
CGPROGRAM
//#pragma target 4.0

#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"


#define CODE_BLOCK_VERTEX
//uniform float4x4 UNITY_MATRIX_MVP;
uniform float4 AccessoryColor;
uniform int AccessoryBlendMode00;
uniform int AccessoryBlendMode01;
uniform sampler2D AccessoryTex00;
uniform sampler2D AccessoryTex01;
uniform sampler2D AccessoryMask;
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
    if((AccessoryBlendMode00!=5))
    {
        if((AccessoryBlendMode01==1))
        {
            float4 tmpvar_2;
            tmpvar_2 = tex2D(AccessoryTex00, in_f.xlv_TEXCOORD0.xy);
            float4 tmpvar_3;
            tmpvar_3 = tex2D(AccessoryTex01, in_f.xlv_TEXCOORD0.xy);
            float4 maskColor_4;
            maskColor_4.xyz = (tmpvar_2.xyz * AccessoryColor.xyz);
            maskColor_4.w = tmpvar_2.w;
            float4 tmpvar_5;
            tmpvar_5 = (maskColor_4 * tmpvar_2.wwww);
            outputColor_1 = tmpvar_5;
            float4 tmpvar_6;
            if((tmpvar_3.w==0))
            {
                tmpvar_6 = tmpvar_5;
            }
            else
            {
                tmpvar_6 = (tmpvar_3 * tmpvar_5);
            }
            outputColor_1 = lerp(tmpvar_5, tmpvar_6, tmpvar_3.wwww);
        }
        else
        {
            if((AccessoryBlendMode01==3))
            {
                float4 tmpvar_7;
                tmpvar_7 = tex2D(AccessoryTex00, in_f.xlv_TEXCOORD0.xy);
                float4 tmpvar_8;
                tmpvar_8 = tex2D(AccessoryTex01, in_f.xlv_TEXCOORD0.xy);
                float4 maskColor_9;
                maskColor_9.xyz = (tmpvar_7.xyz * AccessoryColor.xyz);
                maskColor_9.w = tmpvar_7.w;
                outputColor_1 = lerp(lerp(outputColor_1, maskColor_9, tmpvar_7.wwww), tmpvar_8, tmpvar_8.wwww);
            }
            else
            {
                float4 tmpvar_10;
                tmpvar_10 = tex2D(AccessoryTex00, in_f.xlv_TEXCOORD0.xy);
                float4 tmpvar_11;
                tmpvar_11 = tex2D(AccessoryMask, in_f.xlv_TEXCOORD0.xy);
                if((tmpvar_10.w>0.01))
                {
                    float4 finalColor_12;
                    finalColor_12 = tmpvar_10;
                    if(((tmpvar_10.w>0) && (tmpvar_11.w>0.001)))
                    {
                        float3 rgbColor_13;
                        rgbColor_13 = AccessoryColor.xyz;
                        float tmpvar_14;
                        tmpvar_14 = (((0.596 * rgbColor_13.x) - (0.275 * rgbColor_13.y)) - (0.321 * rgbColor_13.z));
                        float tmpvar_15;
                        tmpvar_15 = (((0.212 * rgbColor_13.x) - (0.523 * rgbColor_13.y)) + (0.311 * rgbColor_13.z));
                        float tmpvar_16;
                        float tmpvar_17;
                        tmpvar_17 = (min(abs((tmpvar_15 / tmpvar_14)), 1) / max(abs((tmpvar_15 / tmpvar_14)), 1));
                        float tmpvar_18;
                        tmpvar_18 = (tmpvar_17 * tmpvar_17);
                        tmpvar_18 = (((((((((((-0.01213232 * tmpvar_18) + 0.05368138) * tmpvar_18) - 0.1173503) * tmpvar_18) + 0.1938925) * tmpvar_18) - 0.3326756) * tmpvar_18) + 0.9999793) * tmpvar_17);
                        tmpvar_18 = (tmpvar_18 + (float((abs((tmpvar_15 / tmpvar_14))>1)) * ((tmpvar_18 * (-2)) + 1.570796)));
                        tmpvar_16 = (tmpvar_18 * sign((tmpvar_15 / tmpvar_14)));
                        if((abs(tmpvar_14)>(1E-08 * abs(tmpvar_15))))
                        {
                            if((tmpvar_14<0))
                            {
                                if((tmpvar_15>=0))
                                {
                                    tmpvar_16 = (tmpvar_16 + 3.141593);
                                }
                                else
                                {
                                    tmpvar_16 = (tmpvar_16 - 3.141593);
                                }
                            }
                        }
                        else
                        {
                            tmpvar_16 = (sign(tmpvar_15) * 1.570796);
                        }
                        float3 tmpvar_19;
                        tmpvar_19.x = tmpvar_16;
                        tmpvar_19.y = sqrt(((tmpvar_14 * tmpvar_14) + (tmpvar_15 * tmpvar_15)));
                        tmpvar_19.z = (((0.299 * rgbColor_13.x) + (0.587 * rgbColor_13.y)) + (0.114 * rgbColor_13.z));
                        float3 rgbColor_20;
                        rgbColor_20 = tmpvar_10.xyz;
                        float tmpvar_21;
                        tmpvar_21 = (((0.596 * rgbColor_20.x) - (0.275 * rgbColor_20.y)) - (0.321 * rgbColor_20.z));
                        float tmpvar_22;
                        tmpvar_22 = (((0.212 * rgbColor_20.x) - (0.523 * rgbColor_20.y)) + (0.311 * rgbColor_20.z));
                        float tmpvar_23;
                        float tmpvar_24;
                        tmpvar_24 = (min(abs((tmpvar_22 / tmpvar_21)), 1) / max(abs((tmpvar_22 / tmpvar_21)), 1));
                        float tmpvar_25;
                        tmpvar_25 = (tmpvar_24 * tmpvar_24);
                        tmpvar_25 = (((((((((((-0.01213232 * tmpvar_25) + 0.05368138) * tmpvar_25) - 0.1173503) * tmpvar_25) + 0.1938925) * tmpvar_25) - 0.3326756) * tmpvar_25) + 0.9999793) * tmpvar_24);
                        tmpvar_25 = (tmpvar_25 + (float((abs((tmpvar_22 / tmpvar_21))>1)) * ((tmpvar_25 * (-2)) + 1.570796)));
                        tmpvar_23 = (tmpvar_25 * sign((tmpvar_22 / tmpvar_21)));
                        if((abs(tmpvar_21)>(1E-08 * abs(tmpvar_22))))
                        {
                            if((tmpvar_21<0))
                            {
                                if((tmpvar_22>=0))
                                {
                                    tmpvar_23 = (tmpvar_23 + 3.141593);
                                }
                                else
                                {
                                    tmpvar_23 = (tmpvar_23 - 3.141593);
                                }
                            }
                        }
                        else
                        {
                            tmpvar_23 = (sign(tmpvar_22) * 1.570796);
                        }
                        float3 tmpvar_26;
                        tmpvar_26.x = tmpvar_23;
                        tmpvar_26.y = sqrt(((tmpvar_21 * tmpvar_21) + (tmpvar_22 * tmpvar_22)));
                        tmpvar_26.z = (((0.299 * rgbColor_20.x) + (0.587 * rgbColor_20.y)) + (0.114 * rgbColor_20.z));
                        float tmpvar_27;
                        tmpvar_27 = (tmpvar_19.y * sin(tmpvar_16));
                        float tmpvar_28;
                        tmpvar_28 = (tmpvar_19.y * cos(tmpvar_16));
                        float3 tmpvar_29;
                        tmpvar_29.x = ((tmpvar_26.z + (0.956 * tmpvar_28)) + (0.621 * tmpvar_27));
                        tmpvar_29.y = ((tmpvar_26.z - (0.272 * tmpvar_28)) - (0.647 * tmpvar_27));
                        tmpvar_29.z = ((tmpvar_26.z - (1.107 * tmpvar_28)) + (1.704 * tmpvar_27));
                        finalColor_12.xyz = float3(tmpvar_29);
                        finalColor_12 = lerp(tmpvar_10, finalColor_12, tmpvar_11.wwww);
                    }
                    outputColor_1 = lerp(tmpvar_10, finalColor_12, finalColor_12.wwww);
                }
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