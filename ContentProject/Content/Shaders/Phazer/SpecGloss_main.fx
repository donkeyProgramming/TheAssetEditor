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

// directional light strength, dial from 0 - X
// TODO: good candiates for a dials
static const float Directional_Light_Raddiance = 5.0f;
static const float Ambient_Light_Raddiance = 1.0;

// colors for lighting, set a color almost white, with a TINY blue tint to change mood in scene
// typically these two are simply te same color, unless one wanted to simular artifical light (like a lamp/flashlight etc)
// TODO: good candiates for a dials
static float3 Directional_Light_color = float3(1, 1, 1);
static float3 Ambient_Light_color = float3(1, 1, 1);



// *******************************************************************************************************************************
//          Vertex Shader Code
// *******************************************************************************************************************************


// -------------------------------------------------------------------------------------
//		VERTEX SHADER
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

// *******************************************************************************************************************************
//          Pixel Shader
// *******************************************************************************************************************************

// ------------------------------------------------------------------------------------------------------------------------------ 
//   Directional LIght component
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

    
    // TODO: check Which of these two "direct light mat scatter" are better?    
    
	// Diffuse scattering happens due to light being refracted multiple times by a dielectric medium.
	// Metals on the other hand either reflect or absorb energy, so diffuse contribution is always zero.
	// To be energy conserving we must scale diffuse BRDF contribution based on Fresnel factor & metalness.
    float3 dlightMaterialScattering = lerp(float3(1, 1, 1) - F, float3(0, 0, 0), saturate(specular));

    //float3 dlightMaterialScattering = 1.0f - specular; //  All photons not accounted for by reflectivity are accounted by scattering. From the energy difference between in-coming light and emitted light we could calculate the amount of energy turned into heat. This energy would not be enough to make a viewable difference at standard illumination levels.
    
	// Lambert diffuse BRDF.
	// We don't scale by 1/PI for lighting & material units to be more convenient.
	// See: https://seblagarde.wordpress.com/2012/01/08/pi-or-not-to-pi-in-game-lighting-equation/
    
	// TODO: is no div by PI ok?                                                      
    float3 diffuseBRDF = diffuse * dlightMaterialScattering; //  / PI;	
	
	// Cook-Torrance specular microfacet BRDF.
    float3 specularBRDF = (F * D * G) / max(Epsilon, 4.0 * cosLi * cosLo);

	// Total contribution for this light.
    directLighting += (diffuseBRDF + specularBRDF) * Lradiance * cosLi * Directional_Light_color;

    return directLighting;
}

// -------------------------------------------------------------------------------------------------------------
//  Fetch Data needed to rener 1 pixel
// -------------------------------------------------------------------------------------------------------------
GBufferMaterial GetMaterial(in PixelInputType input)
{
    GBufferMaterial material;

    float2 texCord = float2(nfmod(input.tex.x, 1), nfmod(input.tex.y, 1));
    
    material.specular = float4(0, 0, 0, 1);
    if (UseSpecular)
    {
        material.specular = _linear(SpecularTexture.Sample(SampleType, texCord).rgb);		
    }
    
    material.diffuse = float4(0.5f, 0.5f, 0.5f, 1);
    if (UseDiffuse)
    {
        float4 diffuseTexSample = DiffuseTexture.Sample(SampleType, texCord);
        material.diffuse.rgb = _linear(diffuseTexSample.rgb);
        material.diffuse.a = diffuseTexSample.a;
    }
    
    float4 glossTex = float4(0, 0, 0, 1);
    if (UseGloss)
    {    
        glossTex = GlossTexture.Sample(SampleType, texCord);
    }
                                                            	
    material.roughness = saturate((1 - glossTex.r));
        
    material.pixelNormal = input.normal;
    if (UseNormal)
    {        
        material.pixelNormal = GetPixelNormal(input);
    }            
    
    return material;
}

float3 GetAmbientlLight_SpecGloss(in float3 N, in float3 diffuse, in float3 specular, in float roughness, in float3 viewDirection)
{
    float3 ambientLightColor = float3(0, 0, 0);
    	
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

    //float3 kD = diffuse.rgb * (1 - saturate(max(max(specular.r, specular.g), specular.b)));
    
    float3 diffuseIBL = kD * diffuse * irradiance;    
    
	// rotate refletion map by rotating the reflect vector
    float3 Lr_Rot = mul(Lr, (float3x3) EnvMapTransform);
    float3 specularIrradiance = sample_environment_specular(roughness, normalize(Lr_Rot));

    float2 brdf = specularBRDF_LUT.Sample(spBRDF_Sampler, float2(cosLo, (1.0 - roughness))).xy;
    float3 specularIBL = (brdf.x * F0 + brdf.y) * specularIrradiance;       
        
    float3 ambientLighting = (specularIBL + diffuseIBL) * Ambient_Light_Raddiance; // * light[0].ambientFactor;    
            
    return ambientLightColor;
}

float4 DoToneMapED(float3 hdrColor, float exposureFactor = 1.0, float gamma_value= 2.2)
{
    float3 hdrColorExposed = hdrColor.rgb * exposure * exposureFactor;	
    float3 toneMappedColor = Uncharted2ToneMapping(hdrColorExposed);
    float3 gammmaCorrectedColor = pow(toneMappedColor, 1.0 / gamma_value);                 

    return float4(gammmaCorrectedColor.rgb, 1);
}

float4 mainPS(in PixelInputType input, bool bIsFrontFace : SV_IsFrontFace) : SV_TARGET0
{
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

	// TODO: alpha is not enabled for some reason, so if enable this line alpha clipping is off for all/most models??
	//if (UseAlpha == 1)
	{
        alpha_test(material.diffuse.a);
    }
		
    return DoToneMapED(color.rgb);
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

// -------------------------------------
//    TODO: DELETE OLD
// -------------------------------------

//float4 mainPS(in PixelInputType _input, bool bIsFrontFace : SV_IsFrontFace) : SV_TARGET0
//{
//    PixelInputType input;
//    input = _input;

//	/*   if (bIsFrontFace)
//	  {
//			input.normal *= -1;
//			input.tangent *= -1;
//			input.binormal *= -1;
//		}
//	*/
//    float2 texCord = float2(nfmod(input.tex.x, 1), nfmod(input.tex.y, 1));
//    float4 SpecTex = float4(0, 0, 0, 1);
//    if (UseSpecular)
//    {
//        SpecTex.rgb = _linear(SpecularTexture.Sample(SampleType, texCord).rgb);
//		//SpecTex = pow(SpecTex, 2.2);
//    }

//    float4 DiffuseTex = float4(0.5f, 0.5f, 0.5f, 1);
//    if (UseDiffuse)
//    {
//        float4 diffuseValue = DiffuseTexture.Sample(SampleType, texCord);
//        DiffuseTex.rgb = _linear(diffuseValue.rgb);
//        DiffuseTex.a = diffuseValue.a;
//		//DiffuseTex = pow(DiffuseTex, 2.2);
//		//DiffuseTex.rgb = DiffuseTex.rgb * (1 - max(SpecTex.b, max(SpecTex.r, SpecTex.g)));
//    }

//    float4 GlossTex = float4(0, 0, 0, 1);
//    if (UseGloss)
//        GlossTex = GlossTexture.Sample(SampleType, texCord);

//	//return float4(GlossTex.rrr, 1);

//    float4 NormalTex = float4(0.5f, 0.5f, 0.5f, 1);
//    if (UseNormal)
//        NormalTex = NormalTexture.Sample(s_normal, input.tex);

//    float smoothness = (GlossTex.r);
	
	
//	// smoothness = pow(smoothness, 2);
//    float roughness = saturate((1 - smoothness));
	
//	// transorm roughness response, make more "shiny", equivalent to do doing reverse gamma correction	
//    roughness = saturate(pow(roughness, 2.2f)); // roughness channel

//    float3 bumpNormal = GetPixelNormal(input);
	
//    if (UseNormal == false)
//    {
//        bumpNormal = _input.normal;
//		//return float4(1, 0, 0, 1);
//    }

//	// ************************************************************************
//	//bumpNormal = input.normal;

//	// ************************************************************************
//    float3 N = normalize(bumpNormal);

//	//float3 Lo = float3(0,0,1);
//    float3 Lo = normalize(input.viewDirection);

//	// Angle between surface normal and outgoing light direction.
//    float cosLo = max(0.0, dot(N, Lo));

//	// Specular reflection vector.
//	// float3 Lr = 2.0 * cosLo * N- Lo;  // written out reflect formula
//    float3 Lr = reflect(N, Lo); // HLSL intrisic reflection function

//		// specular
//    float3 F0 = SpecTex.rgb;

//	// rotate only normal with ENV map matrix, when they are use the to sample the ENV maps
//	// so the transfors does not disturb the PBR math
//	// --
//    //float3 rot_lightDir = mul(light_Direction_Constant, (float3x3) DirLightTransform);
//    float3 rot_lightDir = normalize(mul(light_Direction_Constant, (float3x3) DirLightTransform));

//	// TODO: BEGIN DEBUG CODE
//    //return float4(rot_lightDir, 1);
//	// EBD: DEBUG C ODE	
    
//    float3 bumpNormal_Rot = mul(N, (float3x3) EnvMapTransform);
//    bumpNormal_Rot = normalize(bumpNormal_Rot);
//    float3 irradiance = tex_cube_diffuse.Sample(SampleType, bumpNormal_Rot).rgb;
//	//return float4(irradiance, 1);
//	//float3 mapped_test = Uncharted2ToneMapping(irradiance);
//	//float3 color_test = pow(mapped_test, 1.0 / 1.0);
//	//return float4(irradiance, 1);
//	   //// ----------------

//    float3 F = fresnelSchlickRoughness(cosLo, F0, roughness);

//    float3 kS = F;
//    float3 kD = 1.0 - kS;

//    float3 diffuseIBL = kD * DiffuseTex.rgb * irradiance;

//	// PHAZER:
//	// rotate only normal with ENV map matrix, when they are use the to sample the ENV maps
//	// so the transfors does not disturb the PBR math
//	// --
//	// rotate refletion map by rotating the reflect vector
//    float3 Lr_Rot = mul(Lr, (float3x3) EnvMapTransform);
//    float3 specularIrradiance = sample_environment_specular(roughness, normalize(Lr_Rot));

//    float2 brdf = specularBRDF_LUT.Sample(spBRDF_Sampler, float2(cosLo, (1.0 - roughness))).xy;
//    float3 specularIBL = (brdf.x * F0 + brdf.y) * specularIrradiance;

    
	
//    float3 directionalLightColor = GetDirectionalLight_SpecGloss(bumpNormal, DiffuseTex.rgb, SpecTex.rgb, roughness, Lo);
	
	
	
//    float3 ambientLighting = (specularIBL + diffuseIBL) * Ambient_Light_Raddiance; // * light[0].ambientFactor;

//    float4 color = float4(ambientLighting + directionalLightColor, 1.0);

//	// TODO: alpha is not enabled for some reason, so if enable this line alpha clipping is off for all/most models??
//	//if (UseAlpha == 1)
//	{
//        alpha_test(DiffuseTex.a);
//    }
		                                              	
//    static const float gamma_value = 2.2;

//    float3 hdrColor = color.rgb * exposure;

//	// PHAZER: Tint has to multiplied unto the texture color before light processing, so you get a new diffuse texture color
//    float3 mapped = Uncharted2ToneMapping(hdrColor);
//    mapped = pow(mapped, 1.0 / gamma_value);

//    float ambinent = 0.0f;
//    float3 finalColor = float4(mapped, 1); // + float4(ambinent, ambinent, ambinent,0);

//    return float4(finalColor, 1);

//}
