#include "../Helpers/CALib.hlsli"
#include "../TextureSamplers.hlsli"

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