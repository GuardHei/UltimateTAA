Shader "Hidden/ARPDepthStencilMV" {
    
    Properties {
        _StencilRef("Stencil Ref", int) = 1
    }
    
    SubShader {
        
        Pass {
            
            Name "StaticDepthStencil"
            
            Tags {
                "LightMode" = "DepthStencil"
            }
            
            Stencil {
                Ref 0
                WriteMask 3
                Comp Always
                Pass Replace
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
                output.posCS = TransformObjectToHClip(input.posOS);
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
            
            Stencil {
                Ref [_StencilRef]
                WriteMask 3
                Comp Always
                Pass Replace
            }
            
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
                float4 mv_posCS : TEXCOORD0;
                float4 mv_prevPosCS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            DepthMVVertexOutput DepthVertex(DepthMVVertexInput input) {
                DepthMVVertexOutput output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.posCS = TransformObjectToHClip(input.posOS);
                output.mv_posCS = mul(UNITY_MATRIX_UNJITTERED_VP, mul(GetObjectToWorldMatrix(), float4(input.posOS, 1.0)));
                output.mv_prevPosCS = mul(UNITY_MATRIX_UNJITTERED_VP, mul(GetPrevObjectToWorldMatrix(), float4(input.prevPosOS, 1.0)));
                return output;
            }

            float2 DepthFragment(DepthMVVertexOutput input) : SV_TARGET {
                UNITY_SETUP_INSTANCE_ID(input);
                float2 mv = EncodeMotionVector(CalculateMotionVector(input.posCS, input.mv_prevPosCS));
                return mv;
            }
            
            ENDHLSL
        }
    }
}