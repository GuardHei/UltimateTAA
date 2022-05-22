#ifndef ARP_ANTI_ALIASING_INCLUDED
#define ARP_ANTI_ALIASING_INCLUDED

float3 OneTapBicubicFilter(float3 center, float3 top, float3 right, float3 bottom, float3 left, float3 prevCenter, float2 uv, float sharpness) {
    float2 f = frac(_ScreenSize.xy * uv - .5f);
    float c = .8f * sharpness;
    float2 w = c * (f * f - f);
    float4 color = float4(lerp(left, right, f.x), 1.0f) * w.x;
    color += float4(lerp(top, bottom, f.y), 1.0f) * w.y;
    color += float4((1.0f + color.a) * prevCenter - color.a * center, 1.0f);
    return color.rgb * rcp(color.a);
}

float3 AdaptiveGaussianBlur(float3 center, float3 cross, float3 corners, float centerWeight) {
    float secondaryWeight = (1.0f - centerWeight) * .2f;
    float tertiaryWeight = secondaryWeight * .25f;
    return center * centerWeight + cross * secondaryWeight + corners * tertiaryWeight;
}

float3 AdaptiveSharpening(float3 center, float3 cross, float sharpnessFactor) {
    return clamp(center * (1.0f + 4.0f * sharpnessFactor) + cross * -sharpnessFactor, .0f, HALF_MAX);
}

#define AABB_TEST 0

float3 ClipAABB(float3 aabbMin, float3 aabbMax, float3 prev) {
#if AABB_TEST == 0
    float3 center = .5f * (aabbMax + aabbMin);
    float3 extents = .5f * (aabbMax - aabbMin);
    float3 dist = prev - center;
    float3 ts = abs(extents / (dist + .0001f));
    float t = saturate(min(ts.x, min(ts.y, ts.z)));
    float3 result = center + dist * t;
    return result;
#else
    float3 p_clip = .5f * (aabbMax + aabbMin);
    float3 e_clip = .5f * (aabbMax - aabbMin) + .0001f;
    float3 v_clip = prev - p_clip;
    float3 v_unit = v_clip / e_clip;
    float3 a_unit = abs(v_unit);
    float ma_unit = max(a_unit.x, max(a_unit.y, a_unit.z));

    return ma_unit > 1.0f ? p_clip + v_clip / ma_unit : prev;
#endif
}

float3 ClipVariance(float3 m1, float3 m2, float n, float gamma, float3 prev) {
    float3 mu = m1 / n;
    float3 sigma = sqrt(abs(m2 / n - mu * mu));
    sigma *= gamma;

    float3 minColor = mu - sigma;
    float3 maxColor = mu + sigma;

    float3 clipped = ClipAABB(minColor, maxColor, prev);
    return clipped;
}

float DepthVariance(float m1, float m2, float n, float curr, float prev) {
    float mu = m1 / n;
    float variance = abs(mu * mu - (m2 / n));
    float k = abs(prev - curr);
    float chebyshevWeight = variance / (variance + (k * k));

    return saturate(chebyshevWeight);
}

#endif