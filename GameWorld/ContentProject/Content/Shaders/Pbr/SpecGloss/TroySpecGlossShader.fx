#include "../Helpers/CASpecglosshelper.hlsli"
#include "../helpers/constants.hlsli"
#include "../Helpers/tone_mapping.hlsli"

#include "../Shared/const_layout.hlsli"
#include "../Shared/MainVertexShader.hlsli"

#include "../TextureSamplers.hlsli"
#include "../inputlayouts.hlsli"



float3 getPixelNormal(PixelInputType input)
{
    //float3x3 basis = float3x3(normalize(input.tangent), normalize(input.normal), normalize(input.binormal));
    float3x3 basis = float3x3(normalize(input.tangent.xyz), normalize(input.binormal.xyz), normalize(input.normal.xyz)); // works in own shader

    float4 NormalTex = NormalTexture.Sample(s_normal, input.tex);

    float3 Np = 0;
    Np.x = NormalTex.r * NormalTex.a;
    Np.y = NormalTex.g;
    Np = (Np * 2.0f) - 1.0f;

    Np.z = sqrt(1 - Np.x * Np.x - Np.y * Np.y);

    return normalize(mul(Np.xyz, basis));
}

// -------------------------------------------------------------------------------------------------------------
//  Fetch Data needed to render 1 pixel
// -------------------------------------------------------------------------------------------------------------
GBufferMaterial GetMaterial(in PixelInputType input)
{
    GBufferMaterial material;

    // default values
    material.diffuse = float4(0.2f, 0.2f, 0.2f, 1);
    material.specular = float4(0, 0, 0, 1);
    material.roughness = 1.0f;
    material.metalness = 0.0f;
    material.pixelNormal = input.normal;
    
    float2 texCord = float2(nfmod(input.tex.x, 1), nfmod(input.tex.y, 1));
        
    if (UseSpecular)
    {
        material.specular = _linear(SpecularTexture.Sample(SampleType, texCord).rgb);
    }
    
    if (UseDiffuse)
    {
        float4 diffuseTexSample = DiffuseTexture.Sample(SampleType, texCord);
        material.diffuse.rgb = _linear(diffuseTexSample.rgb);
        material.diffuse.a = diffuseTexSample.a;
    }
    
    if (UseGloss)
    {
        float4 glossTex = GlossTexture.Sample(SampleType, texCord);
        material.roughness = saturate((1 - glossTex.r));
    }
    
    if (UseNormal)
    {
        material.pixelNormal = getPixelNormal(input);
    }
    
    return material;
}


float4 mainPS(in PixelInputType input, bool bIsFrontFace : SV_IsFrontFace) : SV_TARGET0
{    
    // -- fill material,
    GBufferMaterial material = GetMaterial(input);

    if (UseAlpha == 1)    
        alpha_test(material.diffuse.a);
    
    float3 normalizedViewDirection = -normalize(input.viewDirection);
    float3 rotatedNormalizedLightDirection = normalize(mul(light_Direction_Constant, (float3x3) DirLightTransform));
    
    float occlusion = 1.0f;
    float shadow = 1.0f;
	//  Create the standard material...       
    R2_4_StandardLightingModelMaterial standard_mat = R2_4_create_standard_lighting_material(
        material.diffuse.rgb,
        material.specular.rgb,
        material.pixelNormal.rgb,
        1.0 - material.roughness,
        float4(input.worldPosition, 0),
        shadow,
        occlusion);    


    float3 hdr_linear_col = standard_lighting_model_directional_light(
        float3(1, 1, 1),
        rotatedNormalizedLightDirection,
        normalizedViewDirection,
        standard_mat
    );

	//  Tone-map the pixel...
//    float3 ldr_linear_col = (saturate(tone_map_linear_hdr_pixel_value(1.3 * hdr_linear_col)));
    //float3 ldr_linear_col = saturate(tone_map_linear_hdr_pixel_value(hdr_linear_col));
        //  Tone-map the pixel...            
    float3 ldr_linear_col = saturate(Uncharted2ToneMapping(hdr_linear_col));

//----------------------------------------------------------

	//  Return gamma corrected value...
    return float4( /*_gamma*/(ldr_linear_col), 1.0f);
    //return float4( /*_gamma*/(hdr_linear_col), 1.0f);

}


technique BasicColorDrawing
{
    pass P0
    {
        VertexShader = compile vs_5_0 MainVertexShader();
        PixelShader = compile ps_5_0 mainPS();
    }
};