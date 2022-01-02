Shader "Hidden/ARPCubemapReflection" {
    Properties {
        _MainTex("Texture", 2D) = "white" { }
    }
    
    SubShader {
        
        Pass {
            
            Name "CubemapReflection"
            
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
                // output.posCS = Test1(vertexID);
                // output.screenUV = Test2(vertexID);
                output.ray = VertexIDToFrustumCorners(vertexID);
                // output.ray.w = vertexID == 0 ? 2 : 0;
                return output;
            }

            float4 Fragment(VertexOutput input) : SV_TARGET {
                float2 uv = input.screenUV;
                if (_ProjectionParams.x < 0.0) uv.y = 1 - uv.y;

                float3 N = SampleNormalWS(uv);
                
                float depth = SampleDepth(uv);
                float4 posWS = DepthToWorldPosFast(depth, input.ray);
                float3 V = normalize(_CameraPosWS - posWS.xyz);

                float NdotV = dot(N, V);
                float3 R = reflect(-V, N);

                float4 packed = SAMPLE_TEXTURE2D(_GBuffer2, sampler_point_clamp, uv);
                float3 f0 = packed.rgb;
                float linearRoughness = packed.a;
                float roughness = LinearRoughnessToRoughness(linearRoughness);
                float3 kS = F_SchlickRoughness(f0, NdotV, linearRoughness);

                float3 energyCompensation;
                float4 lut = GetDGFFromLut(energyCompensation, f0, roughness, NdotV);
                
                float3 indirectSpecular = EvaluateSpecularIBL(kS, R, linearRoughness, lut.rgb, energyCompensation);
                
                float4 output = float4(indirectSpecular, 1.0f);
                return output;
            }
            
            ENDHLSL
        }
    }
}