Shader "Hidden/ARPBlit" {
    Properties {
        // _MainTex("Texture", 2D) = "white" { }
    }
    
    SubShader {
        
        Pass {
            
            Name "Blit"
            
            Cull Off
            ZWrite Off
            ZTest Always
            
            HLSLPROGRAM

            #pragma vertex BlitVert
            #pragma fragment BlitFragment

            #include "ARPCommon.hlsl"

            struct VertexOutput {
                float4 posCS : SV_POSITION;
                float2 screenUV : VAR_SCREEN_UV;
            };

            VertexOutput BlitVert(uint vertexID : SV_VertexID) {
                VertexOutput output;
                output.posCS = VertexIDToPosCS(vertexID);
                output.screenUV = VertexIDToScreenUV(vertexID);
                return output;
            }

            float4 BlitFragment(VertexOutput input) : SV_TARGET {
                float2 uv = input.screenUV;
                if (_ProjectionParams.x < 0.0) uv.y = 1 - uv.y;
                float4 output = SAMPLE_TEXTURE2D(_MainTex, sampler_linear_clamp, input.screenUV);
                return output;
            }
            
            ENDHLSL
        }
        
        Pass {
            
            Name "ScaledBlit"
            
            Cull Off
            ZWrite Off
            ZTest Always
            
            HLSLPROGRAM

            #pragma vertex ScaledBlitVert
            #pragma fragment ScaledBlitFragment

            #include "ARPCommon.hlsl"

            struct VertexOutput {
                float4 posCS : SV_POSITION;
                float2 screenUV : VAR_SCREEN_UV;
            };

            VertexOutput ScaledBlitVert(uint vertexID : SV_VertexID) {
                VertexOutput output;
                output.posCS = VertexIDToPosCS(vertexID);
                output.screenUV = VertexIDToScreenUV(vertexID);
                return output;
            }

            float4 ScaledBlitFragment(VertexOutput input) : SV_TARGET {
                float2 uv = input.screenUV * _RTHandleProps.rtHandleScale.xy;
                // float2 uv = input.screenUV;
                if (_ProjectionParams.x < 0.0) uv.y = 1 - uv.y;
                float4 output = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                return output;
            }
            
            ENDHLSL
        }
        
        Pass {
            
            Name "DebugStencil"
            
            Cull Off
            ZWrite Off
            ZTest Always
            
            HLSLPROGRAM

            #pragma vertex BlitVert
            #pragma fragment BlitFragment

            #include "ARPCommon.hlsl"

            struct VertexOutput {
                float4 posCS : SV_POSITION;
                float2 screenUV : VAR_SCREEN_UV;
            };

            VertexOutput BlitVert(uint vertexID : SV_VertexID) {
                VertexOutput output;
                output.posCS = VertexIDToPosCS(vertexID);
                output.screenUV = VertexIDToScreenUV(vertexID);
                return output;
            }

            float3 BlitFragment(VertexOutput input) : SV_TARGET {
                float2 uv = input.screenUV;
                if (_ProjectionParams.x < 0.0) uv.y = 1 - uv.y;
                
                uint stencil = LOAD_TEXTURE2D(_StencilTex, int2(floor(uv.x * _ScreenSize.x), floor(uv.y * _ScreenSize.y))).STENCIL_CHANNEL;

                float3 output = float3(.0f, float(stencil) / 255.0f, .0f);

                // if (stencil != 0) output.r = .5f;
                
                return output;
            }
            
            ENDHLSL
        }
    }
}