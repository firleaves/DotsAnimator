Shader "DotsAnimator/GpuAnimationUnlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _AnimationTex ("Animation Texture", 2D) = "white" {}
        _AnimationInfo ("Animation Info", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/DotsAnimator/GpuAnimation/Shader/GpuAnimation.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv :TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv: TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _AnimationInfo;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_AnimationTex);
            SAMPLER(sampler_AnimationTex);

            Varyings vert(Attributes input, uint vertexId : SV_VertexID)
            {
                Varyings output = (Varyings)0;
                output.positionCS = TransformObjectToHClip(SampleGpuAnimationPosition(_AnimationTex, vertexId, _AnimationInfo));
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 frag(Varyings input) : SV_TARGET
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
            }
            ENDHLSL
        }
    }
}