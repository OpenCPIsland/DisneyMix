Shader "FX/Water"
{
    Properties
    {
        _WaveScale ("Wave scale", Range(0.02, 0.15)) = 0.063
        _ReflDistort ("Reflection distort", Range(0, 1.5)) = 0.44
        _RefrDistort ("Refraction distort", Range(0, 1.5)) = 0.4
        _RefrColor ("Refraction color", Color) = (0.34, 0.85, 0.92, 1)
        [NoScaleOffset] _Fresnel ("Fresnel (A)", 2D) = "gray" {}
        [NoScaleOffset] _BumpMap ("Normalmap", 2D) = "bump" {}
        WaveSpeed ("Wave speed (map1 x,y; map2 x,y)", Vector) = (19, 9, -16, -7)
        [NoScaleOffset] _ReflectiveColor ("Reflective color (RGB) fresnel (A)", 2D) = "white" {}
        _HorizonColor ("Simple water horizon color", Color) = (0.172, 0.463, 0.435, 1)
        [HideInInspector] _ReflectionTex ("Internal Reflection", 2D) = "black" {}
        [HideInInspector] _RefractionTex ("Internal Refraction", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "WaterMode"="Refractive" }
        Pass
        {
            CGPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile WATER_REFRACTIVE WATER_REFLECTIVE WATER_SIMPLE
            #include "UnityCG.cginc"

            sampler2D _Fresnel;
            sampler2D _BumpMap;
            sampler2D _ReflectiveColor;
            sampler2D _ReflectionTex;
            sampler2D _RefractionTex;
            float4 _WaveScale4;
            float4 _WaveOffset;
            float4 _RefrColor;
            float4 _HorizonColor;
            float _ReflDistort;
            float _RefrDistort;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 screenPosition : TEXCOORD0;
                float2 bumpUV0 : TEXCOORD1;
                float2 bumpUV1 : TEXCOORD2;
                float3 viewDirection : TEXCOORD3;
                UNITY_FOG_COORDS(4)
            };

            v2f vert(appdata v)
            {
                v2f o;
                float3 worldPosition = mul(unity_ObjectToWorld, v.vertex).xyz;
                float4 waveUV = worldPosition.xzxz * _WaveScale4 + _WaveOffset;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPosition = ComputeScreenPos(o.vertex);
                o.bumpUV0 = waveUV.xy;
                o.bumpUV1 = waveUV.wz;
                o.viewDirection = (_WorldSpaceCameraPos - worldPosition).xzy;
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                half3 bump0 = UnpackNormal(tex2D(_BumpMap, i.bumpUV0));
                half3 bump1 = UnpackNormal(tex2D(_BumpMap, i.bumpUV1));
                half3 bump = (bump0 + bump1) * 0.5h;
                half fresnelFactor = dot(normalize(i.viewDirection), bump);
                fixed4 color;

                #if defined(WATER_REFRACTIVE) || defined(WATER_REFLECTIVE)
                    float4 reflectionUV = i.screenPosition;
                    reflectionUV.xy += bump.xy * _ReflDistort;
                    fixed4 reflection = tex2Dproj(_ReflectionTex, UNITY_PROJ_COORD(reflectionUV));
                #endif

                #if defined(WATER_REFRACTIVE)
                    float4 refractionUV = i.screenPosition;
                    refractionUV.xy -= bump.xy * _RefrDistort;
                    fixed4 refraction = tex2Dproj(_RefractionTex, UNITY_PROJ_COORD(refractionUV)) * _RefrColor;
                    half fresnel = tex2D(_Fresnel, float2(fresnelFactor, fresnelFactor)).a;
                    color = lerp(refraction, reflection, fresnel);
                #elif defined(WATER_REFLECTIVE)
                    fixed4 water = tex2D(_ReflectiveColor, float2(fresnelFactor, fresnelFactor));
                    color = fixed4(lerp(water.rgb, reflection.rgb, water.a), reflection.a * water.a);
                #else
                    fixed4 water = tex2D(_ReflectiveColor, float2(fresnelFactor, fresnelFactor));
                    color = fixed4(lerp(water.rgb, _HorizonColor.rgb, water.a), _HorizonColor.a);
                #endif
                UNITY_APPLY_FOG(i.fogCoord, color);
                return color;
            }
            ENDCG
        }
    }
}
