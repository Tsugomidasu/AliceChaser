Shader "AliceChaser/RabbitHoleFloorWonderland"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _TileRed ("Top Tile Red", Color) = (0.43, 0.055, 0.065, 1)
        _TileRedDark ("Top Tile Dark Red", Color) = (0.21, 0.020, 0.033, 1)
        _TileOchre ("Top Tile Ochre", Color) = (0.55, 0.315, 0.075, 1)
        _TileCream ("Top Tile Cream", Color) = (0.57, 0.45, 0.29, 1)
        _TopSeam ("Top Tile Seam", Color) = (0.055, 0.020, 0.024, 1)
        _TrimDark ("Carved Trim Dark", Color) = (0.075, 0.020, 0.028, 1)
        _TrimBrass ("Muted Brass", Color) = (0.50, 0.29, 0.075, 1)

        _UnderDark ("Underside Dark", Color) = (0.030, 0.018, 0.026, 1)
        _UnderMid ("Underside Burgundy", Color) = (0.105, 0.030, 0.040, 1)
        _UnderLight ("Underside Highlight", Color) = (0.165, 0.050, 0.055, 1)
        _UnderSeam ("Underside Seam", Color) = (0.012, 0.010, 0.015, 1)

        _WorldTiling ("World Tiling", Float) = 1.0
        _TopBandStart ("Top Surface Start", Range(0.55,0.95)) = 0.78
        _DiamondScaleX ("Top Diamond Width", Float) = 1.15
        _DiamondScaleY ("Top Diamond Height", Float) = 0.48
        _TopSeamWidth ("Top Seam Width", Range(0.005,0.10)) = 0.025
        _TopWarp ("Top Pattern Irregularity", Range(0,0.15)) = 0.035
        _TrimWidth ("Lower Trim Width", Range(0.006,0.08)) = 0.026
        _CreamChance ("Cream Accent Amount", Range(0,0.5)) = 0.12

        _BlockWidth ("Underside Block Width", Float) = 1.75
        _BlockHeight ("Underside Block Height", Float) = 0.58
        _BlockSeamWidth ("Underside Seam Width", Range(0.005,0.12)) = 0.030
        _UnderVariation ("Underside Variation", Range(0,0.35)) = 0.16
        _UnderWarp ("Underside Irregularity", Range(0,0.12)) = 0.022

        _Opacity ("Opacity", Range(0,1)) = 1
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
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _TileRed;
                float4 _TileRedDark;
                float4 _TileOchre;
                float4 _TileCream;
                float4 _TopSeam;
                float4 _TrimDark;
                float4 _TrimBrass;
                float4 _UnderDark;
                float4 _UnderMid;
                float4 _UnderLight;
                float4 _UnderSeam;
                float _WorldTiling;
                float _TopBandStart;
                float _DiamondScaleX;
                float _DiamondScaleY;
                float _TopSeamWidth;
                float _TopWarp;
                float _TrimWidth;
                float _CreamChance;
                float _BlockWidth;
                float _BlockHeight;
                float _BlockSeamWidth;
                float _UnderVariation;
                float _UnderWarp;
                float _Opacity;
            CBUFFER_END

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
                float3 positionWS : TEXCOORD1;
                float4 color : COLOR;
            };

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float alpha = sprite.a * IN.color.a * _Color.a * _Opacity;

                float2 wp = IN.positionWS.xy * max(_WorldTiling, 0.0001);

                // --------------------
                // DARK STRUCTURAL BODY
                // --------------------
                float bw = max(_BlockWidth, 0.05);
                float bh = max(_BlockHeight, 0.05);
                float bodyX = wp.x + sin(wp.y * 1.65) * _UnderWarp;
                float row = floor(wp.y / bh);
                float stagger = fmod(abs(row), 2.0) * 0.5;
                float2 blockUV = float2(frac(bodyX / bw + stagger), frac(wp.y / bh));
                float edgeX = min(blockUV.x, 1.0 - blockUV.x);
                float edgeY = min(blockUV.y, 1.0 - blockUV.y);
                float seamX = 1.0 - smoothstep(_BlockSeamWidth, _BlockSeamWidth * 2.2, edgeX);
                float seamY = 1.0 - smoothstep(_BlockSeamWidth, _BlockSeamWidth * 2.2, edgeY);
                float bodySeam = saturate(max(seamX, seamY));

                float blockNoise = hash21(float2(floor(bodyX / bw + stagger), row));
                float3 underside = lerp(_UnderDark.rgb, _UnderMid.rgb, 0.38 + blockNoise * _UnderVariation);
                float horizontalWear = (sin(bodyX * 3.4 + wp.y * 0.7) * 0.5 + 0.5) * 0.08;
                underside = lerp(underside, _UnderLight.rgb, horizontalWear);
                underside = lerp(underside, _UnderSeam.rgb, bodySeam * 0.92);

                // --------------------
                // WONDERLAND TOP SURFACE
                // --------------------
                float capMask = smoothstep(_TopBandStart, _TopBandStart + 0.010, IN.uv.y);
                float capLocalY = saturate((IN.uv.y - _TopBandStart) / max(1.0 - _TopBandStart, 0.001));

                float topX = wp.x + sin(wp.x * 0.61 + wp.y * 2.1) * _TopWarp;
                float sx = max(_DiamondScaleX, 0.05);
                float sy = max(_DiamondScaleY, 0.05);

                // 45-degree transformed world coordinates create broad diamond/checker cells.
                float2 diamondP = float2((topX / sx) + (wp.y / sy), (topX / sx) - (wp.y / sy));
                float2 diamondCell = floor(diamondP);
                float2 diamondUV = frac(diamondP);
                float parity = fmod(abs(diamondCell.x + diamondCell.y), 2.0);

                float3 tileA = lerp(_TileRedDark.rgb, _TileRed.rgb, 0.72);
                float3 tileB = _TileOchre.rgb;
                float3 topColor = lerp(tileA, tileB, parity);

                // Sparse cream tiles keep the palette storybook-like without becoming bright checkerboard everywhere.
                float tileNoise = hash21(diamondCell);
                float creamMask = step(1.0 - _CreamChance, tileNoise);
                topColor = lerp(topColor, _TileCream.rgb, creamMask * 0.72);

                float dEdgeX = min(diamondUV.x, 1.0 - diamondUV.x);
                float dEdgeY = min(diamondUV.y, 1.0 - diamondUV.y);
                float diamondSeamX = 1.0 - smoothstep(_TopSeamWidth, _TopSeamWidth * 2.2, dEdgeX);
                float diamondSeamY = 1.0 - smoothstep(_TopSeamWidth, _TopSeamWidth * 2.2, dEdgeY);
                float diamondSeam = saturate(max(diamondSeamX, diamondSeamY));
                topColor = lerp(topColor, _TopSeam.rgb, diamondSeam * 0.78);

                // Broad cel-like highlight at the actual top edge. The silhouette stays perfectly horizontal.
                float topHighlight = smoothstep(0.78, 1.0, capLocalY);
                topColor *= 0.92 + topHighlight * 0.12;

                // --------------------
                // CARVED DIVIDER TRIM
                // --------------------
                float dividerDistance = abs(IN.uv.y - _TopBandStart);
                float trimOuter = 1.0 - smoothstep(_TrimWidth, _TrimWidth * 1.9, dividerDistance);
                float trimInner = 1.0 - smoothstep(_TrimWidth * 0.30, _TrimWidth * 0.65, dividerDistance);
                float3 dividerColor = lerp(_TrimDark.rgb, _TrimBrass.rgb, trimInner * 0.72);

                float3 finalColor = lerp(underside, topColor, capMask);
                finalColor = lerp(finalColor, dividerColor, trimOuter * (1.0 - capMask * 0.15));
                finalColor *= IN.color.rgb * _Color.rgb;

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
}
