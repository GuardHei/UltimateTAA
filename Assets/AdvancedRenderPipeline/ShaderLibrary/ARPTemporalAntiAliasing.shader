Shader "Hidden/ARPTemporalAntiAliasing" {
    
    Properties {
        _MainTex("Texture", 2D) = "white" { }
    }
    
    SubShader {
        
        Pass {
            
            Name "TemporalAntiAliasing"
            
            Cull Off
            ZWrite Off
            ZTest Always
            
            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Fragment

            #include "ARPCommon.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            struct VertexOutput {
                float4 posCS : SV_POSITION;
                float2 screenUV : VAR_SCREEN_UV;
            };

            float _EnableReprojection;
            float4 _TaaParams; // { minHistoryWeight, maxHistoryWeight, minClipScale, maxClipScale }

            VertexOutput Vert(uint vertexID : SV_VertexID) {
                VertexOutput output;
                output.posCS = VertexIDToPosCS(vertexID);
                output.screenUV = VertexIDToScreenUV(vertexID);
                return output;
            }

            float4 Fragment(VertexOutput input) : SV_TARGET {
                float2 uv = input.screenUV;
                if (_ProjectionParams.x < .0f) uv.y = 1.0f - uv.y;

                float4 curr = SAMPLE_TEXTURE2D(_MainTex, sampler_linear_clamp, uv);
                curr = FastTonemap(curr);
                float4 output;

                if (_EnableReprojection < .0f) {
                    output = curr;
                    return output;
                }

                float4 prev = SAMPLE_TEXTURE2D(_PrevTaaColorTex, sampler_linear_clamp, uv);

                output = lerp(curr, prev, _TaaParams.y);
                return output;
            }
            
            ENDHLSL
        }
    }
}