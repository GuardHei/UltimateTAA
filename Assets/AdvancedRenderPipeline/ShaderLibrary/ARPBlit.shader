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
                if (_ProjectionParams.x < .0f) uv.y = 1.0f - uv.y;
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
                if (_ProjectionParams.x < .0f) uv.y = 1.0f - uv.y;
                float4 output = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                return output;
            }
            
            ENDHLSL
        }
        
        Pass {
            
            Name "BlitStencil"
            
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

            uint BlitFragment(VertexOutput input) : SV_TARGET {
                float2 uv = input.screenUV;
                if (_ProjectionParams.x < .0f) uv.y = 1.0f - uv.y;
                
                return LOAD_TEXTURE2D(_StencilTex, int2(floor(uv.x * _ScreenSize.x), floor(uv.y * _ScreenSize.y))).STENCIL_CHANNEL;
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
                if (_ProjectionParams.x < .0f) uv.y = 1.0f - uv.y;
                
                uint stencil = LOAD_TEXTURE2D(_StencilTex, int2(floor(uv.x * _ScreenSize.x), floor(uv.y * _ScreenSize.y))).STENCIL_CHANNEL;

                float3 output = float3(.0f, float(stencil) / 255.0f, .0f);

                if ((stencil & (1 << 2)) == 1 << 2) output.r = .5f;
                
                return output;
            }
            
            ENDHLSL
        }
        
        Pass {
            
            Name "DebugVelocity"
            
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

            float4 VectorToColor(float2 mv) {
				float phi = atan2(mv.x, mv.y);
				float hue = (phi / PI + 1.0f) * .5f;

				float r = abs(hue * 6.0f - 3.0f) - 1.0f;
				float g = 2.0f - abs(hue * 6.0f - 2.0f);
				float b = 2.0f - abs(hue * 6.0f - 4.0f);
				float a = length(mv);

				return saturate(float4(r, g, b, a));
			}

            float4 BlitFragment(VertexOutput input) : SV_TARGET {
                float2 uv = input.screenUV;
                if (_ProjectionParams.x < .0f) uv.y = 1.0f - uv.y;
                
                float2 output = SAMPLE_TEXTURE2D(_MainTex, sampler_point_clamp, uv).rg * 5.0f;
                return float4(output.r, output.g, .0f, 1.0f);
                return VectorToColor(output);
            }
            
            ENDHLSL
        }
        
        Pass {
            
            Name "DebugSmoothness"
            
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
                if (_ProjectionParams.x < .0f) uv.y = 1.0f - uv.y;
                float output = 1.0f - SAMPLE_TEXTURE2D(_MainTex, sampler_point_clamp, uv).a;
                return output;
            }
            
            ENDHLSL
        }
        
        Pass {
            
            Name "DebugNaN"
            
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
                if (_ProjectionParams.x < .0f) uv.y = 1.0f - uv.y;
                float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_point_clamp, uv);
                float4 output = float4(.0f, .0f, .0f, 1.0f);
                output.r = AnyIsNaN(color) ? 1.0f : .0f;
                output.g = AnyIsInf(color) ? 1.0f : .0f;
                
                return output;
            }
            
            ENDHLSL
        }
    }
}