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
    
    if (Tint_UseFactionColours)
    {   
        textureDiffuse.r = lerp(textureDiffuse.r, textureDiffuse.r * (Tint_FactionsColours[0].r), maskValue.r);
        textureDiffuse.g = lerp(textureDiffuse.g, textureDiffuse.g * (Tint_FactionsColours[1].g), maskValue.g);
        textureDiffuse.b = lerp(textureDiffuse.b, textureDiffuse.b * (Tint_FactionsColours[2].b), maskValue.b);
    }
    
    float tintFactor = dot(Tint_Mask, maskValue);
    float3 tintColour = Tint_TintColour * Tint_TintVariation * tintFactor;
    
    return textureDiffuse * tintColour;
}