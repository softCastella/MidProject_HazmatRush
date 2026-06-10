Shader "MidProject/2D/SpriteOutline"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _OutlineColor ("Outline Color", Color) = (1, 0.75, 0.1, 1)
        _OutlineWidth ("Outline Width (px)", Range(1, 4)) = 2
        _GlowSize ("Glow Size (px)", Range(2, 120)) = 61
        _GlowIntensity ("Glow Intensity", Range(0, 1)) = 0.32
        _GlowFalloff ("Glow Falloff", Range(0.5, 4)) = 1.87
        _GlowMinBrightness ("Dark Glow Lift", Range(0, 0.5)) = 0.18
        _AlphaThreshold ("Alpha Threshold", Range(0, 1)) = 0.01
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

            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment
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
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _OutlineColor;
                half _OutlineWidth;
                half _GlowSize;
                half _GlowIntensity;
                half _GlowFalloff;
                half _GlowMinBrightness;
                half _AlphaThreshold;
            CBUFFER_END

            #define GLOW_RINGS 24
            #define GLOW_ANGLES 32

            Varyings OutlineVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings output = CommonUnlitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }

            half SampleSpriteAlpha(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }

            half SampleRingMaxAlpha(float2 uv, float2 offset)
            {
                half maxAlpha = 0;
                maxAlpha = max(maxAlpha, SampleSpriteAlpha(uv + float2(offset.x, 0)));
                maxAlpha = max(maxAlpha, SampleSpriteAlpha(uv + float2(-offset.x, 0)));
                maxAlpha = max(maxAlpha, SampleSpriteAlpha(uv + float2(0, offset.y)));
                maxAlpha = max(maxAlpha, SampleSpriteAlpha(uv + float2(0, -offset.y)));
                maxAlpha = max(maxAlpha, SampleSpriteAlpha(uv + float2(offset.x, offset.y)));
                maxAlpha = max(maxAlpha, SampleSpriteAlpha(uv + float2(-offset.x, offset.y)));
                maxAlpha = max(maxAlpha, SampleSpriteAlpha(uv + float2(offset.x, -offset.y)));
                maxAlpha = max(maxAlpha, SampleSpriteAlpha(uv + float2(-offset.x, -offset.y)));
                return maxAlpha;
            }

            half ComputeSoftGlow(float2 uv, float2 texel)
            {
                half minDist = _GlowSize + 1.0;
                const half tau = 6.2831853;
                const half angleStep = tau / GLOW_ANGLES;

                [loop]
                for (int ring = 1; ring <= GLOW_RINGS; ring++)
                {
                    half dist = (half)ring / GLOW_RINGS * _GlowSize;

                    [loop]
                    for (int a = 0; a < GLOW_ANGLES; a++)
                    {
                        half ang = ((half)a + (half)(ring & 1) * 0.5) * angleStep;
                        float2 offset = float2(cos(ang), sin(ang)) * dist;
                        if (SampleSpriteAlpha(uv + offset * texel) >= _AlphaThreshold)
                            minDist = min(minDist, dist);
                    }
                }

                if (minDist > _GlowSize)
                    return 0;

                half t = 1.0 - minDist / (_GlowSize + 1.0);
                return pow(t, _GlowFalloff) * _GlowIntensity;
            }

            half3 GetGlowRgb()
            {
                half3 rgb = _OutlineColor.rgb;
                half lum = max(max(rgb.r, rgb.g), rgb.b);
                half lift = 1.0 - saturate(lum / max(_GlowMinBrightness, 0.001));
                return rgb + lift * _GlowMinBrightness;
            }

            half4 OutlineFragment(Varyings input) : SV_Target
            {
                half4 spriteColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                half centerAlpha = spriteColor.a;

                if (centerAlpha >= _AlphaThreshold)
                    return spriteColor;

                float2 texel = _MainTex_TexelSize.xy;
                half glow = ComputeSoftGlow(input.uv, texel);

                float2 outlineOffset = texel * _OutlineWidth;
                half outlineAlpha = SampleRingMaxAlpha(input.uv, outlineOffset);
                half rim = (outlineAlpha >= _AlphaThreshold) ? 0.35 : 0;

                half finalAlpha = saturate(glow + rim);
                if (finalAlpha <= 0.001)
                    return half4(0, 0, 0, 0);

                half4 result;
                result.rgb = GetGlowRgb();
                result.a = finalAlpha * _OutlineColor.a;
                return result;
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/2D/Sprite-Unlit-Default"
}
