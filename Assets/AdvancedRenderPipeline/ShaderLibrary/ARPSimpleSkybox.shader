Shader "Advanced Render Pipeline/ARPSimpleSkybox" {

    Subshader {
        
        Pass {
        
            Cull Off
            ZWrite Off
        
            Tags {
                "Queue" = "Background"
                "RenderType" = "Background"
                "PreviewType" = "Skybox"
            }

            HLSLPROGRAM
        
            #pragma vertex SkyboxVertex
            #pragma fragment SkyboxFragment

            #include "ARPCommon.hlsl"

            struct VertexInput {
                float3 posOS : POSITION;
            };

            struct VertexOutput {
                float4 posCS : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            VertexOutput SkyboxVertex(VertexInput input) {
                VertexOutput output;
                // float3 rotated = RotateAroundYInDegrees(input.posOS, _GlobalEnvMapRotation);
                output.posCS = TransformObjectToHClip(input.posOS);
                output.dir = input.posOS.xyz;
                return output;
            }

            float4 SkyboxFragment(VertexOutput input) : SV_Target {
                /*
                float4 skybox = _GlobalEnvMapSpecular.SampleLevel(sampler_GlobalEnvMapSpecular, input.dir, _SkyboxMipLevel);
                skybox *= _GlobalEnvMapExposure;
                */

                float3 dir = normalize(input.dir);
                float3 skybox = SampleGlobalEnvMapSpecular(dir, _SkyboxMipLevel);
                return float4(skybox, 1.0f);
            }

            ENDHLSL
        }
    }
}