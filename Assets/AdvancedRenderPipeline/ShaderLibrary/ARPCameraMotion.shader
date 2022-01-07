Shader "Hidden/ARPCameraMotion" {
    
    Properties {
        _MainTex("Texture", 2D) = "white" { }
    }
    
    SubShader {
        
        Pass {
            
            Name "CameraMotionVectors"
            
            Stencil {
                Ref 4
                ReadMask 4
                Comp NotEqual
                Pass Keep
            }
            
            Cull Off
            ZWrite Off
            ZTest Always
            
            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Fragment

            #include "ARPCommon.hlsl"

            struct VertexOutput {
                float4 posCS : SV_POSITION;
                float2 screenUV : VAR_SCREEN_UV;
                float3 ray : TEXCOORD0;
            };

            VertexOutput Vert(uint vertexID : SV_VertexID) {
                VertexOutput output;
                output.posCS = VertexIDToPosCS(vertexID);
                output.screenUV = VertexIDToScreenUV(vertexID);
                output.ray = VertexIDToFrustumCorners(vertexID).xyz;
                return output;
            }

            float2 Fragment(VertexOutput input) : SV_TARGET {
                float2 uv = input.screenUV;
                if (_ProjectionParams.x < .0f) uv.y = 1.0f - uv.y;
                
                float depth = SampleDepth(uv);
                float4 posWS = DepthToWorldPosFast(depth, input.ray);

                float4 posCS = mul(UNITY_MATRIX_UNJITTERED_VP, posWS);
                float4 prevPosCS = mul(UNITY_PREV_MATRIX_VP, posWS);

                float2 mv = EncodeMotionVector(CalculateMotionVector(posCS, prevPosCS));
                
                // mv = float2(.0f, 1.0f);

                /*
                float2 posH = posCS.xy / posCS.w;
                float2 prePosH = prevPosCS.xy / prevPosCS.w;
                
                float2 posV = (posH.xy + 1.0f) / 2.0f;
                float2 prevPosV = (prePosH.xy + 1.0f) / 2.0f;

                #if UNITY_UV_STARTS_AT_TOP
                posV.y = 1.0f - posV.y;
                prevPosV.y = 1.0f - prevPosV.y;
                #endif

                float2 mv = posV - prevPosV;
                */
                
                return mv;
            }
            
            ENDHLSL
        }
    }
}