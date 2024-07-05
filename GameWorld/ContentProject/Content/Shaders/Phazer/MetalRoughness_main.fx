/*******************************************************************************
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

********************************************************************************/

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

// Direction for directional lighting source
static const float3 light_Direction_Constant = normalize(float3(0.1, 0.1, 1.f));

// directional light strength, dial from 0 - X
static const float Directional_Light_Raddiance = 5.0f;

// ambient light strength
static const float Ambient_Light_Raddiance = 1.0f;

// colors for lighting, set a color almost white, with a TINY blue tint to change mood in scene
// typically these two are simply te same color, unless one wanted to simular artifical light (like a lamp/flashlight etc)
static const float3 Directional_Light_color = float3(1, 1, 1);
static const float3 Ambient_Light_color = float3(1, 1, 1);

// **************************************************************************************************************************************
// *		VERTEX SHADER CODE
// **************************************************************************************************************************************

// -------------------------------------------------------------------------------------
//		Vertex Shader Main
// -------------------------------------------------------------------------------------
PixelInputType MainVS(in VertexInputType input) // main is the default function name
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

// **************************************************************************************************************************************
// *		PIXEL SHADER CODE
// **************************************************************************************************************************************


// --------------------------------------------------------------------------------------------------------------------------------------
//  Returns Directional Light Color
// --------------------------------------------------------------------------------------------------------------------------------------            
float3 getDirectionalLight(in float3 N, in float3 albedo, in float roughness, in float metalness, in float3 viewDir, in float3 lightDir)
{
    float3 Lo = normalize(viewDir);

	// Angle between surface normal and outgoing light direction.
    float cosLo = max(0.0, dot(N, Lo));

	// Specular reflection vector.
    float3 Lr = 2.0 * cosLo * N - Lo;

	// Fresnel reflectance at normal incidence (for metals use albedo color).
    float3 F0 = lerp(Fdielectric, albedo, metalness);

	// Direct lighting calculation for analytical lights.
    float3 directionalLightColor = 0.0;
    	
    float3 Li = normalize(lightDir);	
    float3 Lradiance = Directional_Light_Raddiance * LightMult;
		
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
    float3 kd = lerp(float3(1, 1, 1) - F, float3(0, 0, 0), metalness);

	// Lambert diffuse BRDF.
	// We don't scale by 1/PI for lighting & material units to be more convenient.
	// See: https://seblagarde.wordpress.com/2012/01/08/pi-or-not-to-pi-in-game-lighting-equation/
    float3 diffuseBRDF = kd * albedo;

	// Cook-Torrance specular microfacet BRDF.    
    float3 specularBRDF = (F * D * G) / max(Epsilon, 4.0 * cosLi * cosLo);
	
    // Total contribution for this directional light.
    directionalLightColor += (diffuseBRDF + specularBRDF) * Lradiance * cosLi * Directional_Light_color;
                                              
    return directionalLightColor;
}

// --------------------------------------------------------------------------------------------------------------------------------------
//  Returns Ambient Light Color
// --------------------------------------------------------------------------------------------------------------------------------------            
float3 getAmbientLight(in float3 normal1, in float3 viewDir, in float3 Albedo,	in float roughness,	in float metalness,	float4x4 envTransform)
{   
    float3 Lo = normalize(viewDir);

	// Angle between surface normal and outgoing light direction.
    float cosLo = max(0.0, dot(normal1, Lo));

	// Specular reflection vector.	
    float3 Lr = reflect(normal1, Lo); // HLSL intrisic reflection function

	// Fresnel reflectance at normal incidence (for metals use albedo color).
    float3 F0 = lerp(Fdielectric, Albedo.rgb, metalness);

	// Calculate Fresnel term for ambient lighting.	            
    float3 F = fresnelSchlick(F0, cosLo);

	// Get diffuse contribution factor (as with direct lighting).
    float3 kd = lerp(1.0 - F, 0.0, metalness);
        	
	// the environment rotated normals    
    float3 diffuseRotatedEnvNormal = normalize(mul(normal1, (float3x3) EnvMapTransform));
    float3 specularRotatedEnvNormal = normalize(mul(Lr, (float3x3) EnvMapTransform));    

	//float3 irradiance = tex_cube_diffuse.Sample(SampleType, normal).rgb;
    float3 irradiance = tex_cube_diffuse.Sample(SampleType, diffuseRotatedEnvNormal).rgb;
    float3 diffuseIBL = kd * Albedo.rgb * irradiance;

    float3 specularIrradiance = sample_environment_specular(roughness, normalize(specularRotatedEnvNormal));                                                       

	// Split-sum approximation factors for Cook-Torrance specular BRDF. (look up table)
    float2 env_brdf = specularBRDF_LUT.Sample(spBRDF_Sampler, float2(cosLo, roughness)).rg;
    float2 brdf = specularBRDF_LUT.Sample(spBRDF_Sampler, float2(cosLo, (1.0 - roughness))).xy;
    float3 specularIBL = (brdf.x * F0 + brdf.y) * specularIrradiance;

    return float4((specularIBL + diffuseIBL) * Ambient_Light_color * Ambient_Light_Raddiance, 1); // * light[0].ambientFactor;		
}                                                                    

// -------------------------------------------------------------------------------------------------------------
//  Fetch Data needed to render 1 pixel, like a deffered shader's GBuffer
// -------------------------------------------------------------------------------------------------------------
GBufferMaterial GetMaterial(in PixelInputType input)
{
    GBufferMaterial material;
    
    // default values
    material.diffuse = float4(0.2f, 0.2f, 0.2f, 1);
    material.specular = float4(0, 0, 0, 0);    
    material.roughness = 1.0f;
    material.metalness = 0.0f;   
    material.pixelNormal = input.normal;    
    material.maskValue = 0;

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

float4 DoToneMapping(float3 hdrColor, float exposureFactor = 1.0, float gamma_value = 2.2)
{
    float3 hdrColorExposed = hdrColor.rgb * exposure * exposureFactor;
    float3 toneMappedColor = Uncharted2ToneMapping(hdrColorExposed);
    float3 gammmaCorrectedColor = pow(toneMappedColor, 1.0 / gamma_value);

    return float4(gammmaCorrectedColor.rgb, 1);
}


float3 InterpolateColor(float3 c1, float3 c2, float t)
{
    return lerp(c1, c2, t);
}

float3 SampleGradient(float t)
{
    
    float3 colors[4] = { float3(0, 0, 0), float3(1, 0.200000003, 0), float3(0.670000017, 0.670000017, 0.670000017), float3(1, 1, 1) };
    float times[4] = { 0, 0.330000013, 0.670000017, 1 };
    
    
    if (t <= times[0])
        return colors[0];
    if (t >= times[3])
        return colors[3];

    // Binary search for the segment
    int left = 0;
    int right = 3;
    while (left < right - 1)
    {
        int mid = (left + right) / 2;
        if (times[mid] <= t)
            left = mid;
        else
            right = mid;
    }

    // Normalize t within the segment
    float localT = (t - times[left]) / (times[right] - times[left]);

    return InterpolateColor(colors[left], colors[right], localT);
}


float4 MainPS(in PixelInputType input, bool bIsFrontFace : SV_IsFrontFace) : SV_TARGET0
{
    GBufferMaterial material = GetMaterial(input);        

    float3 envColor = getAmbientLight(
		material.pixelNormal,
		normalize(input.viewDirection),
		material.diffuse.rgb,// * TintColour,
		material.roughness,
		material.metalness,
		float4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1)
		);
	
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
    float3 emissiveColour = SampleGradient(material.maskValue) * 3;
    color += emissiveColour;// * material.maskValue;
	
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
        VertexShader = compile vs_5_0 MainVS();
        PixelShader = compile ps_5_0 MainPS();
    }
};