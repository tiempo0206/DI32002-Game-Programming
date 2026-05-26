Shader "Splat Fighters/Ink Liquid Overlay"
{
    Properties
    {
        [MainTexture] _MainTex ("Paint Mask", 2D) = "white" {}
        _LiquidAlpha ("Liquid Alpha", Range(0, 1)) = 0.96
        _GlossStrength ("Gloss Strength", Range(0, 1)) = 0.3
        _FlowSpeed ("Flow Speed", Float) = 0.4
        _RippleScale ("Ripple Scale", Float) = 42
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
            Name "LiquidOverlay"
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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half _LiquidAlpha;
                half _GlossStrength;
                half _FlowSpeed;
                half _RippleScale;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 paint = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half alpha = paint.a * _LiquidAlpha;
                clip(alpha - 0.01h);

                float t = _Time.y * _FlowSpeed;
                half waveA = sin((input.uv.x * 1.7h + input.uv.y * 1.13h + t) * _RippleScale);
                half waveB = sin((input.uv.x * -0.9h + input.uv.y * 2.4h + t * 1.37h) * (_RippleScale * 0.55h));
                half ripple = (waveA + waveB) * 0.5h;
                half wetShade = 0.86h + ripple * 0.08h;
                half glossMask = pow(saturate(ripple * 0.5h + 0.5h), 6.0h) * _GlossStrength;

                half3 color = paint.rgb * wetShade + glossMask.xxx;
                return half4(saturate(color), alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Unlit/Transparent"
}
