#include "../Helpers/CALib.hlsli"
#include "../helpers/constants.hlsli"
#include "../Helpers/tone_mapping.hlsli"
#include "../Helpers/GPUSkinning.hlsli" 

#include "../Shared/const_layout.hlsli"
#include "../Shared/AnimationConstants.hlsli"
#include "../Shared/MainVertexShader.hlsli"

#include "../TextureSamplers.hlsli"
#include "../inputlayouts.hlsli"


#include "Helpers.hlsli"



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
        material.pixelNormal = GetPixelNormal(input);
    }
    
    return material;
}





float4 mainPS(in PixelInputType input, bool bIsFrontFace : SV_IsFrontFace) : SV_TARGET0
{
    // -- fill material,
    GBufferMaterial material = GetMaterial(input);      

    // -- Get Directional LIght	
    float3 directionalLightColor = GetDirectionalLight_SpecGloss(
        material.pixelNormal, 
        material.diffuse.rgb, 
        material.specular, 
        material.roughness, 
        input.viewDirection);
	
	// -- Get Ambient Light
    float3 ambientLighting = GetAmbientlLight_SpecGloss(
        material.pixelNormal, 
        material.diffuse.rgb, 
        material.specular, 
        material.roughness, 
        input.viewDirection);
                             
    float3 color = ambientLighting + directionalLightColor;
	
	if (UseAlpha == 1)
	{
        alpha_test(material.diffuse.a);
    }
		
    return DoToneMapping(color.rgb);
}


technique BasicColorDrawing
{
    pass P0
    {
        VertexShader = compile vs_5_0 MainVertexShader();
        PixelShader = compile ps_5_0 mainPS();
    }
};