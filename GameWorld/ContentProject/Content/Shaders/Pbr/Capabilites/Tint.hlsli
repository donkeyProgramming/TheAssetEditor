#include "../helpers/mathfunctions.hlsli"

// Input parameters 
bool CapabilityFlag_ApplyTinting;
bool Tint_UseFactionColours;
bool Tint_UseTinting;
float3 Tint_FactionsColours[3];
float3 Tint_TintColours[3];
float4 Tint_Mask;
float Tint_TintVariation;

float3 ApplyTintAndFactionColours(float3 textureDiffuse, float4 maskValue)
{        
    if (CapabilityFlag_ApplyTinting == false)
        return textureDiffuse;
    
    float3 diffuseResult = textureDiffuse;
    if (Tint_UseFactionColours)
    {
        diffuseResult = lerp(diffuseResult.rgb, diffuseResult.rgb * _linear(Tint_FactionsColours[0]), maskValue.r);
        diffuseResult = lerp(diffuseResult.rgb, diffuseResult.rgb * _linear(Tint_FactionsColours[1]), maskValue.g);
        diffuseResult = lerp(diffuseResult.rgb, diffuseResult.rgb * _linear(Tint_FactionsColours[2]), maskValue.b);
    }    
    
    if (Tint_UseTinting) // allow tinting of faction colored pixel
    {
        diffuseResult = lerp(diffuseResult.rgb, diffuseResult.rgb * _linear(Tint_TintColours[0]), maskValue.r);
        diffuseResult = lerp(diffuseResult.rgb, diffuseResult.rgb * _linear(Tint_TintColours[1]), maskValue.g);
        diffuseResult = lerp(diffuseResult.rgb, diffuseResult.rgb * _linear(Tint_TintColours[2]), maskValue.b);
    }
    
    return diffuseResult;
}