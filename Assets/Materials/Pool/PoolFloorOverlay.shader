Shader "AliceChaser/PoolFloorOverlay"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Sprite Tint", Color) = (1,1,1,1)

        _BaseColor ("Base Soil", Color) = (0.58, 0.32, 0.27, 1)
        _WarmColor ("Warm Ochre", Color) = (0.68, 0.45, 0.27, 1)
        _RoseColor ("Dusty Rose", Color) = (0.56, 0.28, 0.32, 1)
        _ShadowColor ("Mauve Shadow", Color) = (0.27, 0.18, 0.23, 1)
        _MossColor ("Muted Moss", Color) = (0.34, 0.39, 0.24, 1)
        _ContourColor ("Contour", Color) = (0.20, 0.13, 0.15, 1)

        _WorldScale ("World Pattern Scale", Float) = 1.0
        _PatchScale ("Broad Patch Scale", Float) = 0.75
        _MossAmount ("Moss Amount", Range(0,1)) = 0.16
        _MossSoftness ("Moss Softness", Range(0.01,0.5)) = 0.12
        _ContourStrength ("Contour Strength", Range(0,1)) = 0.25
        _ContourWidth ("Contour Width", Range(0.01,0.25)) = 0.055
        _SurfaceBreakup ("Surface Breakup", Range(0,1)) = 0.28
        _OverlayBlend ("Overlay Blend", Range(0,1)) = 0.92
        _Opacity ("Opacity", Range(0,1)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Universal2D"
            Tags { "LightMode"="Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 positionWS : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _BaseColor;
                float4 _WarmColor;
                float4 _RoseColor;
                float4 _ShadowColor;
                float4 _MossColor;
                float4 _ContourColor;
                float _WorldScale;
                float _PatchScale;
                float _MossAmount;
                float _MossSoftness;
                float _ContourStrength;
                float _ContourWidth;
                float _SurfaceBreakup;
                float _OverlayBlend;
                float _Opacity;
            CBUFFER_END

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1,0));
                float c = hash21(i + float2(0,1));
                float d = hash21(i + float2(1,1));
                return lerp(lerp(a,b,f.x), lerp(c,d,f.x), f.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                v += valueNoise(p) * a; p = p * 2.03 + 17.1; a *= 0.5;
                v += valueNoise(p) * a; p = p * 2.07 + 31.7; a *= 0.5;
                v += valueNoise(p) * a;
                return v;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.uv = IN.uv;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;

                float2 p = IN.positionWS.xy * max(_WorldScale, 0.0001);
                float broad = fbm(p * max(_PatchScale, 0.0001));
                float fine = fbm(p * 2.35 + 9.7);

                float warmMask = smoothstep(0.42, 0.72, broad);
                float roseMask = smoothstep(0.50, 0.78, 1.0 - abs(broad - 0.48) * 1.7);
                float shadowMask = smoothstep(0.12, 0.62, (1.0 - broad) * 0.72 + fine * 0.25);

                float3 soil = lerp(_BaseColor.rgb, _WarmColor.rgb, warmMask * 0.55);
                soil = lerp(soil, _RoseColor.rgb, roseMask * 0.33);
                soil = lerp(soil, _ShadowColor.rgb, shadowMask * _SurfaceBreakup * 0.55);

                // Sparse moss freckles/patches, kept broad enough to read like storybook ground.
                float mossN = fbm(p * 1.45 + float2(12.3, 4.8));
                float mossThreshold = 1.0 - _MossAmount;
                float mossMask = smoothstep(mossThreshold - _MossSoftness, mossThreshold + _MossSoftness, mossN);
                soil = lerp(soil, _MossColor.rgb, mossMask * 0.45);

                // Very subtle, irregular looping contour motif referencing the Caucus Race "sort of circle".
                float2 q = p * 0.42;
                q.x += sin(q.y * 0.85) * 0.22;
                q.y += sin(q.x * 0.65) * 0.14;
                float rings = abs(frac(length(q * float2(1.0, 0.72)) * 0.62) - 0.5);
                float contour = 1.0 - smoothstep(_ContourWidth, _ContourWidth * 2.1, rings);
                contour *= _ContourStrength * (0.45 + 0.55 * broad);
                soil = lerp(soil, _ContourColor.rgb, contour);

                float3 spriteBase = tex.rgb;
                float3 outRgb = lerp(spriteBase, soil, _OverlayBlend);
                half outA = tex.a * _Opacity;
                return half4(outRgb, outA);
            }
            ENDHLSL
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 positionWS : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _BaseColor;
                float4 _WarmColor;
                float4 _RoseColor;
                float4 _ShadowColor;
                float4 _MossColor;
                float4 _ContourColor;
                float _WorldScale;
                float _PatchScale;
                float _MossAmount;
                float _MossSoftness;
                float _ContourStrength;
                float _ContourWidth;
                float _SurfaceBreakup;
                float _OverlayBlend;
                float _Opacity;
            CBUFFER_END

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1,0));
                float c = hash21(i + float2(0,1));
                float d = hash21(i + float2(1,1));
                return lerp(lerp(a,b,f.x), lerp(c,d,f.x), f.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                v += valueNoise(p) * a; p = p * 2.03 + 17.1; a *= 0.5;
                v += valueNoise(p) * a; p = p * 2.07 + 31.7; a *= 0.5;
                v += valueNoise(p) * a;
                return v;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.uv = IN.uv;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;

                float2 p = IN.positionWS.xy * max(_WorldScale, 0.0001);
                float broad = fbm(p * max(_PatchScale, 0.0001));
                float fine = fbm(p * 2.35 + 9.7);

                float warmMask = smoothstep(0.42, 0.72, broad);
                float roseMask = smoothstep(0.50, 0.78, 1.0 - abs(broad - 0.48) * 1.7);
                float shadowMask = smoothstep(0.12, 0.62, (1.0 - broad) * 0.72 + fine * 0.25);

                float3 soil = lerp(_BaseColor.rgb, _WarmColor.rgb, warmMask * 0.55);
                soil = lerp(soil, _RoseColor.rgb, roseMask * 0.33);
                soil = lerp(soil, _ShadowColor.rgb, shadowMask * _SurfaceBreakup * 0.55);

                float mossN = fbm(p * 1.45 + float2(12.3, 4.8));
                float mossThreshold = 1.0 - _MossAmount;
                float mossMask = smoothstep(mossThreshold - _MossSoftness, mossThreshold + _MossSoftness, mossN);
                soil = lerp(soil, _MossColor.rgb, mossMask * 0.45);

                float2 q = p * 0.42;
                q.x += sin(q.y * 0.85) * 0.22;
                q.y += sin(q.x * 0.65) * 0.14;
                float rings = abs(frac(length(q * float2(1.0, 0.72)) * 0.62) - 0.5);
                float contour = 1.0 - smoothstep(_ContourWidth, _ContourWidth * 2.1, rings);
                contour *= _ContourStrength * (0.45 + 0.55 * broad);
                soil = lerp(soil, _ContourColor.rgb, contour);

                float3 outRgb = lerp(tex.rgb, soil, _OverlayBlend);
                return half4(outRgb, tex.a * _Opacity);
            }
            ENDHLSL
        }
    }
}
