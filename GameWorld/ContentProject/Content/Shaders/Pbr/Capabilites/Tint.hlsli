bool        CapabilityFlag_ApplyTint = false;

bool        Tint_UseFactionColours = false;
float3      Tint_FactionsColours[3] = { float3(1, 0, 0), float3(0, 1, 0), float3(0, 0, 1)};
float4      Tint_Mask = float4(0, 0, 0, 0);
float3      Tint_TintColour = float3(1, 1, 1);
float       Tint_TintVariation = 0;

float3 ApplyTintAndFactionColours(float3 textureDiffuse, float4 maskValue)
{    
    if (CapabilityFlag_ApplyTint == false)
        return textureDiffuse;
 
    float3 diffuseResult = textureDiffuse;
    if (Tint_UseFactionColours)
    {   
        diffuseResult = lerp(diffuseResult.rgb, diffuseResult.rgb * (Tint_FactionsColours[0]), maskValue.r);
        diffuseResult = lerp(diffuseResult.rgb, diffuseResult.rgb * (Tint_FactionsColours[1]), maskValue.g);
        diffuseResult = lerp(diffuseResult.rgb, diffuseResult.rgb * (Tint_FactionsColours[2]), maskValue.b);
    }
    
    float tintFactor = dot(Tint_Mask, maskValue);
    float3 tintColour = Tint_TintColour * Tint_TintVariation * tintFactor;
    
    return diffuseResult * tintColour;
}