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
            float4 _TaaParams_2; // { minSharpness, maxSharpness, motionSharpeningFactor, staticClipScale }

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

                const int2 offset1 = int2(1, 0);
                const int2 offset2 = int2(1, 1);
                const int2 offset3 = int2(1, -1);
                const int2 offset4 = int2(0, 1);
                const int2 offset5 = int2(0, -1);
                const int2 offset6 = int2(-1, 0);
                const int2 offset7 = int2(-1, 1);
                const int2 offset8 = int2(-1, -1);

                const float d0 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0).r;
                const float d1 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0, offset1).r;
                const float d2 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0, offset2).r;
                const float d3 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0, offset3).r;
                const float d4 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0, offset4).r;
                const float d5 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0, offset5).r;
                const float d6 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0, offset6).r;
                const float d7 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0, offset7).r;
                const float d8 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0, offset8).r;

                // velocity dilation
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

                float3 c1 = FastTonemap(_MainTex.SampleLevel(sampler_linear_clamp, uv, 0, offset1).rgb);
                float3 c2 = FastTonemap(_MainTex.SampleLevel(sampler_linear_clamp, uv, 0, offset2)).rgb;
                float3 c3 = FastTonemap(_MainTex.SampleLevel(sampler_linear_clamp, uv, 0, offset3)).rgb;
                float3 c4 = FastTonemap(_MainTex.SampleLevel(sampler_linear_clamp, uv, 0, offset4)).rgb;
                float3 c5 = FastTonemap(_MainTex.SampleLevel(sampler_linear_clamp, uv, 0, offset5)).rgb;
                float3 c6 = FastTonemap(_MainTex.SampleLevel(sampler_linear_clamp, uv, 0, offset6)).rgb;
                float3 c7 = FastTonemap(_MainTex.SampleLevel(sampler_linear_clamp, uv, 0, offset7)).rgb;
                float3 c8 = FastTonemap(_MainTex.SampleLevel(sampler_linear_clamp, uv, 0, offset8)).rgb;

                float3 n0 = RGBToYCoCg(curr.rgb);
                float3 n1 = RGBToYCoCg(c1);
                float3 n2 = RGBToYCoCg(c2);
                float3 n3 = RGBToYCoCg(c3);
                float3 n4 = RGBToYCoCg(c4);
                float3 n5 = RGBToYCoCg(c5);
                float3 n6 = RGBToYCoCg(c6);
                float3 n7 = RGBToYCoCg(c7);
                float3 n8 = RGBToYCoCg(c8);

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

                const float minHistoryWeight = _TaaParams_0.x;
                const float maxHistoryWeight = _TaaParams_0.y;
                const float minClipScale = _TaaParams_0.z;
                const float maxClipScale = _TaaParams_0.w;
                const float minVelocityRejection = clamp(.00001f, .99999f, _TaaParams_1.x);
                const float velocityRejectionScale = _TaaParams_1.y;
                const float minDepthRejection = _TaaParams_1.z;
                const float depthRejectionScale = _TaaParams_1.w;
                const float minSharpness = _TaaParams_2.x;
                const float maxSharpness = _TaaParams_2.y;
                const float motionSharpeningFactor = _TaaParams_2.z;
                const float staticClipScale = _TaaParams_2.w;
                
                float mvScale = length(mv);

                // velocity weighting - default standard should be 1.0f
                float clipScale = mvScale < minVelocityRejection ? lerp(maxClipScale, 1.0f, mvScale / minVelocityRejection) : lerp(1.0f, minClipScale, saturate((mvScale - minVelocityRejection) * velocityRejectionScale / (1.0f - minVelocityRejection)));
                
                int2 prevCoord = int2(floor(prevUV.x * _ScreenSize.x), floor(prevUV.y * _ScreenSize.y));
                
                float prevMVScale = length(LOAD_TEXTURE2D(_PrevVelocityTex, prevCoord).rg);
                float prevPotentialMVScale = length(LOAD_TEXTURE2D(_PrevVelocityTex, prevCoord + offset1).rg);
                prevMVScale = max(prevMVScale, prevPotentialMVScale);
                prevPotentialMVScale = length(LOAD_TEXTURE2D(_PrevVelocityTex, prevCoord + offset2).rg);
                prevMVScale = max(prevMVScale, prevPotentialMVScale);
                prevPotentialMVScale = length(LOAD_TEXTURE2D(_PrevVelocityTex, prevCoord + offset3).rg);
                prevMVScale = max(prevMVScale, prevPotentialMVScale);
                prevPotentialMVScale = length(LOAD_TEXTURE2D(_PrevVelocityTex, prevCoord + offset4).rg);
                prevMVScale = max(prevMVScale, prevPotentialMVScale);
                prevPotentialMVScale = length(LOAD_TEXTURE2D(_PrevVelocityTex, prevCoord + offset5).rg);
                prevMVScale = max(prevMVScale, prevPotentialMVScale);
                prevPotentialMVScale = length(LOAD_TEXTURE2D(_PrevVelocityTex, prevCoord + offset6).rg);
                prevMVScale = max(prevMVScale, prevPotentialMVScale);
                prevPotentialMVScale = length(LOAD_TEXTURE2D(_PrevVelocityTex, prevCoord + offset7).rg);
                prevMVScale = max(prevMVScale, prevPotentialMVScale);
                prevPotentialMVScale = length(LOAD_TEXTURE2D(_PrevVelocityTex, prevCoord + offset8).rg);
                prevMVScale = max(prevMVScale, prevPotentialMVScale);

                // stencil test
                const uint st0 = LOAD_TEXTURE2D(_StencilTex, coord).STENCIL_CHANNEL & 3;
                const uint st2 = LOAD_TEXTURE2D(_StencilTex, coord + offset2).STENCIL_CHANNEL & 3;
                const uint st3 = LOAD_TEXTURE2D(_StencilTex, coord + offset3).STENCIL_CHANNEL & 3;
                const uint st7 = LOAD_TEXTURE2D(_StencilTex, coord + offset7).STENCIL_CHANNEL & 3;
                const uint st8 = LOAD_TEXTURE2D(_StencilTex, coord + offset8).STENCIL_CHANNEL & 3;
                const uint prevSt = LOAD_TEXTURE2D(_PrevStencilTex, prevCoord).STENCIL_CHANNEL & 3;

                bool atEdge = st0 != st2;
                atEdge = atEdge || st0 != st3;
                atEdge = atEdge || st0 != st7;
                atEdge = atEdge || st0 != st8;

                const float linearDepth0 = DepthToLinearEyeSpace(d0);
                const float linearDepth2 = DepthToLinearEyeSpace(d2);
                const float linearDepth3 = DepthToLinearEyeSpace(d3);
                const float linearDepth7 = DepthToLinearEyeSpace(d7);
                const float linearDepth8 = DepthToLinearEyeSpace(d8);

                atEdge = atEdge || abs(linearDepth0 - linearDepth2) > minDepthRejection;
                atEdge = atEdge || abs(linearDepth0 - linearDepth3) > minDepthRejection;
                atEdge = atEdge || abs(linearDepth0 - linearDepth7) > minDepthRejection;
                atEdge = atEdge || abs(linearDepth0 - linearDepth8) > minDepthRejection;

                // return atEdge ? 1.0f : .0f;

                const float prevDepth = _PrevDepthTex.SampleLevel(sampler_point_clamp, prevUV, 0).r;
                const float linearPrevDepth = PrevDepthToLinearEyeSpace(prevDepth);

                bool mismatch = (st0 != prevSt) || abs(linearDepth0 - linearPrevDepth) > minDepthRejection;
                // mismatch = mismatch && !atEdge;
                
                clipScale = mismatch ? minClipScale : clipScale;

                // count for imprecision
                bool staticPixel = mvScale < 2.0f * FLT_EPS && prevMVScale < 2.0f * FLT_EPS;
                // increase clip box scale when it is a static pixel and we detect a large different in depth/stencil either historically or spatially
                // we also need to make sure it is not covered by any transparent effect, in which case the MV computation is broken
                bool antiFlicker = staticPixel && (atEdge || mismatch || prev.a < .5f) && curr.a > .99f;
                clipScale = antiFlicker ? staticClipScale : clipScale;

                prev.rgb = YCoCgToRGB(ClipVariance(m1, m2, 9.0f, clipScale, RGBToYCoCg(prev.rgb)));
                
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
                

                prev.rgb = YCoCgToRGB(clamp(RGBToYCoCg(prev.rgb), minColor, maxColor));

                // prev.rgb = YCoCgToRGB(ClipAABB(minColor, maxColor, RGBToYCoCg(prev.rgb)));
                */

                // sharpening
                /*
                float3 corners = c2 + c3 + c7 + c8;
                corners = (corners - curr.rgb) * 2.0f * .166667f;
                corners = (curr.rgb - corners) * 2.718282f * _TaaParams_2.y;
                curr.rgb += corners;
                curr = clamp(curr, 0, HALF_MAX);
                */

                // cross pattern sharperning
                float sharpnessFactor = lerp(maxSharpness, minSharpness, mvScale * motionSharpeningFactor);
                float3 corners = c1 + c4 + c5 + c6;
                curr.rgb = curr.rgb * (1.0 + 4.0f * sharpnessFactor) + corners * -sharpnessFactor;
                curr = clamp(curr, .0f, HALF_MAX);

                // decrease history weight when transparent pixel has a large opacity (motion vector is less reliable here)
                float historyWeight = curr.a > .999f ? maxHistoryWeight : lerp(maxHistoryWeight, minHistoryWeight, curr.a);

                output = lerp(curr, prev, historyWeight);
                // record antiFlicker history
                output.a = antiFlicker ? .0f : 1.0f;
                // output.rgb = clipScale > 1.0f ? 1.0f : .0f;
                return output;
            }
            
            ENDHLSL
        }
    }
}