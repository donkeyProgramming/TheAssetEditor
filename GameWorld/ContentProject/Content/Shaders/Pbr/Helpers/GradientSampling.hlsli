#ifndef GRADIENTSAMPLING_HLSLI
#define GRADIENTSAMPLING_HLSLI


float3 SampleGradient_old(float t)
{
    
    float3 colors[4] = { float3(0, 0, 0), float3(1, 0.200000003, 0), float3(0.670000017, 0.670000017, 0.670000017), float3(1, 1, 1) };
    float times[4] = { 0, 0.330000013, 0.670000017, 1 };
    
    
    if (t <= times[0])
        return colors[0];
    if (t >= times[3])
        return colors[3];

    // Binary search for the segment
    int left = 0;
    int right = 3;
    while (left < right - 1)
    {
        int mid = (left + right) / 2;
        if (times[mid] <= t)
            left = mid;
        else
            right = mid;
    }

    // Normalize t within the segment
    float localT = (t - times[left]) / (times[right] - times[left]);

    return lerp(colors[left], colors[right], localT);
}

float3 SampleGradient(float t, float3 colors[4], float times[4])
{
    if (t <= times[0])
        return colors[0];
    if (t >= times[3])
        return colors[3];

    // Binary search for the segment
    int left = 0;
    int right = 3;
    while (left < right - 1)
    {
        int mid = (left + right) / 2;
        if (times[mid] <= t)
            left = mid;
        else
            right = mid;
    }

    // Normalize t within the segment
    float localT = (t - times[left]) / (times[right] - times[left]);

    return lerp(colors[left], colors[right], localT);
}

#endif