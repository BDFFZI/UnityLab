float2 ParallaxMapping(float3 viewDirTS, sampler2D heightMap, float2 uv, float scale)
{
    float height = tex2D(heightMap, uv).r - 0.5;
    float2 parallaxOffset = height * scale * viewDirTS.xy;
    return parallaxOffset;
}
float2 SteepParallaxMapping(int step, float3 viewDirTS, sampler2D heightMap, float2 uv, float scale)
{
    float2 basicsOffset = scale * viewDirTS.xy;

    for (int i = step; i >= 0; i--)
    {
        float estimatedHeight = 1.0f / step * i - 0.5;
        float2 parallaxOffset = estimatedHeight * basicsOffset;
        float practicalHeight = tex2D(heightMap, uv + parallaxOffset).r - 0.5;
        if (practicalHeight >= estimatedHeight)
            return parallaxOffset;
    }
    return 0;
}
float2 ParallaxMaskingMapping(int step, float3 viewDirTS, sampler2D heightMap, float2 uv, float scale)
{
    float2 basicsOffset = scale * viewDirTS.xy;

    float lastEstimatedHeight = 1.0f - 0.5;
    float lastPracticalHeight = tex2D(heightMap, uv + lastEstimatedHeight * basicsOffset).r - 0.5;

    for (int i = step - 1; i >= 0; i--)
    {
        float estimatedHeight = 1.0f / step * i - 0.5;
        float practicalHeight = tex2D(heightMap, uv + estimatedHeight * basicsOffset).r - 0.5;
        if (practicalHeight >= estimatedHeight)
        {
            float ac = practicalHeight - estimatedHeight;
            float bd = lastEstimatedHeight - lastPracticalHeight;
            float ef = lastEstimatedHeight - estimatedHeight;
            float eo = ac * ef / (bd + ac);

            return (estimatedHeight + eo) * basicsOffset;
        }

        lastEstimatedHeight = estimatedHeight;
        lastPracticalHeight = practicalHeight;
    }
    return 0;
}
float2 ParallaxMaskingMappingDepth(int step, float3 viewDirTS, sampler2D depthMap, float2 uv, float scale)
{
    float2 basicsOffset = scale * -viewDirTS.xy;

    float lastEstimatedDepth = 0.0f;
    float lastPracticalDepth = tex2D(depthMap, uv).r;

    for (int i = 1; i <= step; i++)
    {
        float estimatedDepth = 1.0f / step * i;
        float practicalDepth = tex2D(depthMap, uv + estimatedDepth * basicsOffset).r;
        if (practicalDepth <= estimatedDepth)
        {
            float ac = estimatedDepth - practicalDepth;
            float bd = lastPracticalDepth - lastEstimatedDepth;
            float ef = estimatedDepth - lastEstimatedDepth;
            float eo = bd * ef / (bd + ac);

            return (estimatedDepth + eo) * basicsOffset;
        }

        lastEstimatedDepth = estimatedDepth;
        lastPracticalDepth = practicalDepth;
    }
    return 0;
}