Shader "Advanced Render Pipeline/ARPSimpleSkybox" {
    Properties {
        [Gamma]
        _Exposure("Exposure", Range(0, 8)) = 1.0
        _Rotation("Rotation", Range(0, 360)) = 0
        _MipLevel("MipLevel", Range(0, 11)) = 0
    }

    Subshader {
        
        Pass {
        
            Cull Off
            ZWrite Off
        
            Tags {
                "Queue"="Background"
                "RenderType"="Background"
                "PreviewType"="Skybox"
            }

            HLSLPROGRAM
        
            #pragma vertex SkyboxVertex
            #pragma fragment SkyboxFragment

            #include "ARPCommon.hlsl"

            struct VertexInput {
                float4 posOS : POSITION;
            };

            struct VertexOutput {
                float4 posCS : SV_POSITION;
                float3 texcoord : TEXCOORD0;
            };

            float _Exposure;
            float _Rotation;
            float _MipLevel;

            float3 RotateAroundYInDegrees (float3 vertex, float degrees) {
                float alpha = degrees * PI / 180.0f;
                float sina, cosa;
                sincos(alpha, sina, cosa);
                float2x2 m = float2x2(cosa, -sina, sina, cosa);
                return float3(mul(m, vertex.xz), vertex.y).xzy;
            }

            VertexOutput SkyboxVertex(VertexInput input) {
                VertexOutput output;
                float3 rotated = RotateAroundYInDegrees(input.posOS, _Rotation);
                output.posCS = TransformObjectToHClip(rotated);
                output.texcoord = input.posOS.xyz;
                return output;
            }

            float4 SkyboxFragment(VertexOutput input) : SV_Target {
                float4 skybox = _GlobalEnvMapSpecular.SampleLevel(sampler_GlobalEnvMapSpecular, input.texcoord, _MipLevel);
                skybox *= _Exposure;
                skybox.a = 1.0f;
                return skybox;
            }

            ENDHLSL
        }
    }
}