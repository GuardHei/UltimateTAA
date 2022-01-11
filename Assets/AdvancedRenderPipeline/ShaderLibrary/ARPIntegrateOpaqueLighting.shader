Shader "Hidden/ARPIntegrateOpaqueLighting" {
    
    Properties {
        _MainTex("Texture", 2D) = "white" { }
    }
    
    SubShader {
        
        Pass {
            
            Name "IntegrateOpaqueLighting"
            
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

            float4 Fragment(VertexOutput input) : SV_TARGET {
                float2 uv = input.screenUV;
                if (_ProjectionParams.x < 0.0) uv.y = 1 - uv.y;

                float4 finalLighting = float4(.0f, .0f, .0f, 1.0f);

                float3 forwardLighting = SAMPLE_TEXTURE2D(_RawColorTex, sampler_point_clamp, uv).rgb;
                float3 indirectSpecular = SAMPLE_TEXTURE2D(_IndirectSpecular, sampler_point_clamp, uv).rgb;
                float indirectOcclusion = 1.0f;

                finalLighting.rgb = (forwardLighting + indirectSpecular) * indirectOcclusion;
                finalLighting.a = 1.0f;
                
                return finalLighting;
            }
            
            ENDHLSL
        }
    }
}