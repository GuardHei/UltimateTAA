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
            float4 _TaaParams_1; // { minVelocityRejection, velocityRejectionScale, minDepthRejection, resamplingSharpness }
            float4 _TaaParams_2; // { minSharpness, maxSharpness, motionSharpeningFactor, staticClipScale }

            VertexOutput Vert(uint vertexID : SV_VertexID) {
                VertexOutput output;
                output.posCS = VertexIDToPosCS(vertexID);
                output.screenUV = VertexIDToScreenUV(vertexID);
                return output;
            }

            float3 OneTapBicubicFilter(float3 center, float3 top, float3 right, float3 bottom, float3 left, float3 prevCenter, float2 uv, float sharpness) {
                float2 f = frac(_ScreenSize.xy * uv - .5f);
                float c = .8f * sharpness;
                float2 w = c * (f * f - f);
                float4 color = float4(lerp(left, right, f.x), 1.0f) * w.x;
                color += float4(lerp(top, bottom, f.y), 1.0f) * w.y;
                color += float4((1.0f + color.a) * prevCenter - color.a * center, 1.0f);
                return color.rgb * rcp(color.a);
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

                const int2 offset1 = int2(1, 1); // top right
                const int2 offset2 = int2(1, -1); // bottom right
                const int2 offset3 = int2(-1, 1); // top left
                const int2 offset4 = int2(-1, -1); // bottom left
                const int2 offset5 = int2(0, 1); // top middle
                const int2 offset6 = int2(1, 0); // middle right
                const int2 offset7 = int2(0, -1); // bottom middle
                const int2 offset8 = int2(-1, 0); // middle left

                const int2 coord1 = coord + offset1;
                const int2 coord2 = coord + offset2;
                const int2 coord3 = coord + offset3;
                const int2 coord4 = coord + offset4;
                
                const float d0 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0).r;
                const float d1 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0, offset1).r;
                const float d2 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0, offset2).r;
                const float d3 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0, offset3).r;
                const float d4 = _DepthTex.SampleLevel(sampler_point_clamp, uv, 0, offset4).r;

                const float linearDepth0 = DepthToLinearEyeSpace(d0);
                const float linearDepth1 = DepthToLinearEyeSpace(d1);
                const float linearDepth2 = DepthToLinearEyeSpace(d2);
                const float linearDepth3 = DepthToLinearEyeSpace(d3);
                const float linearDepth4 = DepthToLinearEyeSpace(d4);

                // velocity dilation
                float closetDepth = d0;
                float closetLinearDepth = linearDepth0;
                int2 closetOffset = int2(0, 0);
                if (CLOSER_DEPTH(closetDepth, d1)) {
                    closetDepth = d1;
                    closetLinearDepth = linearDepth1;
                    closetOffset = offset1;
                }
                if (CLOSER_DEPTH(closetDepth, d2)) {
                    closetDepth = d2;
                    closetLinearDepth = linearDepth2;
                    closetOffset = offset2;
                }
                if (CLOSER_DEPTH(closetDepth, d3)) {
                    closetDepth = d3;
                    closetLinearDepth = linearDepth3;
                    closetOffset = offset3;
                }
                if (CLOSER_DEPTH(closetDepth, d4)) {
                    closetDepth = d4;
                    closetLinearDepth = linearDepth4;
                    closetOffset = offset4;
                }
                
                float2 mv = LOAD_TEXTURE2D(_VelocityTex, coord + closetOffset).rg; // if velocity is outside the screen, we want to set it to 0 instead of clamping.
                float2 prevUV = uv - mv;

                float3 c1 = FastTonemap(_MainTex.SampleLevel(sampler_point_clamp, uv, 0, offset1)).rgb;
                float3 c2 = FastTonemap(_MainTex.SampleLevel(sampler_point_clamp, uv, 0, offset2)).rgb;
                float3 c3 = FastTonemap(_MainTex.SampleLevel(sampler_point_clamp, uv, 0, offset3)).rgb;
                float3 c4 = FastTonemap(_MainTex.SampleLevel(sampler_point_clamp, uv, 0, offset4)).rgb;
                float3 c5 = FastTonemap(_MainTex.SampleLevel(sampler_point_clamp, uv, 0, offset5)).rgb;
                float3 c6 = FastTonemap(_MainTex.SampleLevel(sampler_point_clamp, uv, 0, offset6)).rgb;
                float3 c7 = FastTonemap(_MainTex.SampleLevel(sampler_point_clamp, uv, 0, offset7)).rgb;
                float3 c8 = FastTonemap(_MainTex.SampleLevel(sampler_point_clamp, uv, 0, offset8)).rgb;

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
                m1 += n2;
                m1 += n3;
                m1 += n4;
                m1 += n5;
                m1 += n6;
                m1 += n7;
                m1 += n8;
                m2 += n1 * n1;
                m2 += n2 * n2;
                m2 += n3 * n3;
                m2 += n4 * n4;
                m2 += n5 * n5;
                m2 += n6 * n6;
                m2 += n7 * n7;
                m2 += n8 * n8;

                const float minHistoryWeight = _TaaParams_0.x;
                const float maxHistoryWeight = _TaaParams_0.y;
                const float minClipScale = _TaaParams_0.z;
                const float maxClipScale = _TaaParams_0.w;
                const float minVelocityRejection = clamp(.00001f, .99999f, _TaaParams_1.x);
                const float velocityRejectionScale = _TaaParams_1.y;
                const float minDepthRejection = _TaaParams_1.z;
                const float resamplingSharpness = _TaaParams_1.w;
                const float minSharpness = _TaaParams_2.x;
                const float maxSharpness = _TaaParams_2.y;
                const float motionSharpeningFactor = _TaaParams_2.z;
                const float staticClipScale = _TaaParams_2.w;

                int2 prevCoord = int2(floor(prevUV.x * _ScreenSize.x), floor(prevUV.y * _ScreenSize.y));

                float4 prev;

                // if sharpness == .0f, we use bilinear resampling, otherwise we perform the 1-sample bicubic resampling
                if (resamplingSharpness == .0f) prev = SAMPLE_TEXTURE2D_LOD(_PrevTaaColorTex, sampler_linear_clamp, prevUV, 0);
                else {
                    prev = SAMPLE_TEXTURE2D_LOD(_PrevTaaColorTex, sampler_point_clamp, prevUV, 0);
                    prev.rgb = OneTapBicubicFilter(curr, c5, c6, c7, c8, prev.rgb, prevUV, resamplingSharpness);
                }

                // velocity weighting
                const float prevD0 = _PrevDepthTex.SampleLevel(sampler_point_clamp, prevUV, 0).r;
                const float prevD1 = _PrevDepthTex.SampleLevel(sampler_point_clamp, prevUV, 0, offset1).r;
                const float prevD2 = _PrevDepthTex.SampleLevel(sampler_point_clamp, prevUV, 0, offset2).r;
                const float prevD3 = _PrevDepthTex.SampleLevel(sampler_point_clamp, prevUV, 0, offset3).r;
                const float prevD4 = _PrevDepthTex.SampleLevel(sampler_point_clamp, prevUV, 0, offset4).r;

                float prevClosetDepth = prevD0;
                int2 prevClosetOffset = int2(0, 0);
                if (CLOSER_DEPTH(prevClosetDepth, prevD1)) {
                    prevClosetDepth = prevD1;
                    prevClosetOffset = offset1;
                }
                if (CLOSER_DEPTH(prevClosetDepth, prevD2)) {
                    prevClosetDepth = prevD2;
                    prevClosetOffset = offset2;
                }
                if (CLOSER_DEPTH(prevClosetDepth, prevD3)) {
                    prevClosetDepth = prevD3;
                    prevClosetOffset = offset3;
                }
                if (CLOSER_DEPTH(prevClosetDepth, prevD4)) {
                    prevClosetDepth = prevD4;
                    prevClosetOffset = offset4;
                }

                float2 prevMV = LOAD_TEXTURE2D(_PrevVelocityTex, prevCoord + prevClosetOffset).rg;
                float prevMVScale = length(prevMV);

                float mvScale = length(mv);

                float velocityWeight = saturate((length(prevMV - mv) - minVelocityRejection) * velocityRejectionScale);
                float clipScale = lerp(maxClipScale, minClipScale, velocityWeight);

                // return velocityWeight;

                // stencil test
                const uint st0 = LOAD_TEXTURE2D(_StencilTex, coord).STENCIL_CHANNEL & 3;
                const uint st1 = LOAD_TEXTURE2D(_StencilTex, coord1).STENCIL_CHANNEL & 3;
                const uint st2 = LOAD_TEXTURE2D(_StencilTex, coord2).STENCIL_CHANNEL & 3;
                const uint st3 = LOAD_TEXTURE2D(_StencilTex, coord3).STENCIL_CHANNEL & 3;
                const uint st4 = LOAD_TEXTURE2D(_StencilTex, coord4).STENCIL_CHANNEL & 3;
                const uint prevSt = LOAD_TEXTURE2D(_PrevStencilTex, prevCoord).STENCIL_CHANNEL & 3;

                bool atEdge = st0 != st1;
                atEdge = atEdge || st0 != st2;
                atEdge = atEdge || st0 != st3;
                atEdge = atEdge || st0 != st4;

                atEdge = atEdge || abs(linearDepth0 - linearDepth1) > minDepthRejection;
                atEdge = atEdge || abs(linearDepth0 - linearDepth2) > minDepthRejection;
                atEdge = atEdge || abs(linearDepth0 - linearDepth3) > minDepthRejection;
                atEdge = atEdge || abs(linearDepth0 - linearDepth4) > minDepthRejection;

                // return atEdge ? 1.0f : .0f;

                // depth test
                const float linearPrevDepth = PrevDepthToLinearEyeSpace(prevClosetDepth);

                bool mismatch = (st0 != prevSt) || (DEPTH_DIFF(closetLinearDepth, linearPrevDepth) > minDepthRejection);
                
                clipScale = mismatch ? minClipScale : clipScale;

                // count for imprecision
                bool staticPixel = mvScale < 2.0f * FLT_EPS && prevMVScale < 2.0f * FLT_EPS;
                // increase clip box scale when it is a static pixel and we detect a large different in depth/stencil either historically or spatially
                // we also need to make sure it is not covered by any transparent effect, in which case the MV computation is broken
                bool antiFlicker = staticPixel && (atEdge || prev.a < .5f) && curr.a > .99f;
                clipScale = antiFlicker ? staticClipScale : clipScale;

                // return antiFlicker ? 1.0f : .0f;
                // return mismatch ? 1.0f : .0f;
                // return (mismatch && !antiFlicker) ? 1.0f : .0f;

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

                // cross pattern sharperning
                float sharpnessFactor = lerp(maxSharpness, minSharpness, mvScale * motionSharpeningFactor);
                float3 corners = c1 + c2 + c3 + c4;
                curr.rgb = curr.rgb * (1.0 + 4.0f * sharpnessFactor) + corners * -sharpnessFactor;
                curr = clamp(curr, .0f, HALF_MAX);

                // decrease history weight when transparent pixel has a large opacity (motion vector is less reliable here)
                float historyWeight = curr.a > .999f ? maxHistoryWeight : lerp(maxHistoryWeight, minHistoryWeight, curr.a);

                output = lerp(curr, prev, historyWeight);
                // record antiFlicker history
                output.a = antiFlicker ? .0f : 1.0f;
                return output;
            }
            
            ENDHLSL
        }
    }
}