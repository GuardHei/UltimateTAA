Shader "Hidden/ARPDepthStencilMV" {
    
    SubShader {
        
        Pass {
            
            Name "StaticDepthStencil"
            
            Tags {
                "LightMode" = "DepthStencil"
            }
            
            ZTest LEqual
            ZWrite On
			Cull Back
            
            HLSLPROGRAM

            #pragma multi_compile_instancing
            #pragma vertex DepthVertex
            #pragma fragment DepthFragment
            
            #include "ARPCommon.hlsl"

            struct BasicVertexInput {
                float3 posOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct BasicVertexOutput {
                float4 posCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            BasicVertexOutput DepthVertex(BasicVertexInput input) {
                BasicVertexOutput output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                float3 posWS = TransformObjectToWorld(input.posOS);
                output.posCS = TransformObjectToHClip(posWS);
                return output;
            }

            void DepthFragment(BasicVertexOutput input) {
                UNITY_SETUP_INSTANCE_ID(input);
            }
            
            ENDHLSL
        }
        
        Pass {
            
            Name "DefaultDynamicDepthStencil"
            
            Tags {
                "LightMode" = "DepthStencil"
            }
        
            Colormask 0
            
            ZTest LEqual
            ZWrite On
			Cull Back
            
            HLSLPROGRAM

            #pragma multi_compile_instancing
            #pragma vertex DepthVertex
            #pragma fragment DepthFragment
            
            #include "ARPCommon.hlsl"

            struct DepthMVVertexInput {
                float3 posOS : POSITION;
                float3 prevPosOS : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthMVVertexOutput {
                float4 posCS : SV_POSITION;
                float4 prevPosCS : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            bool _HasLastPositionData;

            DepthMVVertexOutput DepthVertex(DepthMVVertexInput input) {
                DepthMVVertexOutput output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                float3 posWS = TransformObjectToWorld(input.posOS);
                output.posCS = TransformObjectToHClip(posWS);
                output.prevPosCS = output.posCS;
                return output;
            }

            float4 DepthFragment(DepthMVVertexOutput input) : SV_TARGET {
                UNITY_SETUP_INSTANCE_ID(input);
                return 1.0;
            }
            
            ENDHLSL
        }
    }
}