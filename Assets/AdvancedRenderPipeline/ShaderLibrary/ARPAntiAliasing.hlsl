#ifndef ARP_ANTI_ALIASING_INCLUDED
#define ARP_ANTI_ALIASING_INCLUDED

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

#endif