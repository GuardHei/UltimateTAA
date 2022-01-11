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
            #include "ARPAntiAliasing.hlsl"
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
                    output.a = 1.0f;
                    return output;
                }

                int2 coord = int2(floor(uv.x * _ScreenSize.x), floor(uv.y * _ScreenSize.y));
                int2 coord1 = int2(coord.x + 1, coord.y);
                int2 coord2 = int2(coord.x + 1, coord.y + 1);
                int2 coord3 = int2(coord.x + 1, coord.y - 1);
                int2 coord4 = int2(coord.x, coord.y + 1);
                int2 coord5 = int2(coord.x, coord.y - 1);
                int2 coord6 = int2(coord.x - 1, coord.y);
                int2 coord7 = int2(coord.x - 1, coord.y + 1);
                int2 coord8 = int2(coord.x - 1, coord.y - 1);

                float d0 = LOAD_TEXTURE2D(_DepthTex, coord).r;
                float d1 = LOAD_TEXTURE2D(_DepthTex, coord1).r;
                float d2 = LOAD_TEXTURE2D(_DepthTex, coord2).r;
                float d3 = LOAD_TEXTURE2D(_DepthTex, coord3).r;
                float d4 = LOAD_TEXTURE2D(_DepthTex, coord4).r;
                float d5 = LOAD_TEXTURE2D(_DepthTex, coord5).r;
                float d6 = LOAD_TEXTURE2D(_DepthTex, coord6).r;
                float d7 = LOAD_TEXTURE2D(_DepthTex, coord7).r;
                float d8 = LOAD_TEXTURE2D(_DepthTex, coord8).r;

                int2 closetCoord = CLOSER_DEPTH(d0, d1) ? coord : coord1;
                closetCoord = CLOSER_DEPTH(closetCoord, d2) ? closetCoord : coord2;
                closetCoord = CLOSER_DEPTH(closetCoord, d3) ? closetCoord : coord3;
                closetCoord = CLOSER_DEPTH(closetCoord, d4) ? closetCoord : coord4;
                closetCoord = CLOSER_DEPTH(closetCoord, d5) ? closetCoord : coord5;
                closetCoord = CLOSER_DEPTH(closetCoord, d6) ? closetCoord : coord6;
                closetCoord = CLOSER_DEPTH(closetCoord, d7) ? closetCoord : coord7;
                closetCoord = CLOSER_DEPTH(closetCoord, d8) ? closetCoord : coord8;
                
                float2 mv = LOAD_TEXTURE2D(_VelocityTex, closetCoord).rg;
                uv -= mv;

                /*
                if(min(uv.x, uv.y) < 0 || max(uv.x, uv.y) > 1) prev = curr;
                else prev = SAMPLE_TEXTURE2D(_PrevTaaColorTex, sampler_linear_clamp, uv);
                */

                float4 prev = SAMPLE_TEXTURE2D(_PrevTaaColorTex, sampler_linear_clamp, uv);
                prev = AnyIsNaN(prev) ? curr : prev;

                float3 n0 = RGBToYCoCg(curr.rgb);

                float3 minColor = n0;
                float3 maxColor = minColor;
                
                float3 n1 = RGBToYCoCg(LOAD_TEXTURE2D(_MainTex, coord1).rgb);
                float3 n2 = RGBToYCoCg(LOAD_TEXTURE2D(_MainTex, coord2).rgb);
                float3 n3 = RGBToYCoCg(LOAD_TEXTURE2D(_MainTex, coord3).rgb);
                float3 n4 = RGBToYCoCg(LOAD_TEXTURE2D(_MainTex, coord4).rgb);
                float3 n5 = RGBToYCoCg(LOAD_TEXTURE2D(_MainTex, coord5).rgb);
                float3 n6 = RGBToYCoCg(LOAD_TEXTURE2D(_MainTex, coord6).rgb);
                float3 n7 = RGBToYCoCg(LOAD_TEXTURE2D(_MainTex, coord7).rgb);
                float3 n8 = RGBToYCoCg(LOAD_TEXTURE2D(_MainTex, coord8).rgb);

                minColor = min(minColor, n1);
                minColor = min(minColor, n2);
                minColor = min(minColor, n3);
                minColor = min(minColor, n4);
                minColor = min(minColor, n5);
                minColor = min(minColor, n6);
                minColor = min(minColor, n7);
                minColor = min(minColor, n8);

                maxColor = max(maxColor, n1);
                maxColor = max(maxColor, n2);
                maxColor = max(maxColor, n3);
                maxColor = max(maxColor, n4);
                maxColor = max(maxColor, n5);
                maxColor = max(maxColor, n6);
                maxColor = max(maxColor, n7);
                maxColor = max(maxColor, n8);

                // prev.rgb = YCoCgToRGB(clamp(RGBToYCoCg(prev.rgb), minColor, maxColor));

                prev.rgb = YCoCgToRGB(ClipAABB(minColor, maxColor, RGBToYCoCg(prev.rgb)));

                output = lerp(curr, prev, _TaaParams.y);
                output.a = 1.0f;
                return output;
            }
            
            ENDHLSL
        }
    }
}