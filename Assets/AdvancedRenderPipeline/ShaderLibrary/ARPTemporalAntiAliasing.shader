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
            float4 _TaaParams_0; // { minHistoryWeight, maxHistoryWeight, minClipScale, maxClipScale }
            float4 _TaaParams_1; // { minVelocityRejection, velocityRejectionScale, minDepthRejection, depthRejectionScale }
            float4 _TaaParams_2; // { minSharpness, maxSharpness }

            VertexOutput Vert(uint vertexID : SV_VertexID) {
                VertexOutput output;
                output.posCS = VertexIDToPosCS(vertexID);
                output.screenUV = VertexIDToScreenUV(vertexID);
                return output;
            }

            float4 Fragment(VertexOutput input) : SV_TARGET {
                float2 uv = input.screenUV;
                if (_ProjectionParams.x < .0f) uv.y = 1.0f - uv.y;

                float4 curr = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_linear_clamp, uv, 0);
                curr = FastTonemap(curr);
                float4 output;

                if (_EnableReprojection < .0f) {
                    output = curr;
                    output.a = 1.0f;
                    return output;
                }

                int2 coord = int2(floor(uv.x * _ScreenSize.x), floor(uv.y * _ScreenSize.y));
                
                /*
                int2 coord1 = int2(coord.x + 1, coord.y);
                int2 coord2 = int2(coord.x + 1, coord.y + 1);
                int2 coord3 = int2(coord.x + 1, coord.y - 1);
                int2 coord4 = int2(coord.x, coord.y + 1);
                int2 coord5 = int2(coord.x, coord.y - 1);
                int2 coord6 = int2(coord.x - 1, coord.y);
                int2 coord7 = int2(coord.x - 1, coord.y + 1);
                int2 coord8 = int2(coord.x - 1, coord.y - 1);
                */

                int2 offset1 = int2(1, 0);
                int2 offset2 = int2(1, 1);
                int2 offset3 = int2(1, -1);
                int2 offset4 = int2(0, 1);
                int2 offset5 = int2(0, -1);
                int2 offset6 = int2(-1, 0);
                int2 offset7 = int2(-1, 1);
                int2 offset8 = int2(-1, -1);

                float d0 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0).r;
                float d1 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0, offset1).r;
                float d2 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0, offset2).r;
                float d3 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0, offset3).r;
                float d4 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0, offset4).r;
                float d5 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0, offset5).r;
                float d6 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0, offset6).r;
                float d7 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0, offset7).r;
                float d8 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0, offset8).r;

                int2 closetOffset = CLOSER_DEPTH(d0, d1) ? int2(0, 0) : offset1;
                closetOffset = CLOSER_DEPTH(closetOffset, d2) ? closetOffset : offset2;
                closetOffset = CLOSER_DEPTH(closetOffset, d3) ? closetOffset : offset3;
                closetOffset = CLOSER_DEPTH(closetOffset, d4) ? closetOffset : offset4;
                closetOffset = CLOSER_DEPTH(closetOffset, d5) ? closetOffset : offset5;
                closetOffset = CLOSER_DEPTH(closetOffset, d6) ? closetOffset : offset6;
                closetOffset = CLOSER_DEPTH(closetOffset, d7) ? closetOffset : offset7;
                closetOffset = CLOSER_DEPTH(closetOffset, d8) ? closetOffset : offset8;
                
                float2 mv = LOAD_TEXTURE2D(_VelocityTex, coord + closetOffset).rg; // if velocity is outside the screen, we want to set it to 0 instead of clamping.
                float2 prevUV = uv - mv;

                float4 prev = SAMPLE_TEXTURE2D(_PrevTaaColorTex, sampler_linear_clamp, prevUV);
                // prev = AnyIsNaN(prev) ? curr : prev;

                float3 n0 = RGBToYCoCg(curr.rgb);
                float3 n1 = RGBToYCoCg(FastTonemap(_MainTex.SampleLevel(sampler_linear_clamp, uv, 0, offset1).rgb));
                float3 n2 = RGBToYCoCg(FastTonemap(_MainTex.SampleLevel(sampler_linear_clamp, uv, 0, offset2)).rgb);
                float3 n3 = RGBToYCoCg(FastTonemap(_MainTex.SampleLevel(sampler_linear_clamp, uv, 0, offset3)).rgb);
                float3 n4 = RGBToYCoCg(FastTonemap(_MainTex.SampleLevel(sampler_linear_clamp, uv, 0, offset4)).rgb);
                float3 n5 = RGBToYCoCg(FastTonemap(_MainTex.SampleLevel(sampler_linear_clamp, uv, 0, offset5)).rgb);
                float3 n6 = RGBToYCoCg(FastTonemap(_MainTex.SampleLevel(sampler_linear_clamp, uv, 0, offset6)).rgb);
                float3 n7 = RGBToYCoCg(FastTonemap(_MainTex.SampleLevel(sampler_linear_clamp, uv, 0, offset7)).rgb);
                float3 n8 = RGBToYCoCg(FastTonemap(_MainTex.SampleLevel(sampler_linear_clamp, uv, 0, offset8)).rgb);

                float3 m1 = n0;
                float3 m2 = n0 * n0;

                m1 += n1;
                m2 += n1 * n1;
                m1 += n2;
                m2 += n2 * n2;
                m1 += n3;
                m2 += n3 * n3;
                m1 += n4;
                m2 += n4 * n4;
                m1 += n5;
                m2 += n5 * n5;
                m1 += n6;
                m2 += n6 * n6;
                m1 += n7;
                m2 += n7 * n7;
                m1 += n8;
                m2 += n8 * n8;

                prev.rgb = YCoCgToRGB(ClipVariance(m1, m2, 9.0f, 1.0f, RGBToYCoCg(prev.rgb)));
                
                /*
                float3 minColor = n0;
                float3 maxColor = minColor;

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
                */

                // prev.rgb = YCoCgToRGB(clamp(RGBToYCoCg(prev.rgb), minColor, maxColor));

                // prev.rgb = YCoCgToRGB(ClipAABB(minColor, maxColor, RGBToYCoCg(prev.rgb)));

                float3 corners = YCoCgToRGB(n2) + YCoCgToRGB(n3) + YCoCgToRGB(n7) + YCoCgToRGB(n8);
                corners = (corners - curr.rgb) * 2.0f * .166667f;
                corners = (curr.rgb - corners) * 2.718282f * _TaaParams_2.y;
                curr.rgb += corners;
                curr = clamp(curr, 0, HALF_MAX);

                /*
                float3 sharpen = YCoCgToRGB(n1) + YCoCgToRGB(n4) + YCoCgToRGB(n5) + YCoCgToRGB(n6);
                // curr.rgb = curr.rgb * 5.0f + sharpen * -1.0f;
                curr = clamp(curr, .0f, HALF_MAX);
                */

                output = lerp(curr, prev, _TaaParams_0.y);
                output.a = 1.0f;
                return output;
            }
            
            ENDHLSL
        }
    }
}