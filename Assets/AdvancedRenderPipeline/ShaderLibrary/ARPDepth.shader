Shader "Hidden/ARPDepth" {
    
    Properties {
        /*
        _StencilRef("Stencil Ref", int) = 1
        _Cull("Cull", Float) = 0
        */
    }
    
    SubShader {
        
        Pass {
            
            Name "StaticDepth"
            
            Tags {
                "LightMode" = "Depth"
            }
            
            Stencil {
                Ref 0
                WriteMask 3
                Comp Always
                Pass Replace
            }
            
            ZTest LEqual
            ZWrite On
			Cull [_Cull]
            
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

            void DepthFragment() { }
            
            ENDHLSL
        }
        
        Pass {
            
            Name "DynamicDepth"
            
            Tags {
                "LightMode" = "Depth"
            }
            
            Stencil {
                Ref [_StencilRef]
                WriteMask 3
                Comp Always
                Pass Replace
            }
            
            ZTest LEqual
            ZWrite On
			Cull [_Cull]
            
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

            void DepthFragment() { }
            
            ENDHLSL
        }
        
        Pass {
            
            Name "MotionVectors"
            
            Tags {
                "LightMode" = "MotionVectors"
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

            DepthMVVertexOutput MVVertex(DepthMVVertexInput input) {
                DepthMVVertexOutput output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.posCS = TransformObjectToHClip(input.posOS);
                float4 mv_posCS = mul(UNITY_MATRIX_UNJITTERED_VP, mul(GetObjectToWorldMatrix(), float4(input.posOS, 1.0)));
                mv_posCS.z = .0f;
                float4 mv_prevPosCS = mul(UNITY_MATRIX_UNJITTERED_VP, mul(GetPrevObjectToWorldMatrix(), float4(input.prevPosOS, 1.0)));
                mv_prevPosCS.z = .0f;
                output.mv_posCS = mv_posCS;
                output.mv_prevPosCS = mv_prevPosCS;
                return output;
            }

            float2 MVFragment(DepthMVVertexOutput input) : SV_TARGET {
                UNITY_SETUP_INSTANCE_ID(input);
                float2 mv = EncodeMotionVector(CalculateMotionVector(input.posCS, input.mv_prevPosCS));
                // return(input.mv_prevPosCS.z);

                mv = float2(.5f, .5f);
                return mv;
            }
            
            ENDHLSL
        }
    }
}