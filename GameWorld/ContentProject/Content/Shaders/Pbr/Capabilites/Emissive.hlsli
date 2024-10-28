#include "../Helpers/GradientSampling.hlsli"
#include "../TextureSamplers.hlsli"
#include "../helpers/MathFunctions.hlsli"

// Input parameters
bool                    CapabilityFlag_ApplyEmissive = false;

Texture2D<float4>       Emissive_Texture;
bool                    Emissive_UseTexture = true;

float3                  Emissive_Tint = float3(1, 1, 1);
float                   Emissive_Strength = 1;
float2                  Emissive_Tiling = float2(1, 1);
float3                  Emissive_GradientColours[4];
float                   Emissive_GradientTimes[4];
float                   Emissive_FresnelStrength = 1;

float3 GetEmissiveColour(float2 uv, float4 maskValue, float3 normalizedViewDirection, float3 normalizedNormal)
{
    if (CapabilityFlag_ApplyEmissive == false)
        return float3(0, 0, 0);
    
    float dotProduct = dot(normalizedViewDirection, normalizedNormal);
    float2 texCord = float2(nfmod(uv.x, Emissive_Tiling.x), nfmod(uv.y, Emissive_Tiling.y));
    
    float3 emissiveTexture = float3(1, 1, 1);
    if (Emissive_UseTexture)
        emissiveTexture = Emissive_Texture.Sample(SampleType, texCord);
    
    float fresnelTerm = pow(1.0 - dotProduct, Emissive_FresnelStrength);
    float3 gradientColour = SampleGradient(maskValue.a, Emissive_GradientColours, Emissive_GradientTimes);
    
    float3 emissiveColour = gradientColour * emissiveTexture * Emissive_Tint * (Emissive_Strength * 5) * fresnelTerm;
    return emissiveColour;
}

