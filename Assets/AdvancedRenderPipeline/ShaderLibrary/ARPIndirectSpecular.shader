Shader "Hidden/ARPIndirectSpecular" {
    
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
                output.ray = VertexIDToFrustumCorners(vertexID).xyz;
                // output.ray.w = vertexID == 0 ? 2 : 0;
                return output;
            }

            float3 Fragment(VertexOutput input) : SV_TARGET {
                float2 uv = input.screenUV;
                if (_ProjectionParams.x < 0.0) uv.y = 1 - uv.y;

                float3 N = SampleNormalWS(uv);
                float iblOcclusion = SAMPLE_TEXTURE2D(_GBuffer3, sampler_point_clamp, uv).r;
                
                float depth = SampleDepth(uv);
                float4 posWS = DepthToWorldPosFast(depth, input.ray);
                float3 V = normalize(_CameraPosWS.xyz - posWS.xyz);

                float NdotV = dot(N, V);
                float3 R = reflect(-V, N);

                float4 packed = SAMPLE_TEXTURE2D(_GBuffer2, sampler_point_clamp, uv);
                float3 f0 = packed.rgb;
                float linearRoughness = packed.a;
                float roughness = LinearRoughnessToRoughness(linearRoughness);
                float3 kS = F_SchlickRoughness(f0, NdotV, linearRoughness);

                float3 energyCompensation;
                float3 lut = GetGFFromLut(energyCompensation, f0, roughness, NdotV);

                float3 specularIBL = EvaluateSpecularIBL(kS, R, linearRoughness, lut, energyCompensation) * iblOcclusion;
                
                return specularIBL;
            }
            
            ENDHLSL
        }
        
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
            
            Name "IntegrateIndirectSpecular"
            
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
            };

            VertexOutput Vert(uint vertexID : SV_VertexID) {
                VertexOutput output;
                output.posCS = VertexIDToPosCS(vertexID);
                output.screenUV = VertexIDToScreenUV(vertexID);
                return output;
            }

            float3 Fragment(VertexOutput input) : SV_TARGET {
                float2 uv = input.screenUV;
                if (_ProjectionParams.x < 0.0) uv.y = 1 - uv.y;
                
                float3 ibl = SAMPLE_TEXTURE2D(_ScreenSpaceCubemap, sampler_point_clamp, uv).rgb;
                float4 ssr = SAMPLE_TEXTURE2D(_ScreenSpaceReflection, sampler_point_clamp, uv);

                float3 indirectSpecular = lerp(ibl, ssr.rgb, ssr.a);
                
                return indirectSpecular;
            }
            
            ENDHLSL
        }
    }
}