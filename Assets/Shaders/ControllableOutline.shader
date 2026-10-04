Shader "HouseholdObjectSimulator/Controllable Outline"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (1, 0.75, 0, 1)
        _OutlineWidth("Outline Width (Pixels)", Range(0.5, 10)) = 4
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "UniversalForward" }
            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);

                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float2 outlineDirection = mul((float3x3)UNITY_MATRIX_V, normalWS).xy;
                outlineDirection /= max(length(outlineDirection), 0.0001);

                // Convert the desired pixel width to clip-space, preserving the
                // apparent outline thickness regardless of camera distance.
                float2 clipSpaceOffset = outlineDirection * _OutlineWidth * 2.0 /
                    _ScreenParams.xy * output.positionHCS.w;
                output.positionHCS.xy += clipSpaceOffset;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}
