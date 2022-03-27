Shader "Hidden/ARPDiffuseProbeGBuffer" {
    
    Properties {

    }
    
    SubShader {

        Pass {
            
            Name "DiffuseProbeGbuffer"
            
            Tags {
                "LightMode" = "DiffuseProbeGBuffer"
            }
            
            Stencil {
                Ref 4
                WriteMask 4
                Comp Always
                Pass Replace
            }
            
            ZTest Equal
            ZWrite Off
			Cull [_Cull]
            
            HLSLPROGRAM

            #pragma multi_compile_instancing
            #pragma vertex MVVertex
            #pragma fragment MVFragment
            
            #include "ARPCommon.hlsl"

            struct VertexInput {
                float3 posOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VertexOutput {
                float4 posCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            VertexOutput MVVertex(VertexInput input) {
                VertexOutput output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                float4 posWS = mul(GetObjectToWorldMatrix(), float4(input.posOS, 1.0f));
                output.posCS = mul(GetWorldToHClipMatrix(), posWS);
                return output;
            }

            float3 MVFragment(VertexOutput input) : SV_TARGET {
                UNITY_SETUP_INSTANCE_ID(input);
                return .0f;
            }
            
            ENDHLSL
        }
    }
}