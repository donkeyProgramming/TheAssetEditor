#include "pbr_lib.fx"
#include "tone_mapping.fx"


// -------------------------------------------------------------------------------
//
//	Constant, and WORDs for Ole ;(
//
// -------------------------------------------------------------------------------
// TODO: change these into thing controllable from the program


// Direction for directional lighting source
// TODO: good candiates for a dial, where Z is constant and X+Y is controller by the mouse
// or use your camera class to controll the direction light = "orbital light control"
static const float3 light_Direction_Constant = (0.01, 0.1, 1.f);

// directional light strength, dial from 0 - X
// TODO: good candiates for a dials
static const float Directional_Light_Raddiance = 2.0f;
static const float Ambient_Light_Raddiance = 1.0f;

// colors for lighting, set a color almost white, with a TINY blue tint to change mood in scene
// typically these two are simply te same color, unless one wanted to simular artifical light (like a lamp/flashlight etc)
// TODO: good candiates for a dials
static float3 Directional_Light_color = float3(1, 1, 1);
static float3 Ambient_Light_color = float3(1, 1, 1);





//  From http://en.wikipedia.org/wiki/SRGB                          //
//                                                                  //
//  It seems that gamma to linear and back again is not a simple    //
//  pow function.  The functions above are just a simplification    //
//  of what the spec is and what the hardware is doing, which is    //
//  following this spec.                                            //
//////////////////////////////////////////////////////////////////////
float gamma_accurate_component(in const float linear_val)
{
	const float srgb_gamma_ramp_inflection_point = 0.0031308f;

	if (linear_val <= srgb_gamma_ramp_inflection_point)
	{
		return 12.92f * linear_val;
	}
	else
	{
		const float a = 0.055f;

		return ((1.0f + a) * pow(linear_val, 1.0f / 2.4f)) - a;
	}
}

float3 gamma_accurate(in const float3 vLinear)
{
	return float3(gamma_accurate_component(vLinear.r), gamma_accurate_component(vLinear.g), gamma_accurate_component(vLinear.b));
}


float3 _gamma(in float3 vLinear)
{
	return gamma_accurate(vLinear);
}

float _gamma(in float fLinear)
{
	return gamma_accurate_component(fLinear);
}

//#include "pbr_lib.hlsli"
//#include "tone_mapping.hlsli"

bool doAnimation = true;

float4x4 World;
float4x4 View;
float3 CameraPos;
float4x4 Projection;

float4x4 EnvMapTransform;
float LightMult = 1;

bool debug = true;
float debugVal = 0;
int show_reflections = false;
bool is_diffuse_linear = false;
bool is_specular_linear = false;
float exposure = 1;
bool scale_by_one_over_pi = false;

float light0_roughnessFactor = 1;
float light0_radiannce = 1;
float light0_ambientFactor = 1;

float3 TintColour = float3(1, 1, 1);

// Textures
Texture2D<float4> DiffuseTexture;
Texture2D<float4> SpecularTexture;
Texture2D<float4> NormalTexture;
Texture2D<float4> GlossTexture;
Texture2D<float4> MaterialMapTexture;

TextureCube<float4> tex_cube_diffuse;
TextureCube<float4> tex_cube_specular;
Texture2D<float4> specularBRDF_LUT;

bool UseDiffuse = true;
bool UseSpecular = true;
bool UseNormal = true;
bool UseGloss = true;
bool UseAlpha = false;

float4x4 tranforms[256];
int WeightCount = 0;

SamplerState SampleType
{
	//Texture = <tex_cube_specular>;
	MinFilter = LINEAR;
	MagFilter = LINEAR;
	Mipfilter = LINEAR;
	Filter = Anisotropic;
	MaxAnisotropy = 16;
	AddressU = Wrap;
	AddressV = Wrap;
};

SamplerState spBRDF_Sampler
{
	//Texture = <tex_cube_specular>;
	MinFilter = LINEAR;
	MagFilter = LINEAR;
	Mipfilter = LINEAR;
	Filter = LINEAR;
	MaxAnisotropy = 16;
	AddressU = Clamp;
	AddressV = Clamp;
};

SamplerState s_normal
{
	//Texture = <tex_cube_specular>;
	MinFilter = LINEAR;
	MagFilter = LINEAR;
	Mipfilter = LINEAR;
	Filter = LINEAR;
	MaxAnisotropy = 16;
	AddressU = Wrap;
	AddressV = Wrap;
};

//

struct VertexInputType
{
	float4 position : POSITION;
	float3 normal : NORMAL0;
	float2 tex : TEXCOORD0;

	float3 tangent : TANGENT;
	float3 binormal : BINORMAL;

	float4 Weights : COLOR;
	float4 BoneIndices : BLENDINDICES0;
};

struct PixelInputType
{
	float4 position : SV_POSITION;
	float2 tex : TEXCOORD0;

	float3 normal : NORMAL0;
	float3 tangent : TANGENT;
	float3 binormal : BINORMAL;

	float3 viewDirection : TEXCOORD1;
	float3 worldPosition : TEXCOORD5;

};

float3 getBlueNormal(PixelInputType input)
{
	//float3x3 basis = float3x3(normalize(input.tangent), normalize(input.normal), normalize(input.binormal));
	float3x3 basis = float3x3(normalize(input.tangent.xyz), normalize(input.binormal.xyz), normalize(input.normal.xyz)); // works in own shader

	float4 NormalTex = NormalTexture.Sample(s_normal, input.tex);

	float3 Np = 0;
	Np.x = NormalTex.r * NormalTex.a;
	Np.y = 1.0 - NormalTex.g;
	Np = (Np * 2.0f) - 1.0f;

	Np.z = sqrt(1 - Np.x * Np.x - Np.y * Np.y);

	//Np.y = -Np.y;
	return normalize(mul(normalize(Np.xyz), basis));
}

float get_cube_env_scale_factor()
{
	return LightMult;
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

	float texture_num_lods = 4; //<------- LOWER = more reflective
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

// -------------------------------------------------------------------------------------
//		VERTEX SHADER
// -------------------------------------------------------------------------------------
PixelInputType main(in VertexInputType input) // main is the default function name
{
	PixelInputType output;

	float fac = 1;
	float4x4 scale4x4 = float4x4(
		fac, 0, 0, 0,
		0, fac, 0, 0,
		0, 0, fac, 0,
		0, 0, 0, 1);

	if (doAnimation && WeightCount != 0)
	{
		float4x4 PM[4];
		PM[0] = tranforms[input.BoneIndices.x];
		PM[1] = tranforms[input.BoneIndices.y];
		PM[2] = tranforms[input.BoneIndices.z];
		PM[3] = tranforms[input.BoneIndices.w];

		float4 pos = 0;
		float4 normal = 0;
		float4 tangent = 0;
		float4 biNormal = 0;

		for (int i = 0; i < 4; i++)
		{
			pos += input.Weights[i] * mul(input.position, PM[i]);
			normal.xyz += input.Weights[i] * mul(input.normal, (float3x3) PM[i]);
			tangent.xyz += input.Weights[i] * mul(input.tangent, (float3x3) PM[i]);
			biNormal.xyz += input.Weights[i] * mul(input.binormal, (float3x3) PM[i]);
		}

		output.position = pos;
		output.normal = normal.xyz;
		output.tangent = tangent.xyz;
		output.binormal = biNormal.xyz;
	}
	else
	{
		output.position = input.position;
		output.normal = input.normal;
		output.tangent = input.tangent;
		output.binormal = input.binormal;
	}

	//output.position = float4(output.position.xyz, 1);

	output.position = mul(mul(output.position, scale4x4), World);
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

// --------------------------------------- Pixel shader Math/Helper functons ---------------------------------------------

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

static const float texture_alpha_ref = 0.1f;

void alpha_test(in const float pixel_alpha)
{
	clip(pixel_alpha < 0.7f ? -1 : 1);
	//clip(-1);
}

float nfmod(float a, float b)
{
	return a - b * floor(a / b);
}





float3 getDirectionalLight(in float3 N, in float3 albedo, in float roughness, in float metalness, in float3 viewDir /*, in float3 eyePosition, float3 pos*/)
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
	float3 Li = normalize(light_Direction_Constant);
	//float3 Lradiance = LightData[0].radiannce;
	float3 Lradiance = Directional_Light_Raddiance;

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

//return float4(GlossTex.rrr, 1);

float4 NormalTex = float4(0.5f, 0.5f, 0.5f, 1);
if (UseNormal)
	NormalTex = NormalTexture.Sample(s_normal, input.tex);

float metalness = GlossTex.r; // metal mask channel
//float roughness = _linear(GlossTex.g);  // roughness channel
float roughness = pow(GlossTex.g, 2.2f);  // roughness channel

float3 bumpNormal = getBlueNormal(input);

if (UseNormal == false)
{
	bumpNormal = _input.normal;
	//return float4(1, 0, 0, 1);
}

float3 envColor = getAmbientLight(
	bumpNormal,
	normalize(input.viewDirection),
	DiffuseTex.rgb * TintColour,
	roughness,
	metalness,
	float4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1)
	);


// directional ligting color    
float3 dirColor = getDirectionalLight(
	bumpNormal,
	DiffuseTex.rgb * TintColour,
	roughness,
	metalness,
	normalize(input.viewDirection)

);

// add environman (ambient) light to directional light
float3 color = envColor + dirColor;

if (UseAlpha == 1)
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