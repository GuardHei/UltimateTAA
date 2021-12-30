Shader "Hidden/ARPTonemapping" {
    Properties {
        _MainTex("Texture", 2D) = "white" { }
    }
    
    SubShader {
        
        Pass {
            
            Name "Tonemapping"
            
            Cull Off
            ZWrite Off
            ZTest Always
            
            HLSLPROGRAM

            #pragma vertex TonemapVert
            #pragma fragment TonemapFragment

            #include "ARPCommon.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            struct VertexOutput {
                float4 posCS : SV_POSITION;
                float2 screenUV : VAR_SCREEN_UV;
            };

            int _TonemappingMode;

            VertexOutput TonemapVert(uint vertexID : SV_VertexID) {
                VertexOutput output;
                output.posCS = VertexIDToPosCS(vertexID);
                output.screenUV = VertexIDToScreenUV(vertexID);
                return output;
            }

            float4 TonemapFragment(VertexOutput input) : SV_TARGET {
                float2 uv = input.screenUV;
                if (_ProjectionParams.x < 0.0) uv.y = 1 - uv.y;
                float4 output = SAMPLE_TEXTURE2D(_MainTex, sampler_linear_clamp, uv);
                
                if (_TonemappingMode == 1) output.rgb = AcesTonemap(unity_to_ACES(output.rgb));
                else if (_TonemappingMode == 2) output.rgb = NeutralTonemap(output.rgb);
                else if (_TonemappingMode == 3) output.rgb = output.rgb / (output.rgb + 1.0f);
                return output;
            }
            
            ENDHLSL
        }
    }
}