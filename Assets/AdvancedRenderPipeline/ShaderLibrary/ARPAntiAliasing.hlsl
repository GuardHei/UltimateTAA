#ifndef ARP_ANTI_ALIASING_INCLUDED
#define ARP_ANTI_ALIASING_INCLUDED

float3 ClipAABB(float3 aabbMin, float3 aabbMax, float3 prev) {
    float3 center = .5f * (aabbMax + aabbMin);
    float3 extents = .5f * (aabbMax - aabbMin);
    float3 dist = prev - center;
    float3 ts = abs(extents / (dist + .0001f));
    float t = saturate(min(ts.x, min(ts.y, ts.z)));
    float3 result = center + dist * t;
    return result;
}

#endif