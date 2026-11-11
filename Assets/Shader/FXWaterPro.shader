Shader "FX/Water" {
Properties {
    _WaveScale ("Wave scale", Range(0.020000,0.150000)) = 0.063000
    _ReflDistort ("Reflection distort", Range(0.000000,1.500000)) = 0.440000
    _RefrDistort ("Refraction distort", Range(0.000000,1.500000)) = 0.400000
    _RefrColor ("Refraction color", Color) = (0.340000,0.850000,0.920000,1.000000)
    [NoScaleOffset] _Fresnel ("Fresnel (A) ", 2D) = "gray" { }
    [NoScaleOffset] _BumpMap ("Normalmap ", 2D) = "bump" { }
    WaveSpeed ("Wave speed (map1 x,y; map2 x,y)", Vector) = (19.000000,9.000000,-16.000000,-7.000000)
    [NoScaleOffset] _ReflectiveColor ("Reflective color (RGB) fresnel (A) ", 2D) = "" { }
    _HorizonColor ("Simple water horizon color", Color) = (0.172000,0.463000,0.435000,1.000000)
    [HideInInspector] _ReflectionTex ("Internal Reflection", 2D) = "" { }
    [HideInInspector] _RefractionTex ("Internal Refraction", 2D) = "" { }
}

SubShader {
    Tags { "Queue" = "Transparent" "RenderType" = "Opaque" }
    LOD 200

    Pass {
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #include "UnityCG.cginc"

        struct appdata_t {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            float2 texcoord : TEXCOORD0;
        };

        struct v2f {
            float4 pos : SV_POSITION;
            float4 uv_Bump : TEXCOORD0;
            float4 screenPos : TEXCOORD1;
            float3 viewDir : TEXCOORD2;
        };

        float _WaveScale;
        float _ReflDistort;
        float _RefrDistort;
        float4 _RefrColor;
        float4 _HorizonColor;
        float4 WaveSpeed;

        sampler2D _BumpMap;
        sampler2D _Fresnel;
        sampler2D _ReflectiveColor;
        sampler2D _ReflectionTex;
        sampler2D _RefractionTex;

        v2f vert (appdata_t v)
        {
            v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);
            
            // Calculate screen coordinates for planareflections/refractions
            o.screenPos = ComputeScreenPos(o.pos);

            // Scale and animate normal map UV coordinates (two scrolling maps combined)
            float4 temp;
            float time = _Time.y;
            temp.xy = v.texcoord * _WaveScale + WaveSpeed.xy * time * 0.0005;
            temp.zw = v.texcoord * _WaveScale + WaveSpeed.zw * time * 0.0005;
            o.uv_Bump = temp;

            // View direction vector in object space
            o.viewDir = ObjSpaceViewDir(v.vertex);

            return o;
        }

        fixed4 frag (v2f i) : SV_Target
        {
            // Sample dual animated normal maps
            half3 normal1 = UnpackNormal(tex2D(_BumpMap, i.uv_Bump.xy));
            half3 normal2 = UnpackNormal(tex2D(_BumpMap, i.uv_Bump.zw));
            half3 bump = (normal1 + normal2) * 0.5;

            // Normalize view vector and calculate basic Fresnel factor (dot product)
            float3 viewDir = normalize(i.viewDir);
            half fresnelFactor = saturate(dot(viewDir, bump));

            // Distort screen space UVs using normal perturbation
            float4 uv1 = i.screenPos;
            uv1.xy += bump.xy * _ReflDistort;
            float4 uv2 = i.screenPos;
            uv2.xy -= bump.xy * _RefrDistort;

            // Sample internal reflection and refraction textures (populated via C# Water script)
            half4 reflColor = tex2Dproj(_ReflectionTex, UNITY_PROJ_COORD(uv1));
            half4 refrColor = tex2Dproj(_RefractionTex, UNITY_PROJ_COORD(uv2)) * _RefrColor;

            // Sample Fresnel/Reflective ramps
            half4 fresnelTerm = tex2D(_Fresnel, float2(fresnelFactor, 0.5));
            half4 reflColorMod = tex2D(_ReflectiveColor, float2(fresnelFactor, 0.5));

            // Blend reflection, refraction, and horizon tint
            half4 finalColor = lerp(refrColor, reflColor * reflColorMod, fresnelTerm.a);
            finalColor.rgb = lerp(finalColor.rgb, _HorizonColor.rgb, 1.0 - fresnelFactor);

            return finalColor;
        }
        ENDCG
    }
}
FallBack "Diffuse"
}