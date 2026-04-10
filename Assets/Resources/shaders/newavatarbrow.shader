// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "NewAvatarBrow" {
Properties {
 BrowColor ("Brow Color", Color) = (1.000000,1.000000,1.000000,1.000000)
 BrowXOffset ("Eyebrows X Offset", Float) = 0.000000
 BrowYOffset ("Eyebrows Y Offset", Float) = 0.000000
 BrowBlendMode00 ("Brow Blend Mode", Float) = 0.000000
 BrowCrop ("Brow crop", Float) = 1.000000
 BrowTex00 ("Eyebrows", 2D) = "alpha" { }
 _MainTex ("Main Texture", 2D) = "alpha" { }
}
SubShader { 
 Pass {
  GpuProgramID 42393
CGPROGRAM
//#pragma target 4.0

#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"


#define CODE_BLOCK_VERTEX
//uniform float4x4 UNITY_MATRIX_MVP;
uniform float BrowCrop;
uniform float BrowXOffset;
uniform float BrowYOffset;
uniform float4 BrowColor;
uniform int BrowBlendMode00;
uniform sampler2D BrowTex00;
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
    tmpvar_2.x = BrowXOffset;
    tmpvar_2.y = BrowYOffset;
    float2 tmpvar_3;
    float2 tmpvar_4;
    tmpvar_4 = ((((in_f.xlv_TEXCOORD0.xy + tmpvar_2) * float2(BrowCrop, BrowCrop)) - float2(BrowCrop, BrowCrop)) + float2(1, 1));
    if(((tmpvar_4.x<0) || (tmpvar_4.y<0)))
    {
        tmpvar_3 = float2(0, 0);
    }
    else
    {
        tmpvar_3 = tmpvar_4;
    }
    float4 tmpvar_5;
    tmpvar_5 = tex2D(BrowTex00, tmpvar_3);
    if((BrowBlendMode00==3))
    {
        outputColor_1 = tmpvar_5;
    }
    else
    {
        float4 tmpvar_6;
        if((BrowBlendMode00==0))
        {
            float4 texColor_7;
            texColor_7.w = tmpvar_5.w;
            texColor_7.xyz = (tmpvar_5.xyz * BrowColor.xyz);
            tmpvar_6 = lerp(outputColor_1, texColor_7, tmpvar_5.wwww);
        }
        else
        {
            tmpvar_6 = outputColor_1;
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