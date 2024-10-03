#include "../helpers/mathfunctions.hlsli"

// Input parameters 
bool CapabilityFlag_ApplyTinting = true; // PHAZER: dx11 global non-static consts are = 0, so values are not used
bool Tint_UseFactionColours = false;
bool Tint_UseTinting = true;
float3 Tint_FactionsColours[3] = { float3(1, 0, 0), float3(0, 1, 0), float3(0, 0, 1) };
float4 Tint_Mask = float4(0, 0, 0, 0);
float3 Tint_TintColours[3] = { { 0.59, 0.56, 0.48 }, { 0.33, 0.29, 0.43 }, { 0.38, 0.48, 0.36 } };
float Tint_TintVariation = 0;

float3 ApplyTintAndFactionColours(float3 textureDiffuse, float4 maskValue)
{        
    if (CapabilityFlag_ApplyTinting == false)
        return textureDiffuse;
    
    float3 diffuseResult = textureDiffuse;
    if (Tint_UseFactionColours)
    {
        diffuseResult = lerp(diffuseResult.rgb, diffuseResult.rgb * (Tint_FactionsColours[0]), maskValue.r);
        diffuseResult = lerp(diffuseResult.rgb, diffuseResult.rgb * (Tint_FactionsColours[1]), maskValue.g);
        diffuseResult = lerp(diffuseResult.rgb, diffuseResult.rgb * (Tint_FactionsColours[2]), maskValue.b);
    }
    
    // mix tinting into the result?
    if (Tint_UseTinting)    
    {            
        diffuseResult = lerp(diffuseResult.rgb, diffuseResult.rgb * _linear(Tint_TintColours[0]), maskValue.r);
        diffuseResult = lerp(diffuseResult.rgb, diffuseResult.rgb * _linear(Tint_TintColours[1]), maskValue.g);
        diffuseResult = lerp(diffuseResult.rgb, diffuseResult.rgb * _linear(Tint_TintColours[2]), maskValue.b);        
    }       
    
    return diffuseResult;
}