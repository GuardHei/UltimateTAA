Shader "Hidden/ARPIndirectSpecular" {
    
    Properties {
        _MainTex("Texture", 2D) = "white" { }
    }
    
    SubShader {
        
        Pass {
            
            Name "ScreenSpaceReflection"
            
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
                return float4(.0f, .0f, .0f, .0f);
            }
            
            ENDHLSL
        }
        
        Pass {
            
            Name "CubemapReflection"
            
            Cull Off
            ZWrite Off
            ZTest Always
            
            HLSLPROGRAM

            #pragma shader_feature_local ACCURATE_TRANSFORM_ON
            #pragma vertex Vert
            #pragma fragment Fragment

            #include "ARPCommon.hlsl"

            struct VertexOutput {
                float4 posCS : SV_POSITION;
                float2 screenUV : VAR_SCREEN_UV;
                #if !defined(ACCURATE_TRANSFORM_ON)
                float3 ray : TEXCOORD0;
                #endif
            };

            VertexOutput Vert(uint vertexID : SV_VertexID) {
                VertexOutput output;
                output.posCS = VertexIDToPosCS(vertexID);
                output.screenUV = VertexIDToScreenUV(vertexID);
                // output.posCS = Test1(vertexID);
                // output.screenUV = Test2(vertexID);
                #if !defined(ACCURATE_TRANSFORM_ON)
                output.ray = VertexIDToFrustumCorners(vertexID).xyz;
                // output.ray.w = vertexID == 0 ? 2 : 0;
                #endif
                return output;
            }

            float3 Fragment(VertexOutput input) : SV_TARGET {
                float2 uv = input.screenUV;
                if (_ProjectionParams.x < 0.0) uv.y = 1 - uv.y;

                float3 N = SampleNormalWS(uv);
                float iblOcclusion = SAMPLE_TEXTURE2D(_GBuffer3, sampler_point_clamp, uv).r;
                
                float depth = SampleDepth(uv);
                
                #if defined(ACCURATE_TRANSFORM_ON)
                float4 posWS = DepthToWorldPos(depth, uv);
                #else
                float4 posWS = DepthToWorldPosFast(depth, input.ray);
                #endif
                
                float3 V = normalize(_CameraPosWS.xyz - posWS.xyz);

                float NdotV = dot(N, V);
                float3 R = reflect(-V, N);

                float4 packed = SAMPLE_TEXTURE2D(_GBuffer2, sampler_point_clamp, uv);
                float3 f0 = packed.rgb;
                float linearRoughness = packed.a;
                float roughness = LinearRoughnessToRoughness(linearRoughness);
                // float3 kS = F_SchlickRoughness(f0, NdotV, linearRoughness);

                float3 energyCompensation;
                float3 lut = GetGFFromLut(energyCompensation, f0, roughness, NdotV);

                // iblOcclusion = 1.0f;

                float3 specularIBL = EvaluateSpecularIBL(R, linearRoughness, lut, energyCompensation) * iblOcclusion;

                // return iblOcclusion;
                return specularIBL;
            }
            
            ENDHLSL
        }
    }
}