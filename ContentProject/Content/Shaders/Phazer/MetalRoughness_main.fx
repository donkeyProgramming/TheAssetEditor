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

// Direction for directional lighting source
static const float3 light_Direction_Constant = normalize(float3(0.1, 0.1, 1.f));

// directional light strength, dial from 0 - X

static const float Directional_Light_Raddiance = 5.0f;
static const float Ambient_Light_Raddiance = 1.0f;

// colors for lighting, set a color almost white, with a TINY blue tint to change mood in scene
// typically these two are simply te same color, unless one wanted to simular artifical light (like a lamp/flashlight etc)
static const float3 Directional_Light_color = float3(1, 1, 1);
static const float3 Ambient_Light_color = float3(1, 1, 1);

// -------------------------------------------------------------------------------------
//		VERTEX SHADER
// -------------------------------------------------------------------------------------
PixelInputType main(in VertexInputType input) // main is the default function name
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

// --------------------------------------------------------------------------------------------------------------------------------------
// Get Directional Light
// --------------------------------------------------------------------------------------------------------------------------------------

float3 getDirectionalLight(in float3 N, in float3 albedo, in float roughness, in float metalness, in float3 viewDir, in float3 lightDir)
{

	// TODO: PHAZER: alternative View direction (vector from world-space fragment position to the "eye").
	// not really needed, might be more "precise" as it per pixel, but does not seem to change anything
	//float3 Lo = -normalize(eyePosition - pos);

    float3 Lo = normalize(viewDir);

	// Angle between surface normal and outgoing light direction.
    float cosLo = max(0.0, dot(N, Lo));

	// Specular reflection vector.
    float3 Lr = 2.0 * cosLo * N - Lo;

	// Fresnel reflectance at normal incidence (for metals use albedo color).
    float3 F0 = lerp(Fdielectric, albedo, metalness);

	// Direct lighting calculation for analytical lights.
    float3 directLighting = 0.0;



	//float3 Li = normalize(-LightData[0].lightDirection);
    float3 Li = normalize(lightDir);
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
    float3 kd = lerp(float3(1, 1, 1) - F, float3(0, 0, 0), metalness);

	// Lambert diffuse BRDF.
	// We don't scale by 1/PI for lighting & material units to be more convenient.
	// See: https://seblagarde.wordpress.com/2012/01/08/pi-or-not-to-pi-in-game-lighting-equation/
    float3 diffuseBRDF = kd * albedo;

	// Cook-Torrance specular microfacet BRDF.
    float3 specularBRDF = (F * D * G) / max(Epsilon, 4.0 * cosLi * cosLo);
	// Total contribution for this light.
    directLighting += (diffuseBRDF + specularBRDF) * Lradiance * cosLi * Directional_Light_color;


    return directLighting;
}




float3 getAmbientLight(
	in float3 _normal,
	in float3 viewDir,
	in float3 Albedo,
	in float roughness,
	in float metalness,
	float4x4 envTransform)
{

    float3 normal1 = normalize(_normal);

    float3 Lo = normalize(viewDir);;

	// Angle between surface normal and outgoing light direction.
    float cosLo = max(0.0, dot(normal1, Lo));

	// Specular reflection vector.
	// float3 Lr = 2.0 * cosLo * N- Lo;  // written out reflect formula, not using intrinsic HLSL function
    float3 Lr = reflect(normal1, Lo); // HLSL intrisic reflection function


	// Fresnel reflectance at normal incidence (for metals use albedo color).
    float3 F0 = lerp(Fdielectric, Albedo.rgb, metalness);


	// Calculate Fresnel term for ambient lighting.	
    float3 F = fresnelSchlick(F0, cosLo);

	//float3 F = FresnelSchlickRoughness(cosLo, F0, roughness);

	// Get diffuse contribution factor (as with direct lighting).
    float3 kd = lerp(1.0 - F, 0.0, metalness);


	// rotate only normal with ENV map matrix, when they are used the to sample the ENV maps
	// don't change the actual normal, 
	// so the transforsm does not disturb the PBR math


	// the environment rotated normals
    float3 diffuse_rot_normal = mul(normal1, (float3x3) EnvMapTransform);
    diffuse_rot_normal = normalize(diffuse_rot_normal);

    float3 specular_rot_normal = mul(Lr, (float3x3) EnvMapTransform);
    specular_rot_normal = normalize(specular_rot_normal);



	//float3 irradiance = tex_cube_diffuse.Sample(SampleType, normal).rgb;
    float3 irradiance = tex_cube_diffuse.Sample(SampleType, diffuse_rot_normal).rgb;
    float3 diffuseIBL = kd * Albedo.rgb * irradiance;

	// TODO: scale up diffuse, if needed
	//diffuseIBL.rgb *= 10.f;


	// PHAZER:
	// rotate only normal with ENV map matrix, when they are use the to sample the ENV maps
	// so the transform does not disturb the PBR math
	// --
	// rotate refletion map by rotating the reflected normal vector, and use that to sample the specular map


	// TODO:  NOTE:
	/*
		the evn rotated output value "Lr_Not is NOT used, to reflectio vectors are not rotation

		also I found a the right way to rotate environment without messeing up ligting, will add later

	*/
#if 0
	float3 Lr_Rot = mul(Lr, (float3x3) EnvMapTransform);
#endif
    float3 specularIrradiance = sample_environment_specular(roughness, normalize(specular_rot_normal));




	// Split-sum approximation factors for Cook-Torrance specular BRDF. (look up table)
    float2 env_brdf = specularBRDF_LUT.Sample(spBRDF_Sampler, float2(cosLo, roughness)).rg;


    float2 brdf = specularBRDF_LUT.Sample(spBRDF_Sampler, float2(cosLo, (1.0 - roughness))).xy;
    float3 specularIBL = (brdf.x * F0 + brdf.y) * specularIrradiance;
	//// The follow is a simpler approximation, give less "white edges" with high env light strength, but it also less "physically correct
	//float2 env_brdf = EnvBRDFApprox(roughness, cosLo);

	// the more "physically correct" one, using the Look up Table texture
	//float3 specularIBL = specularIrradiance * (F0 * env_brdf.r + env_brdf.g);


    return float4((specularIBL + diffuseIBL) * Ambient_Light_color * Ambient_Light_Raddiance, 1); // * light[0].ambientFactor;		
}



float4 mainPs(in PixelInputType _input, bool bIsFrontFace : SV_IsFrontFace) : SV_TARGET0
{

    PixelInputType input;
	// make a copy of input, so it can used in "inout", this is very ineffective
	// TODO: optimize
    input = _input;

	/*   if (bIsFrontFace)
	  {
			input.normal *= -1;
			input.tangent *= -1;
			input.binormal *= -1;
		}
	*/
    float2 texCord = float2(nfmod(input.tex.x, 1), nfmod(input.tex.y, 1));
    float4 SpecTex = float4(0, 0, 0, 1);
    if (UseSpecular)
    {
        SpecTex.rgb = _linear(SpecularTexture.Sample(SampleType, texCord).rgb);
		//SpecTex = pow(SpecTex, 2.2);
    }

    float4 DiffuseTex = float4(0.5f, 0.5f, 0.5f, 1);
    if (UseDiffuse)
    {
        float4 diffuseValue = DiffuseTexture.Sample(SampleType, texCord);
        DiffuseTex.rgb = _linear(diffuseValue.rgb);
        DiffuseTex.a = diffuseValue.a;
		//DiffuseTex = pow(DiffuseTex, 2.2);
		//DiffuseTex.rgb = DiffuseTex.rgb * (1 - max(SpecTex.b, max(SpecTex.r, SpecTex.g)));
    }

    float4 GlossTex = float4(0, 0, 0, 1);
    if (UseGloss)
        GlossTex = GlossTexture.Sample(SampleType, texCord);
	
    float4 NormalTex = float4(0.5f, 0.5f, 0.5f, 1);
    if (UseNormal)
        NormalTex = NormalTexture.Sample(s_normal, input.tex);

    float metalness = GlossTex.r; // metal mask channel
																			
	// transorm roughness response, make more "shiny", this is just guess work, it might be TOO shiny
    float roughness = pow(GlossTex.g, 2.2f); // roughness channel

	
	
    float3 pixelNormal = GetPixelNormal(input);

    if (UseNormal == false)
    {
        pixelNormal = _input.normal;
		//return float4(1, 0, 0, 1);
    }

    float3 envColor = getAmbientLight(
		pixelNormal,
		normalize(input.viewDirection),
		DiffuseTex.rgb * TintColour,
		roughness,
		metalness,
		float4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1)
		);

	
    float3 rot_lightDir = normalize(mul(light_Direction_Constant, (float3x3) DirLightTransform));

    //return float4(rot_lightDir, 1);
	
	
	// directional ligting color    
    float3 dirColor = getDirectionalLight(
		pixelNormal,
		DiffuseTex.rgb * TintColour,
		roughness,
		metalness,
		normalize(input.viewDirection),
		rot_lightDir
	

	);

	// add environman (ambient) light to directional light
    float3 color = envColor + dirColor;

	// TODO: alpha is not enabled for some reason, so if enable this line alpha clipping is off for all/most models
	//if (UseAlpha == 1)
	{
        alpha_test(DiffuseTex.a);
    }

    static const float gamma_value = 2.2;

    float3 hdrColor = color.rgb * exposure;

	// PHAZER: I think tint has to be multiplied on BEFORE tonemapping
    float3 mapped = Uncharted2ToneMapping(hdrColor);
    mapped = pow(mapped, 1.0 / gamma_value);

    float ambinent = 0.0f;
    float3 finalColor = float4(mapped, 1); // + float4(ambinent, ambinent, ambinent,0);

    return float4(finalColor, 1);

}
float4 SimplePixel(in PixelInputType _input /*, bool bIsFrontFace : SV_IsFrontFace*/) : SV_TARGET0
{
    return float4(1, 0, 0, 1);
}

technique BasicColorDrawing
{
    pass P0
    {
        VertexShader = compile vs_5_0 main();
        PixelShader = compile ps_5_0 mainPs();
    }
};