Shader "Hidden/ARPDiffuseProbeSkybox" {

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

            struct GBufferOutput {
                float4 gbuffer0 : SV_TARGET0;
                float2 gbuffer1 : SV_TARGET1;
                float gbuffer2 : SV_TARGET2;
            };

            VertexOutput SkyboxVertex(VertexInput input) {
                VertexOutput output;
                // float3 rotated = RotateAroundYInDegrees(input.posOS, _GlobalEnvMapRotation);
                float4 posCS = TransformObjectToHClip(input.posOS);
                // posCS.x = -posCS.x;
                output.posCS = posCS;
                output.dir = input.posOS.xyz;
                return output;
            }

            GBufferOutput SkyboxFragment(VertexOutput input) {
                GBufferOutput output;

                float3 normal = -normalize(input.dir);
                float radialDepth = _DiffuseProbeParams0.w;

                float4 gbuffer0 = float4(.0f, .0f, .0f, 1.0f);

                output.gbuffer0 = gbuffer0;
                output.gbuffer1 = EncodeNormalComplex(normal);
                output.gbuffer2 = radialDepth;

                return output;
            }

            ENDHLSL
        }
    }
}