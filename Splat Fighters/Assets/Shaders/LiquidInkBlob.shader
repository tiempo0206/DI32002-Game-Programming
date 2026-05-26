Shader "Splat Fighters/Liquid Ink Blob"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.05, 0.45, 1, 0.75)
        _RimColor ("Rim Color", Color) = (1, 1, 1, 0.55)
        _StartTime ("Start Time", Float) = 0
        _Lifetime ("Lifetime", Float) = 0.7
        _GlossStrength ("Gloss Strength", Range(0, 1)) = 0.45
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "LiquidBlob"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _RimColor;
                half _StartTime;
                half _Lifetime;
                half _GlossStrength;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 centeredUv = input.uv * 2.0 - 1.0;
                half dist = length(centeredUv);
                half age = saturate((_Time.y - _StartTime) / max(_Lifetime, 0.001h));
                half body = smoothstep(1.0h, 0.14h, dist);
                half rim = smoothstep(0.96h, 0.62h, dist) * (1.0h - smoothstep(0.62h, 0.24h, dist));
                half ripple = pow(saturate(sin(dist * 26.0h - _Time.y * 10.0h) * 0.5h + 0.5h), 5.0h);
                half fade = 1.0h - age;

                half3 color = _BaseColor.rgb + (_RimColor.rgb * (rim + ripple * 0.35h) * _GlossStrength);
                half alpha = _BaseColor.a * body * fade;
                return half4(saturate(color), alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Unlit/Transparent"
}
