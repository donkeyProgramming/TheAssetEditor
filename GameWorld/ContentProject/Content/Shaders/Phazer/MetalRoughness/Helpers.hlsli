#include "../Helpers/CALib.hlsli"
#include "../TextureSamplers.hlsli"

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
float3 getAmbientLight(in float3 normal1, in float3 viewDir, in float3 Albedo, in float roughness, in float metalness, float4x4 envTransform)
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