Shader "MidProject/2D/PollutantB-OilGlow"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _OilRimColor ("Oil Rim (near)", Color) = (0.18, 0.14, 0.11, 0.9)
        _OilSheenColor ("Oil Sheen (outer)", Color) = (0.52, 0.44, 0.32, 0.55)
        _GlowSize ("Glow Size (px)", Range(2, 120)) = 61
        _GlowIntensity ("Glow Intensity", Range(0, 1)) = 0.38
        _GlowFalloff ("Glow Falloff", Range(0.5, 4)) = 1.6
        _AlphaThreshold ("Alpha Threshold", Range(0, 1)) = 0.01
        _PixelsPerUnit ("Pixels Per Unit", Float) = 1
        _SpriteHalfSize ("Sprite Half Size (local)", Vector) = (64, 30, 0, 0)
        _ExpandCenterOS ("Expand Center (local)", Vector) = (64, 30, 0, 0)
        [MaterialToggle] _ZWrite ("ZWrite", Float) = 0

        [HideInInspector] _RendererColor ("RendererColor", Color) = (1, 1, 1, 1)
        [HideInInspector] PixelSnap ("Pixel snap", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex OilVertex
            #pragma fragment OilFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
                float2 localPosOS : TEXCOORD1;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _OilRimColor;
                half4 _OilSheenColor;
                half _GlowSize;
                half _GlowIntensity;
                half _GlowFalloff;
                half _AlphaThreshold;
                half _PixelsPerUnit;
                half4 _SpriteHalfSize;
                half4 _ExpandCenterOS;
            CBUFFER_END

            #define OIL_GLOW_RINGS 24
            #define OIL_GLOW_ANGLES 32

            float2 GetSpriteSizeOS()
            {
                return _SpriteHalfSize.xy * 2.0;
            }

            float2 LocalPosToSpriteUV(float2 localPos)
            {
                float2 size = max(GetSpriteSizeOS(), float2(0.001, 0.001));
                float2 pivotOS = _ExpandCenterOS.xy - _SpriteHalfSize.xy;
                return (localPos - pivotOS) / size;
            }

            Varyings OilVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                half expandUnits = _GlowSize / max(_PixelsPerUnit, 0.001);
                float2 center = _ExpandCenterOS.xy;
                float2 fromCenter = input.positionOS.xy - center;
                half len = length(fromCenter);
                if (len > 0.0001)
                    input.positionOS.xy = center + fromCenter * ((len + expandUnits) / len);

                Varyings output = CommonUnlitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                output.localPosOS = input.positionOS.xy;
                return output;
            }

            half SampleSpriteAlpha(float2 uv)
            {
                if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
                    return 0;
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }

            half FindNearestEdgeDist(float2 spriteUV, float2 texel)
            {
                half minDist = _GlowSize + 1.0;
                const half tau = 6.2831853;
                const half angleStep = tau / OIL_GLOW_ANGLES;

                [loop]
                for (int ring = 1; ring <= OIL_GLOW_RINGS; ring++)
                {
                    half dist = (half)ring / OIL_GLOW_RINGS * _GlowSize;

                    [loop]
                    for (int a = 0; a < OIL_GLOW_ANGLES; a++)
                    {
                        half ang = ((half)a + (half)(ring & 1) * 0.5) * angleStep;
                        float2 offset = float2(cos(ang), sin(ang)) * dist;
                        if (SampleSpriteAlpha(spriteUV + offset * texel) >= _AlphaThreshold)
                            minDist = min(minDist, dist);
                    }
                }

                return minDist;
            }

            half4 OilFragment(Varyings input) : SV_Target
            {
                float2 spriteUV = LocalPosToSpriteUV(input.localPosOS);
                half4 spriteColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                half shapeAlpha = SampleSpriteAlpha(spriteUV);

                if (shapeAlpha >= _AlphaThreshold)
                    return spriteColor;

                float2 texel = _MainTex_TexelSize.xy;
                half minDist = FindNearestEdgeDist(spriteUV, texel);

                if (minDist > _GlowSize)
                    return half4(0, 0, 0, 0);

                half distNorm = minDist / max(_GlowSize, 0.001);
                half fade = 1.0 - distNorm;
                half strength = pow(fade, _GlowFalloff) * _GlowIntensity;

                half3 glowRgb = lerp(_OilRimColor.rgb, _OilSheenColor.rgb, distNorm);
                half glowAlpha = strength * lerp(_OilRimColor.a, _OilSheenColor.a, distNorm);

                if (glowAlpha <= 0.001)
                    return half4(0, 0, 0, 0);

                return half4(glowRgb, glowAlpha);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/2D/Sprite-Unlit-Default"
}
