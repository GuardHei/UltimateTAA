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

                float d0 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0);
                float d1 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0, offset1);
                float d2 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0, offset2);
                float d3 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0, offset3);
                float d4 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0, offset4);
                float d5 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0, offset5);
                float d6 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0, offset6);
                float d7 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0, offset7);
                float d8 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0, offset8);

                int2 closetOffset = CLOSER_DEPTH(d0, d1) ? int2(0, 0) : offset1;
                closetOffset = CLOSER_DEPTH(closetOffset, d2) ? closetOffset : offset2;
                closetOffset = CLOSER_DEPTH(closetOffset, d3) ? closetOffset : offset3;
                closetOffset = CLOSER_DEPTH(closetOffset, d4) ? closetOffset : offset4;
                closetOffset = CLOSER_DEPTH(closetOffset, d5) ? closetOffset : offset5;
                closetOffset = CLOSER_DEPTH(closetOffset, d6) ? closetOffset : offset6;
                closetOffset = CLOSER_DEPTH(closetOffset, d7) ? closetOffset : offset7;
                closetOffset = CLOSER_DEPTH(closetOffset, d8) ? closetOffset : offset8;
                
                float2 mv = LOAD_TEXTURE2D(_VelocityTex, coord + closetOffset).rg; // if velocity is outside the screen, we want to set it to 0 instead of clamping.
                uv -= mv;

                float4 prev = SAMPLE_TEXTURE2D(_PrevTaaColorTex, sampler_linear_clamp, uv);
                // prev = AnyIsNaN(prev) ? curr : prev;

                float3 n0 = RGBToYCoCg(curr.rgb);

                float3 minColor = n0;
                float3 maxColor = minColor;
                
                float3 n1 = RGBToYCoCg(_MainTex.SampleLevel(sampler_linear_clamp, uv, 0, offset1).rgb);
                float3 n2 = RGBToYCoCg(_MainTex.SampleLevel(sampler_linear_clamp, uv, 0, offset2).rgb);
                float3 n3 = RGBToYCoCg(_MainTex.SampleLevel(sampler_linear_clamp, uv, 0, offset3).rgb);
                float3 n4 = RGBToYCoCg(_MainTex.SampleLevel(sampler_linear_clamp, uv, 0, offset4).rgb);
                float3 n5 = RGBToYCoCg(_MainTex.SampleLevel(sampler_linear_clamp, uv, 0, offset5).rgb);
                float3 n6 = RGBToYCoCg(_MainTex.SampleLevel(sampler_linear_clamp, uv, 0, offset6).rgb);
                float3 n7 = RGBToYCoCg(_MainTex.SampleLevel(sampler_linear_clamp, uv, 0, offset7).rgb);
                float3 n8 = RGBToYCoCg(_MainTex.SampleLevel(sampler_linear_clamp, uv, 0, offset8).rgb);

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