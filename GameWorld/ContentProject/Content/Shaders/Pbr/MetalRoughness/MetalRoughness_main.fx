#include "../helpers/CAMetalRoughnessHelper.hlsli"
#include "../helpers/tone_mapping.hlsli"
#include "../helpers/constants.hlsli"
#include "../helpers/GradientSampling.hlsli"

#include "../Shared/MainVertexShader.hlsli"
#include "../Shared/const_layout.hlsli"

#include "../TextureSamplers.hlsli"
#include "../inputlayouts.hlsli"

#include "../Capabilites/Emissive.hlsli"
#include "../Capabilites/Tint.hlsli"

//#include "Helpers.hlsli"

// **************************************************************************************************************************************
// *		PIXEL SHADER CODE
// **************************************************************************************************************************************
GBufferMaterial GetMaterial(in PixelInputType input)
{
    GBufferMaterial material;
    
    // default values
    material.diffuse = float4(0.2f, 0.2f, 0.2f, 1);
    material.specular = float4(0, 0, 0, 0);
    material.roughness = 1.0f;
    material.metalness = 0.0f;
    material.pixelNormal = input.normal;
    material.maskValue = float4(0, 0, 0, 0);

    float2 texCord = float2(nfmod(input.tex.x, 1), nfmod(input.tex.y, 1));
    
    if (UseSpecular)
    {
        material.specular.rgb = _linear(SpecularTexture.Sample(SampleType, texCord).rgb);
    }
    
    if (UseDiffuse)
    {
        float4 diffuseValue = DiffuseTexture.Sample(SampleType, texCord);
        material.diffuse.rgb = _linear(diffuseValue.rgb);
        material.diffuse.a = diffuseValue.a;
    }
    
    if (UseGloss)
    {
        float4 glossTexSample = GlossTexture.Sample(SampleType, texCord);
        material.metalness = (glossTexSample.r);
        material.roughness = (glossTexSample.g);

    }
    
    if (UseNormal)
    {
        material.pixelNormal = GetPixelNormal(input);
    }
    
    if (UseMask)
    {
        material.maskValue = MaskTexture.Sample(SampleType, texCord);
    }
    
    return material;
}

float4 DefaultPixelShader(in PixelInputType input, bool bIsFrontFace : SV_IsFrontFace) : SV_TARGET0
{    
    GBufferMaterial material = GetMaterial(input);        
    
    if (UseAlpha == 1)    
        alpha_test(material.diffuse.a);    
    
    //float3 normalizedViewDirection = -normalize(input.viewDirection);    
    
    float3 normalizedViewDirection = -normalize(CameraPos - input.worldPosition);    
    float3 rotatedNormalizedLightDirection = normalize(mul(light_Direction_Constant, (float3x3) DirLightTransform));    
               
    const float occlusion = 1.0f; // no SSAO yet = no occlusion
    
    //  Create the standard material.  This is what gets written to the gbuffer...
    R2_5_StandardLightingModelMaterial_For_GBuffer standard_mat_compressed = 
        R2_5_create_standard_lighting_material_for_gbuffer(
        material.diffuse.rgb,
        material.pixelNormal,
        material.roughness, 
        material.metalness,
        occlusion);    
    
	//	Create the uncompressed material.  This is what is read from the gbuffer...
    R2_5_StandardLightingModelMaterial_For_Lighting slm_uncompressed = R2_5_get_slm_for_lighting(standard_mat_compressed);

	//	Apply faction colours...    
    slm_uncompressed.Diffuse_Colour.rgb = ApplyTintAndFactionColours(slm_uncompressed.Diffuse_Colour.rgb, material.maskValue);

    float unchartedSunFactor = 3.0f;
    
    //  Light the pixel...    
    float3 hdr_linear_col = standard_lighting_model_directional_light(get_sun_colour() * unchartedSunFactor, rotatedNormalizedLightDirection, normalizedViewDirection, slm_uncompressed);

    //  Tone-map the pixel...            
    //float3 ldr_linear_col = saturate(tone_map_linear_hdr_to_linear_ldr_reinhard(hdr_linear_col));    
    float3 ldr_linear_col = saturate(Uncharted2ToneMapping(hdr_linear_col));    
    
    float3 emissiveColour = GetEmissiveColour(input.tex, material.maskValue, rotatedNormalizedLightDirection, material.pixelNormal);
    
    // Combine all colours
    float3 color = ldr_linear_col + emissiveColour;	
    
    return float4(_gamma(color), 1.0f);
}

float4 EmissiveLayerPixelShader(in PixelInputType input, bool bIsFrontFace : SV_IsFrontFace) : SV_TARGET0
{
    GBufferMaterial material = GetMaterial(input);
    float3 normlizedViewDirection = normalize(input.viewDirection);
    float3 emissiveColour = GetEmissiveColour(input.tex, material.maskValue, normlizedViewDirection, material.pixelNormal);
	
    if (UseAlpha == 1)
        alpha_test(material.diffuse.a);
   
    return float4(emissiveColour, 1);
}

technique BasicColorDrawing
{
    pass P0
    {
        VertexShader = compile vs_5_0 MainVertexShader();
        PixelShader = compile ps_5_0 DefaultPixelShader();
    }
};

technique GlowDrawing
{
    pass P0
    {
        VertexShader = compile vs_5_0 MainVertexShader();
        PixelShader = compile ps_5_0 EmissiveLayerPixelShader();
    }
};