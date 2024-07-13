/*                                                                                                              z
MIT License

Copyright Phazer(c) 2022-2024 

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
*/

/********************************************************************
    Uses code and concepts from 
    https://github.com/Nadrin/PBR/
    
    Original License Included: License_Nadrin_Pbr.txt
********************************************************************/                                                         

#include "const_layout.hlsli"                 
#include "GPUSkinning.hlsli" 
#include "TextureSamplers.hlsli"
#include "inputlayouts.hlsli"
#include "pbr_lib.hlsli"
#include "CALib.hlsli"
#include "tone_mapping.hlsli"
																					 
// or use your camera class to controll the direction light = "orbital light control"
static const float3 light_Direction_Constant = normalize(float3(0.1, 0.1, 1.f));

// light strengths, dial from 0 - X
static const float Directional_Light_Raddiance = 5.0f;
static const float Ambient_Light_Raddiance = 1.0;

// colors for lighting, set a color almost white, with a TINY blue tint to change mood in scene
// typically these two are simply te same color, unless one wanted to simular artifical light (like a lamp/flashlight etc)
static float3 Directional_Light_color = float3(1, 1, 1);
static float3 Ambient_Light_color = float3(1, 1, 1);

// -------------------------------------------------------------------------------------
//  Vertex Shader MAIN
// -------------------------------------------------------------------------------------
PixelInputType mainVS(in VertexInputType input) // main is the default function name
{
    PixelInputType output = (PixelInputType) 0;

    DoSkinning(input, WeightCount, output.position, output.normal, output.tangent, output.binormal);

    output.position = mul(output.position, World);
    output.worldPosition = output.position.xyz;
    output.position = mul(output.position, View);
    output.position = mul(output.position, Projection);

    output.tex = input.tex;

    output.normal = normalize(mul(output.normal, (float3x3) World));
    output.tangent = normalize(mul(output.tangent, (float3x3) World));
    output.binormal = normalize(mul(output.binormal, (float3x3) World));

	// Calculate the position of the vertex in the world.
    output.viewDirection = normalize(CameraPos - output.worldPosition);
    
    return output;
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
        material.pixelNormal = GetPixelNormal(input);
    }
    
    return material;
}

// ------------------------------------------------------------------------------------------------------------------------------ 
//  Directional Light 
// ------------------------------------------------------------------------------------------------------------------------------ 
float3 GetDirectionalLight_SpecGloss(in float3 N, in float3 diffuse, in float3 specular, in float roughness, in float3 viewDir)
{                                                                                   
    float3 Lo = normalize(viewDir);

	// Angle between surface normal and outgoing light direction.
    float cosLo = max(0.0, dot(N, Lo));

	// Specular reflection vector.
    float3 Lr = 2.0 * cosLo * N - Lo;

	// Fresnel reflectance at normal incidence (for metals use albedo color).
    float3 F0 = specular;

	// Direct lighting calculation for analytical lights.
    float3 directLighting = 0.0;

	//float3 Li = normalize(-LightData[0].lightDirection);
    float3 Li = normalize(mul(light_Direction_Constant, (float3x3) DirLightTransform));
	//float3 Lradiance = LightData[0].radiannce;
    float3 Lradiance = Directional_Light_Raddiance * LightMult;

	// Half-vector between Li and Lo.
    float3 Lh = normalize(Li + Lo);

	// Calculate angles between surface normal and various light vectors.
    float cosLi = max(0.0, dot(N, Li));
    float cosLh = max(0.0, dot(N, Lh));

	// Calculate Fresnel term for direct lighting. 
    float3 F = fresnelSchlick(F0, max(0.0, dot(Lh, Lo)));

	// Calculate normal distribution for specular BRDF.
    float D = ndfGGX(cosLh, roughness);
    
	// Calculate geometric attenuation for specular BRDF.
    float G = gaSchlickGGX(cosLi, cosLo, roughness);
    
	// Diffuse scattering happens due to light being refracted multiple times by a dielectric medium.
	// Metals on the other hand either reflect or absorb energy, so diffuse contribution is always zero.
	// To be energy conserving we must scale diffuse BRDF contribution based on Fresnel factor & metalness.
    float3 dlightMaterialScattering = lerp(float3(1, 1, 1) - F, float3(0, 0, 0), saturate(specular));    
    
	// Lambert diffuse BRDF.
	// We don't scale by 1/PI for lighting & material units to be more convenient.
	// See: https://seblagarde.wordpress.com/2012/01/08/pi-or-not-to-pi-in-game-lighting-equation/	
    float3 diffuseBRDF = diffuse * dlightMaterialScattering; 	
	
	// Cook-Torrance specular microfacet BRDF.
    float3 specularBRDF = (F * D * G) / max(Epsilon, 4.0 * cosLi * cosLo);

	// Total contribution for this light.
    directLighting += (diffuseBRDF + specularBRDF) * Lradiance * cosLi * Directional_Light_color;

    return directLighting;
}

float3 GetAmbientlLight_SpecGloss(in float3 N, in float3 diffuse, in float3 specular, in float roughness, in float3 viewDirection)
{    	
    float3 Lo = normalize(viewDirection);

	// Angle between surface normal and outgoing light direction.
    float cosLo = max(0.0, dot(N, Lo));

	// Specular reflection vector.	
    float3 Lr = reflect(N, Lo); // HLSL intrisic reflection function

	// specular
    float3 F0 = specular;
	
    // rotate env
    float3 rot_lightDir = normalize(mul(light_Direction_Constant, (float3x3) DirLightTransform));
    
    float3 bumpNormal_Rot = mul(N, (float3x3) EnvMapTransform);
    bumpNormal_Rot = normalize(bumpNormal_Rot);
    float3 irradiance = tex_cube_diffuse.Sample(SampleType, bumpNormal_Rot).rgb;
    
    //float3 F = fresnelSchlick(F0, cosLo);        
    float3 F = fresnelSchlickRoughness(cosLo, F0, roughness);    
    float3 kS = F;                                               
    float3 kD = saturate(1.0 - kS);    
    
    float3 diffuseIBL = kD * diffuse * irradiance;    
    
	// rotate refletion map by rotating the reflect vector
    float3 Lr_Rot = mul(Lr, (float3x3) EnvMapTransform);
    float3 specularIrradiance = sample_environment_specular(roughness, normalize(Lr_Rot));

    float2 brdf = specularBRDF_LUT.Sample(spBRDF_Sampler, float2(cosLo, (1.0 - roughness))).xy;
    float3 specularIBL = (brdf.x * F0 + brdf.y) * specularIrradiance;       
        
    float3 ambientLighting = (specularIBL + diffuseIBL) * Ambient_Light_Raddiance; // * light[0].ambientFactor;    
            
    return ambientLighting;
}

float4 DoToneMapping(float3 hdrColor, float exposureFactor = 1.0, float gamma_value= 2.2)
{
    float3 hdrColorExposed = hdrColor.rgb * exposure * exposureFactor;	
    float3 toneMappedColor = Uncharted2ToneMapping(hdrColorExposed);
    float3 gammmaCorrectedColor = pow(toneMappedColor, 1.0 / gamma_value);                 

    return float4(gammmaCorrectedColor.rgb, 1);
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

float4 SimplePixel(in PixelInputType _input /*, bool bIsFrontFace : SV_IsFrontFace*/) : SV_TARGET0
{
    return float4(1, 0, 0, 1);
}

technique BasicColorDrawing
{
    pass P0
    {
        VertexShader = compile vs_5_0 mainVS();
        PixelShader = compile ps_5_0 mainPS();
    }
};