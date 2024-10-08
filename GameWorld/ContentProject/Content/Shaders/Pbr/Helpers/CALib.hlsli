#ifndef CALIB_HLSLI
#define CALIB_HLSLI

#include "../Shared/const_layout.hlsli"
#include "../inputlayouts.hlsli"
#include "../TextureSamplers.hlsli"
#include "../helpers/mathfunctions.hlsli"
#include "../helpers/mathconstants.hlsli"


float get_cube_env_scale_factor()
{
    return 1.0f;
}

float adjust_linear_smoothness(in const float linear_smoothness)
{
    return linear_smoothness * linear_smoothness;
}

float3 get_environment_colour(in float3 direction, in float lod)
{
    return (tex_cube_specular.SampleLevel(SampleType, direction, lod).rgb);
}

float get_env_map_lod(in float roughness_in)
{
    float smoothness = pow(1.0 - roughness_in, 4.0);

    float roughness = 1.0 - smoothness;

	//    This must be the number of mip-maps in the environment map!
    float texture_num_lods = 6.0f;

    float env_map_lod = roughness * (texture_num_lods - 1.0);

    return env_map_lod;
}

float3 sample_environment_specular(in float roughness_in, in float3 reflected_view_vec)
{
    const float env_lod_pow = 1.8f;
	//const float env_map_lod_smoothness = adjust_linear_smoothness(1 - roughness_in);
    const float env_map_lod_smoothness = (1 - roughness_in);
    const float roughness = 1.0f - pow(env_map_lod_smoothness, env_lod_pow);

    float texture_num_lods = 8; //<------- LOWER = more reflective
    float env_map_lod = roughness * (texture_num_lods - 1);
    float3 environment_colour = get_environment_colour(reflected_view_vec, env_map_lod);

    float3 result = environment_colour * get_cube_env_scale_factor();

    return result;
}

float3 sample_environment_specular_new(in float roughness_in, in float3 reflected_view_vec)
{
#if 1
	//const float env_lod_pow = 1.8f;
    const float env_lod_pow = 1.8f;
    const float env_map_lod_smoothness = adjust_linear_smoothness(1 - roughness_in);
    const float roughness = 1.0f - pow(env_map_lod_smoothness, env_lod_pow);

    float texture_num_lods = 5; // to a lower number
    float env_map_lod = roughness * (texture_num_lods - 1);
    float3 environment_colour = get_environment_colour(reflected_view_vec, env_map_lod);
#else
	const float roughness = roughness_in;
	const float offset = 3;
	float texture_num_lods = 9.0f; // - offset;
	float env_map_lod = roughness * (texture_num_lods - 1);
	env_map_lod += offset;
	float3 environment_colour = get_environment_colour(reflected_view_vec, env_map_lod);
#endif

    float3 result = environment_colour * get_cube_env_scale_factor();

    return result;
}

float substance_smoothness_get_our_smoothness(in float substance_smoothness)
{
	//	This value is correct for roughnesses from second_join_pos to 1.0.  This is valid for
	//	the end of the roughness curve...
    float original_roughness = 1.0f - substance_smoothness;

    float original_roughness_x2 = original_roughness * original_roughness;
    float original_roughness_x3 = original_roughness_x2 * original_roughness;
    float original_roughness_x4 = original_roughness_x3 * original_roughness;
    float original_roughness_x5 = original_roughness_x4 * original_roughness;

    return 1.0f - saturate((28.09f * original_roughness_x5) - (64.578f * original_roughness_x4) + (48.629f * original_roughness_x3) - (12.659f * original_roughness_x2) + (1.5459f * original_roughness));
}

void alpha_test(in const float pixel_alpha)
{
    clip(pixel_alpha < 0.7f ? -1 : 1);
	//clip(-1);
}

void apply_faction_colours(inout float3 diffuse_colour_rgb, in Texture2D MaskText, in sampler S,
in const float2 tex_coord,
in const float3 c1,
in const float3 c2,
in const float3 c3)
{
   
    
    float mask_p1 = MaskText.Sample(S, tex_coord).r;
    float mask_p2 = MaskText.Sample(S, tex_coord).g;
    float mask_p3 = MaskText.Sample(S, tex_coord).b;

	//faction colours
    diffuse_colour_rgb = lerp(diffuse_colour_rgb, diffuse_colour_rgb * linear_accurate(c1), mask_p1);
    diffuse_colour_rgb = lerp(diffuse_colour_rgb, diffuse_colour_rgb * linear_accurate(c2), mask_p2);
    diffuse_colour_rgb = lerp(diffuse_colour_rgb, diffuse_colour_rgb * linear_accurate(c3), mask_p3);
    
}


float3 normalSwizzle_UPDATED(in float3 ref)
{

#ifdef FXCOMPOSER
	return ref.xyz;
#else
    return float3(ref.x, ref.z, ref.y);
#endif
}

//float3 GetPixelNormal(PixelInputType input)
//{
//	//float3x3 basis = float3x3(normalize(input.tangent), normalize(input.normal), normalize(input.binormal));      // TODO: some games have this, sometimes enables, sometimes as as comment,
//    float3x3 basis = float3x3(normalize(input.tangent.xyz), normalize(input.binormal.xyz), normalize(input.normal.xyz)); // works in own shader

//    float4 NormalTex = NormalTexture.Sample(s_normal, input.tex);

//    // -------------------------------------------------------------------------------------------------------------------
//    // decode the "orange 2d normal map", into a standard "blue" 3d normal map using an othogonal projection
//    // -------------------------------------------------------------------------------------------------------------------
//    float3 Np = 0;
//    Np.x = NormalTex.r * NormalTex.a;
//    Np.y = 1.0 - NormalTex.g;
//    Np = (Np * 2.0f) - 1.0f;
//    Np.z = sqrt(1 - Np.x * Np.x - Np.y * Np.y);
    	
//    return normalize(mul(normalize(Np.xyz), basis));
//}

float3 EnvBRDFApprox(float3 SpecularColor, float Roughness, float NoV)
{
    const float4 c0 = { -1, -0.0275, -0.572, 0.022 };
    const float4 c1 = { 1, 0.0425, 1.04, -0.04 };
    float4 r = Roughness * c0 + c1;
    float a004 = min(r.x * r.x, exp2(-9.28 * NoV)) * r.x + r.y;
    float2 AB = float2(-1.04, 1.04) * a004 + r.zw;
    return ((AB.x * SpecularColor) + AB.y);
}


float3 fresnelSchlickRoughness(float cosTheta, float3 F0, float roughness)
{
    return F0 + (float3) (max((1.0 - roughness), F0) - F0) * pow(1.0 - cosTheta, 5.0);
}

float3 FresnelSchlickWithRoughness(float3 SpecularColor, float3 E, float3 N, float Gloss)
{
    return SpecularColor + (max(Gloss, SpecularColor) - SpecularColor) * pow(1 - saturate(dot(E, N)), 5);
}


float ndfGGX(float cosLh, float roughness)
{
    float alpha = roughness * roughness;
    float alphaSq = alpha * alpha;

    float denom = (cosLh * cosLh) * (alphaSq - 1.0) + 1.0;
    return alphaSq / (pi * denom * denom);
}

// Single term for separable Schlick-GGX below.
float gaSchlickG1(float cosTheta, float k)
{
    return cosTheta / (cosTheta * (1.0 - k) + k);
}

// Schlick-GGX approximation of geometric attenuation function using Smith's method.
float gaSchlickGGX(float cosLi, float cosLo, float roughness)
{
    float r = roughness + 1.0;
    float k = (r * r) / 8.0; // Epic suggests using this roughness remapping for analytic lights.
    return gaSchlickG1(cosLi, k) * gaSchlickG1(cosLo, k);
}

// Shlick's approximation of the Fresnel factor.
float3 fresnelSchlick(float3 F0, float cosTheta)
{
    return F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0);
}

// Returns number of mipmap levels for specular IBL environment map.
uint querySpecularTextureLevels(in TextureCube specularTexture)
{
    uint width, height, levels;
    specularTexture.GetDimensions(0, width, height, levels);
    return levels;
}

#endif // CALIB_HLSLI