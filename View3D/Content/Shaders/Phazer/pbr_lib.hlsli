static const float PI = 3.141592;
static const float Epsilon = 0.00001;

static const uint NumLights = 3;

// Constant normal incidence Fresnel factor for all dielectrics.
static const float3 Fdielectric = 0.04;


float get_direct_roughness(in float roughness_in)
{
	float smoothness = pow(1.0 - roughness_in, 4.0);

	float roughness = 1.0 - smoothness;
    
    return roughness;
}


float get_env_map_lod(in float roughness_in, in float texture_num_lods)
{
	float smoothness = pow(1.0 - roughness_in, 4.0);

	float roughness = 1.0 - smoothness;

	//	This must be the number of mip-maps in the environment map!
//	float texture_num_lods = 10.0f;
	
	float env_map_lod = roughness * (texture_num_lods-1);
	
	return env_map_lod;
}



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
    return alphaSq / (PI * denom * denom);
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

float linear_accurate_component(in const float srgb_val)
{
    const float inflection_point = 0.04045;

    if (srgb_val <= inflection_point)
    {
        return srgb_val / 12.92f;
    }
    else
    {
        const float a = 0.055f;

        return pow((srgb_val + a) / (1.0f + a), 2.4f);
    }
}

float3 linear_accurate(in const float3 fGamma)
{
    return float3(linear_accurate_component(fGamma.r), linear_accurate_component(fGamma.g), linear_accurate_component(fGamma.b));
}

float _linear(in float fGamma)
{
    return linear_accurate_component(fGamma);
}

float3 _linear(in float3 vGamma)
{
    return linear_accurate(vGamma);
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


