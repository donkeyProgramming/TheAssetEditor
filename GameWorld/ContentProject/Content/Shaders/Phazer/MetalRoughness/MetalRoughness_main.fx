﻿#include "../helpers/CALib.hlsli"
#include "../helpers/tone_mapping.hlsli"
#include "../helpers/constants.hlsli"
#include "../helpers/GradientSampling.hlsli"

#include "../Shared/MainVertexShader.hlsli"
#include "../Shared/const_layout.hlsli"

#include "../TextureSamplers.hlsli"
#include "../inputlayouts.hlsli"

#include "Helpers.hlsli"

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
    material.maskValue = float4(0,0,0,0);

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
        material.metalness = glossTexSample.r; // metal mask channel    
        material.roughness = pow(glossTexSample.g, 2.2f); // roughness channel
    }	
    
    if (UseNormal)
    {        
        material.pixelNormal = GetPixelNormal(input);
    }	
    
    if (UseMask)
    {
        material.maskValue = MaskTexture.Sample(SampleType, texCord).a;
    }
    
    return material;
}

float4 DefaultPixelShader(in PixelInputType input, bool bIsFrontFace : SV_IsFrontFace) : SV_TARGET0
{
    GBufferMaterial material = GetMaterial(input);        

    float3 envColor = getAmbientLight(
		material.pixelNormal,
		normalize(input.viewDirection),
		material.diffuse.rgb,// * TintColour,
		material.roughness,
		material.metalness,
		float4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1));
	
    float3 rot_lightDir = normalize(mul(light_Direction_Constant, (float3x3) DirLightTransform));                         	
	
	// directional ligting color    
    float3 dirColor = getDirectionalLight(
		material.pixelNormal,
		material.diffuse.rgb,// * TintColour,
		material.roughness,
		material.metalness,
		normalize(input.viewDirection),
		rot_lightDir
	);

	// add environent (ambient) light color and directional light color
    float3 color = envColor + dirColor;
    
    // Apply emissive 
    float3 emissiveColour = SampleGradient(material.maskValue.a) * 3;
    color += emissiveColour;// * material.maskValue;
	
	if (UseAlpha == 1)
	{
        alpha_test(material.diffuse.a);
    }
    
    return DoToneMapping(color.rgb);
}





float4 EmissiveLayerPixelShader(in PixelInputType input, bool bIsFrontFace : SV_IsFrontFace) : SV_TARGET0
{
    //return float4(1, 0, 0, 1);
   GBufferMaterial material = GetMaterial(input);
   
   // Apply emissive 
   float3 emissiveColour = SampleGradient(material.maskValue.a) * 3;
	
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