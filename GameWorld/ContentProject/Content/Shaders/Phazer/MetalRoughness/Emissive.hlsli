#include "../Helpers/GradientSampling.hlsli"

struct EmissiveGradientValue
{
    float3 Colour;
    float Time;
};

// Input parameters
bool CapabilityFlag_ApplyEmissive = false;

float3 Emissive_Tint = float3(0, 0, 0);
float Emissive_Strength = 1;
float2 Emissive_Tiling = float2(1, 1);
EmissiveGradientValue Emissive_GradientColour[4];


float3 GetEmissiveColour(float3 maskValue)
{
    if (CapabilityFlag_ApplyEmissive == false)
        return float3(0, 0, 0);
    
    float3 emissiveColour = SampleGradient(mask) * 3;
    return emissiveColour;
}

