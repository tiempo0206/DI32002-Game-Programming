Shader "PBRMaskTint"
{
    Properties
    {
        _Albedo("Albedo", 2D) = "white" {}
        _Mask("Mask", 2D) = "white" {}
        _Normal("Normal", 2D) = "bump" {}
        _SAM("SAM", 2D) = "white" {}
        _Color01("Color01", Color) = (0, 0.1394524, 0.8088235, 1)
        _Color02("Color02", Color) = (0.4557808, 0, 0.6176471, 1)
        _Color03("Color03", Color) = (0.4557808, 0, 0.6176471, 1)
        _Color01Power("Color01Power", Range(0, 2)) = 1
        _Color02Power("Color02Power", Range(0, 4)) = 2
        _Color03Power("Color03Power", Range(0, 2)) = 1
        _Brightness("Brightness", Range(0, 4)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_Albedo);
            SAMPLER(sampler_Albedo);
            TEXTURE2D(_Mask);
            SAMPLER(sampler_Mask);
            TEXTURE2D(_Normal);
            SAMPLER(sampler_Normal);
            TEXTURE2D(_SAM);
            SAMPLER(sampler_SAM);

            CBUFFER_START(UnityPerMaterial)
                float4 _Albedo_ST;
                float4 _Mask_ST;
                float4 _Normal_ST;
                float4 _SAM_ST;
                half4 _Color01;
                half4 _Color02;
                half4 _Color03;
                half _Color01Power;
                half _Color02Power;
                half _Color03Power;
                half _Brightness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half3 tangentWS : TEXCOORD2;
                half3 bitangentWS : TEXCOORD3;
                float2 uv : TEXCOORD4;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = normalInputs.tangentWS;
                output.bitangentWS = normalInputs.bitangentWS;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 albedoUv = TRANSFORM_TEX(input.uv, _Albedo);
                float2 maskUv = TRANSFORM_TEX(input.uv, _Mask);
                float2 normalUv = TRANSFORM_TEX(input.uv, _Normal);
                float2 samUv = TRANSFORM_TEX(input.uv, _SAM);

                half4 baseSample = SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, albedoUv);
                half3 mask = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, maskUv).rgb;
                half3 sam = SAMPLE_TEXTURE2D(_SAM, sampler_SAM, samUv).rgb;

                half3 tint =
                    min(mask.r, _Color01.rgb) * _Color01Power +
                    min(mask.g, _Color02.rgb) * _Color02Power +
                    min(mask.b, _Color03.rgb) * _Color03Power;

                half tintAmount = saturate(mask.r + mask.g + mask.b);
                half3 albedo = lerp(baseSample.rgb, saturate(baseSample.rgb * tint) * _Brightness, tintAmount);

                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_Normal, sampler_Normal, normalUv));
                half3 normalWS = TransformTangentToWorld(
                    normalTS,
                    half3x3(normalize(input.tangentWS), normalize(input.bitangentWS), normalize(input.normalWS)));
                normalWS = NormalizeNormalPerPixel(normalWS);

                half3 viewDirectionWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half ndotl = saturate(dot(normalWS, mainLight.direction));

                half smoothness = saturate(sam.r);
                half occlusion = lerp(0.55h, 1.0h, saturate(sam.g));
                half metallic = saturate(sam.b);
                half3 ambient = SampleSH(normalWS) * occlusion;
                half3 direct = mainLight.color * ndotl * mainLight.shadowAttenuation;

                half3 halfDirection = SafeNormalize(mainLight.direction + viewDirectionWS);
                half specularPower = exp2(5.0h + smoothness * 8.0h);
                half specular = pow(saturate(dot(normalWS, halfDirection)), specularPower) * smoothness;
                half3 specularColor = lerp(half3(0.04h, 0.04h, 0.04h), albedo, metallic) * specular * mainLight.color;

                half3 color = albedo * (ambient + direct) + specularColor;
                return half4(color, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _MainLightPosition.xyz));
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Lit"
}
