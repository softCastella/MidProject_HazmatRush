Shader "MidProject/UI/ImageOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _OutlineColor ("Outline Color", Color) = (0.55, 0.92, 1, 1)
        _OutlineWidth ("Outline Width (px)", Range(0.05, 4)) = 0.35
        _GlowSize ("Glow Size (px)", Range(0, 16)) = 2
        _GlowIntensity ("Glow Intensity", Range(0, 1)) = 0.15
        _GlowFalloff ("Glow Falloff", Range(0.5, 4)) = 1.5
        _GlowMinBrightness ("Dark Glow Lift", Range(0, 0.5)) = 0.12
        _AlphaThreshold ("Alpha Threshold", Range(0, 1)) = 0.01

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex UiOutlineVertex
            #pragma fragment UiOutlineFragment
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            float4 _ClipRect;

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

            #define GLOW_RINGS 12
            #define GLOW_ANGLES 24

            Varyings UiOutlineVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.worldPosition = input.positionOS;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            half UnityGet2DClipping(float2 position, float4 clipRect)
            {
                half2 inside = step(clipRect.xy, position.xy) * step(position.xy, clipRect.zw);
                return inside.x * inside.y;
            }

            half4 Premultiply(half4 color)
            {
                color.rgb *= color.a;
                return color;
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

            half4 UiOutlineFragment(Varyings input) : SV_Target
            {
                half4 spriteColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                half centerAlpha = spriteColor.a;
                half4 result;

                if (centerAlpha >= _AlphaThreshold)
                {
                    result = spriteColor;
                }
                else
                {
                    float2 texel = _MainTex_TexelSize.xy;
                    float2 outlineOffset = texel * _OutlineWidth;
                    half outlineAlpha = SampleRingMaxAlpha(input.uv, outlineOffset);
                    half rim = (outlineAlpha >= _AlphaThreshold) ? saturate(_OutlineWidth * 0.18) : 0;

                    half glow = _GlowSize > 0.01 ? ComputeSoftGlow(input.uv, texel) : 0;

                    half finalAlpha = saturate(glow + rim);
                    if (finalAlpha <= 0.001)
                        return half4(0, 0, 0, 0);

                    result.rgb = GetGlowRgb();
                    result.a = finalAlpha * _OutlineColor.a;
                }

                #ifdef UNITY_UI_CLIP_RECT
                result.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - 0.001);
                #endif

                return Premultiply(result);
            }
            ENDHLSL
        }
    }

    Fallback "UI/Default"
}
