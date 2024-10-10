#ifndef _CAMETALROUGHHELPER_
#define _CAMETALROUGHHELPER_

#include "../Shared/const_layout.hlsli"
#include "../inputlayouts.hlsli"
#include "../TextureSamplers.hlsli"
#include "../helpers/mathfunctions.hlsli"
#include "../helpers/mathconstants.hlsli"

//#define MAXTBN float3x3 ( normalize ( input.Tgt ) , normalize ( input.Nml ) , normalize ( input.Btgt ) );
#define MAXTBN float3x3 ( normalize ( input.Tgt ) , normalize ( input.Btgt ) , normalize ( input.Nml ) );

////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////                                              ///////////////////////////
///////////////////////////    Parameters                                ///////////////////////////
///////////////////////////                                              ///////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////

//// ----------------------------------- Per Frame --------------------------------------
//cbuffer UpdatePerFrame : register(b0)
//{
//    float4x4 vMatrixI : ViewInverse < string UIWidget = "None"; >;

//};

//// --------------------------------------- Per Object -----------------------------------------
//cbuffer UpdatePerObject : register(b1)
//{
//    int texcoord0 : Texcoord <int Texcoord = 0; int MapChannel = 1;> = 0;
//    int texcoord1 : Texcoord <int Texcoord = 1; int MapChannel = 2;> = 0;
//    int texcoord2 : Texcoord <int Texcoord = 2; int MapChannel = 3;> = 0;
//    int texcoord3 : Texcoord <int Texcoord = 3; int MapChannel = 4;> = 0;
//    int texcoord4 : Texcoord <int Texcoord = 4; int MapChannel = 0;> = 0;
//    int texcoord5 : Texcoord <int Texcoord = 5; int MapChannel = -2;> = 0;

//    float4x4 wMatrix : World < string UIWidget = "None"; >;
//    float4x4 wvpMatrix : WorldViewProjection < string UIWidget = "None"; >;
//    float4x4 wvMatrixI : WORLDVIEWINVERSE < string UIWidget="None"; >;
//    float4x4 vMatrix : VIEW < string UIWidget="None"; >;
//    float3 CameraPos : WORLDCAMERAPOSITION;
//};

//// ----------------------------------- Lights --------------------------------------
//cbuffer UpdateLights : register(b3)
//{
//    float3 light_color0 : LIGHTCOLOR
//		<
//			string UIWidget = "None";
//			int LightRef = 1;
//		> = { 1.0, 1.0, 1.0 };

//    float3 lightDir : DIRECTION
//		<
//			string UIName =  "Light 0 Direction";
//			string Space = "World";
//			string UIWidget = "None";
//			int RefID = 1;
//			string Object = "TargetLight";
//		> = { 0.0, -1.0, 0.0 };
//};

//// Orthogonalize tangent and bitangent on each pixel, otherwise use the interpolated values.
//// This parameter is controlled by 3ds Max according to the "Tangents and Bitangents" preference.
//bool orthogonalizeTangentBitangentPerPixel
//<
//	string UIName = "Orthogonalize per Pixel (Set by 3ds Max)";
//> = false;

//	In the metalness scheme, all non-metals have the same reflectivity value.  Value chosen to match up with Marmoset.  Calibration done with Mauro.
static const float3 Non_Metals_Reflectivity = float3(0.04f, 0.04f, 0.04f);

//	Tone mapping values...
static const float Tone_Map_Black = 0.001;
static const float Tone_Map_White = 1.0f;
static const float g_whiteout_start = 0.0f;
static const float g_whiteout_end = 1.0f;

static const float low_tones_scurve_bias = 0.1f;
static const float high_tones_scurve_bias = 0.2f;
//static const float low_tones_scurve_bias = 0.5f;
//static const float high_tones_scurve_bias = 0.7f;

static const float g_tone_mapping_brightness = 3.0;
static const float g_tone_mapping_burn = 0.04;

//float3 GetPixelNormal(PixelInputType input)
//{
//	//float3x3 basis = float3x3(normalize(input.tangent), normalize(input.normal), normalize(input.binormal));      // TODO: some games have this, sometimes enables, sometimes as as comment,
//    float3x3 basis = float3x3(normalize(input.tangent.xyz), normalize(input.binormal.xyz), normalize(input.normal.xyz)); // works in own shader

//    float4 NormalTex = NormalTexture.Sample(s_normal, input.tex);

//    // -------------------------------------------------------------------------------------------------------------------
//    // decode the "orange 2d normal map", into a standard "blue" 3d normal map using an othogonal projection
//    // -------------------------------------------------------------------------------------------------------------------
//    float3 deQuantN;
//    deQuantN.x = NormalTex.r * NormalTex.a;
//    deQuantN.y = 1.0 - NormalTex.g;
    
//    deQuantN = (deQuantN * 2.0f) - 1.0f;
//    deQuantN.z = sqrt(1 - deQuantN.x * deQuantN.x - deQuantN.y * deQuantN.y);
    	
//    return normalize(mul(normalize(deQuantN.xyz), basis));
//}

////////////////////////////////////////////////////////////////////////////////////////////////////
//	Inputs/Outputs
////////////////////////////////////////////////////////////////////////////////////////////////////

//struct APP_INPUT
//{
//    float4 Position : POSITION;
//    float3 Normal : NORMAL;
//    float3 Tangent : TANGENT;
//    float3 Bitangent : BINORMAL;
//    float4 TexCoord0 : TEXCOORD0;
//    float4 TexCoord1 : TEXCOORD1;
//    float3 Color : TEXCOORD4;
//    float Alpha : TEXCOORD5;
//};

//struct VS_OUTPUT
//{
//    float4 Position : SV_Position;
//    float4 TexCoord : TEXCOORD0;
//    float3 I : TEXCOORD1;
//    float3 Tgt : TEXCOORD2;
//    float3 Btgt : TEXCOORD3;
//    float3 Nml : NORMAL;
//    float3 Wpos : TEXCOORD5;
//    float4 Color : TEXCOORD6;
//};

/////////////////////////
// parameters
/////////////////////////

// sampler states

//texture list
/*
t_base_colour
t_normal
t_detail_normal
t_roughness
t_metalness
t_mask1
t_mask2
t_mask3
t_ambient_occlusion
t_dirtmap_uv2
t_alpha_mask
t_decal_diffuse
t_decal_normal
t_decal_mask
t_decal_dirtmap
t_decal_dirtmask
t_diffuse_damage
t_anisotropy
t_soft_normal
t_curvature
t_translucency
t_hdr_environment_map
t_hdr_ambient
*/

//SamplerState MMMLWWWSampler
//{
//    Filter = MIN_MAG_MIP_LINEAR;
//    AddressU = WRAP;
//    AddressV = WRAP;
//    AddressW = WRAP;
//};

//SamplerState ReflectSampler;
//SamplerState s_base_colour;
//SamplerState s_normal;
//SamplerState s_detail_normal;
//SamplerState s_roughness;
//SamplerState s_metalness;
//SamplerState s_mask1;
//SamplerState s_mask2;
//SamplerState s_mask3;
//SamplerState s_ambient_occlusion;
//SamplerState s_dirtmap_uv2;
//SamplerState s_alpha_mask;
//SamplerState s_decal_diffuse;
//SamplerState s_decal_normal;
//SamplerState s_decal_mask;
//SamplerState s_decal_dirtmap;
//SamplerState s_decal_dirtmask;
//SamplerState s_diffuse_damage;
//SamplerState s_anisotropy;
//SamplerState s_soft_normal;
//SamplerState s_curvature;
//SamplerState s_translucency;

//SamplerState s_cubemap
//{
//    Filter = ANISOTROPIC;
//    AddressU = CLAMP;
//    AddressV = CLAMP;
//    AddressW = CLAMP;
//};
////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////                                              ///////////////////////////
///////////////////////////    Functions                                 ///////////////////////////
///////////////////////////                                              ///////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////

//  Our directional light is the sun and its rays are not perfectly parallel.
//  Value taken from http://en.wikipedia.org/wiki/Angular_diameter.
float get_sun_angular_diameter()
{
//    float   suns_angular_diameter   = 0.5f; //  The Sun's diameter viewed from the Earth measured in degrees.
//    float   suns_angular_diameter   = 2.0f; //  The Sun's diameter viewed from the Earth measured in degrees. (LDR/GAME FIX).  We need a more pronounced highlight on highly curved surface. Surface quantisation of normals is resulting in the highlight falling between the cracks.
    const float suns_angular_diameter = 1.0f; //  The Sun's diameter viewed from the Earth measured in degrees. (LDR/GAME FIX).  We need a more pronounced highlight on highly curved surface. Surface quantisation of normals is resulting in the highlight falling between the cracks.

    return radians(suns_angular_diameter);
}

float get_sun_angular_radius()
{
    return 0.5f * get_sun_angular_diameter();
}

//	The amount of light reaching a surface from the unit hemisphere is the integral of
//	(2 * pi * sin(theta) * cos(theta)) over the range zero to pi/2.  The solution of this
//	integral is pi.  Therefore the integral of this same function, over the angular radius
// 	of the sun, divided by the full integral, pi, gives the normalised zero to one
//	sun diffuse scaling factor, for any Sun.
//	Integrate[2*p*Sin[?]*Cos[?],{?,0,x}]/p == Sin(?) * Sin(?)
//  Please note that x here is the radius of the sun disk.
float get_sun_diffuse_hemisphere_integral_normalised()
{
    float sine_sun_radius = sin(get_sun_angular_radius());

    return sine_sun_radius * sine_sun_radius;
}

float get_luminance(in float3 colour)
{
    float3 lumCoeff = float3(0.299, 0.587, 0.114);
    float luminance = dot(colour, lumCoeff);
    return saturate(luminance);
}

float3 get_adjusted_faction_colour(in float3 colour)
{
    float3 fc = colour;
    float lum = get_luminance(fc);
    float dark_scale = 1.5;
    float light_scale = 0.5;

    fc = fc * (lerp(dark_scale, light_scale, lum));

    return fc;
}

////////////////////////////////////////////////////////////////////////////////////////////////////
//	Maxconvert Functions
////////////////////////////////////////////////////////////////////////////////////////////////////

float3 texcoordEnvSwizzle(in float3 ref)
{
    // USER EDIT (me):
    return float3(ref.x, ref.y, ref.z);

//this should match the order of the basis
    return float3(ref.x, ref.z, ref.y);
}

#if 0
float3 normalSwizzle(in float3 ref)
{
    // USER EDIT (me):
    return float3(ref.x, ref.y, ref.z);

    return float3(ref.y, ref.x, ref.z);
}

float3 normalSwizzle_UPDATED(in float3 ref)
{
    // USER EDIT (me):
    return float3(ref.x, ref.y, ref.z);

    return float3(ref.y, ref.x, ref.z);

}
#endif

float3 _CA_ONE_texcoordEnvSwizzle(in float3 ref)
{
//this should match the order of the basis
    return float3(ref.x, ref.z, ref.y);
}

float3 _CA_ONE_normalSwizzle(in float3 ref)
{
    return float3(ref.y, ref.x, ref.z);
}

float3 _CA_ONE_normalSwizzle_UPDATED(in float3 ref)
{
    return float3(ref.y, ref.x, ref.z);
}

float cos2sin(float x)
{
    return sqrt(1 - x * x);
}

float cos2tan2(float x)
{
    return (1 - x * x) / (x * x);
}

float contrast(float _val, float _contrast)
{
    _val = ((_val - 0.5f) * max(_contrast, 0)) + 0.5f;
    return _val;
}

////////////////////////////////////////////////////////////////////////////////////////////////////
//	Forward Declarations
////////////////////////////////////////////////////////////////////////////////////////////////////

float3 tone_map_linear_hdr_to_linear_ldr(in float3 linear_hdr, in float black_point, in float white_point);

float3 tone_map_linear_hdr_to_linear_ldr_reinhard(in float3 linear_hdr);

//old///////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////
//	Lighting Functions
////////////////////////////////////////////////////////////////////////////////////////////////////

float3 get_sun_colour()
{
	//	Substance Painter and Designer default lighting strength.
    const float default_max_sun_colour_scale = 20000.0f * LightMult;
    
    return default_max_sun_colour_scale;
}


float3 get_environment_colour(in float3 direction, in float lod)
{
    const float specularCubeMapBrightness = 0.261f;
    
    return tex_cube_specular.SampleLevel(SampleType, direction, lod).rgb * specularCubeMapBrightness * LightMult;
}

//	Ambient diffuse
float3 cube_ambient(in float3 N)
{	
    const float diffuseCubeMapBrightness = 0.261f;
    
    return tex_cube_diffuse.Sample(SampleType, N).rgb * diffuseCubeMapBrightness * LightMult;
}

// Diffuse
// N = Normal (normalized)
// L = Light's direction (from Light, normalized)
float2 phong_diffuse(in float3 N,
						in float3 L)
{ //returns float2( N.-L, N.-L > 0 )

    const float factor = max(0.0f, dot(N, -L));
    return float2(factor, (factor > 0.0f));
}

// Specular
// I = Incident ray (from Eye, normalized) ; N = Normal (normalized) ; shininess = specular exponent
// L = Light's direction (from Light, normalized)
float phong_specular(in float3 I, in float3 N, in float shininess,
						in float3 L)
{

    const float3 R = reflect(L, N);
    return saturate(pow(max(0.0f, dot(R, -I)), shininess));
}

// Specular
float aniso_specular(in float3 I, float3 N, in float3 T, in float shininess,
						in float3 L)
{
    float3 nH = normalize(I + L);
    float3 nT = normalize(T);
	//gram schmidt orthoganalise
    nT = normalize(nT - N * dot(N, nT));
    float spec = pow(sqrt(1 - (pow(dot(nT, nH), 2))), shininess);

    return spec;
}

float blinn_specular(in float3 I, in float3 N, in float shininess,
						in float3 L)
{
    //match blinn exponent to phong exponent to ensure data is uniform
    shininess = shininess * 4.0;
    float3 H = normalize(I + L);
    const float3 R = reflect(L, N);
    return saturate(pow(max(0.0f, dot(N, -H)), shininess));
}

float blinn_phong_specular(in float dotNH, in float SpecularExponent)
{
    float D = pow(dotNH, SpecularExponent) * (SpecularExponent + 1.0f) / 2.0f;
    return D;
}
////////////////////////////////////////////////////////////////////////////////////////////////////
//	Cook Torrance Model
////////////////////////////////////////////////////////////////////////////////////////////////////

float beckmann_distribution(in float dotNH, in float SpecularExponent)
{
    float invm2 = SpecularExponent / 2.0f;
    float D = exp(-cos2tan2(dotNH) * invm2) / pow(dotNH, 4.0f) * invm2;
    return D;
}

float3 fresnel_optimized(in float3 R, in float c)
{
    float3 F = lerp(R, saturate(60.0f * R), pow(1.0f - c, 4.0f));
    return F;
}

float3 fresnel_full(in float3 R, in float c)
{
    // convert reflectance R into (real) refractive index n
    float3 n = (1 + sqrt(R)) / (1 - sqrt(R));
    // then use Fresnel eqns to get angular variance
    float3 FS = (c - n * sqrt(1 - pow(cos2sin(c) / n, 2))) / (c + n * sqrt(1 - pow(cos2sin(c) / n, 2)));
    float3 FP = (sqrt(1 - pow(cos2sin(c) / n, 2)) - n * c) / (sqrt(1 - pow(cos2sin(c) / n, 2)) + n * c);
    return (FS * FS + FP * FP) / 2;
}

////////////////////////////////////////////////////////////////////////////////////////////////////
//	decal / dirt	-  taken from RigidUtil.hlsl
//   Will not work as is because 'in out' seems not to work in 3dsmax.?
////////////////////////////////////////////////////////////////////////////////////////////////////

//void ps_common_blend_decal(in float3 colour, in float3 normal, in float3 specular, out float3 ocolour, out float3 onormal, out float3 ospecular, in float2 uv, in float decal_index, in float4 uv_rect_coords, in float valpha)
//{
//    float2 decal_top_left = uv_rect_coords.xy;
//    float2 decal_dimensions = uv_rect_coords.zw - uv_rect_coords.xy;

//	// Find corresponding decal uv sample coords
//    float2 decal_uv = (uv - decal_top_left) / decal_dimensions;

//    float4 decal_diffuse;
//    float3 decal_normal;

//	//TEX decal_diffuse = tex2D( s_decal_diffuse, decal_uv ).rgba;
//    decal_diffuse = t_decal_diffuse.Sample(s_decal_diffuse, decal_uv).rgba;
//	//TEX decal_normal = normalSwizzle_UPDATED ( ( tex2D( s_decal_normal, decal_uv ).xyz * 2.0f ) - 1.0f );
//    decal_normal = normalSwizzle_UPDATED((t_decal_normal.Sample(s_decal_normal, decal_uv).xyz * 2.0f) - 1.0f);

//	//float decal_mask = tex2D( s_decal_mask, uv ).a;
//    float decal_mask = t_decal_mask.Sample(s_decal_mask, uv).a;

//    float decalblend = decal_mask * decal_diffuse.a * valpha;

//    ocolour = float4(1, 1, 1, 1);
//    onormal = float4(0, 0, 1, 1);
//    ospecular = lerp(specular, decal_diffuse.rgb * 0.5, decalblend);
//	// Blend diffuse
//    ocolour.rgb = lerp(colour.rgb, decal_diffuse.rgb, decalblend);
//	//ocolour = float4(0.2,0.2,0.2,1);
//	// Blend normal
//    onormal.xyz = lerp(onormal.xyz, decal_normal.rgb, decalblend);
//    onormal.xyz = float3(normal.xy + onormal.xy, normal.z);
//}

//void ps_common_blend_dirtmap(in out float3 colour, in out float3 normal, in float3 specular, out float3 ocolour, out float3 onormal, out float3 ospecular, in float2 uv, in float2 uv_offset)
//{
//	//offset masking:
//    uv_offset = uv_offset * float2(i_random_tile_u, i_random_tile_v);
//	// Compute Dirt, first get dirt mask that's associated with the rigid

//	//TEX float mask_alpha = tex2D( s_decal_dirtmask, uv).a;
//    float mask_alpha = t_decal_dirtmask.Sample(s_decal_dirtmask, uv).a;

//	//TEX float4 dirtmap = tex2D( s_decal_dirtmap, ((uv)+uv_offset)*float2(f_uv2_tile_interval_u,f_uv2_tile_interval_v)).xyzw;
//    float4 dirtmap = t_decal_dirtmap.Sample(s_decal_dirtmap, ((uv) + uv_offset) * float2(f_uv2_tile_interval_u, f_uv2_tile_interval_v)).xyzw;

//	//global strength
//    float d_strength = 1.0;

//	// xy holds the normal
//	// Do this carefully!
//	//expand range
//    float2 dirt_normal = (float2(dirtmap.x, dirtmap.y) * 2) - 1;

//	// w holds the mask;
//    float dirt_alpha = dirtmap.w;
//    float dirt_alpha_blend = mask_alpha * dirt_alpha * d_strength;

//	// fetch dirt colour
//    float3 dirt_colour = float3(0.03, 0.03, 0.02);
//    ocolour = colour;
//    onormal = normal;
//	// Blend in dirt colour
//    ocolour.rgb = lerp(colour.rgb, dirt_colour, dirt_alpha_blend);

//	//blend in dirt spec
//    ospecular = lerp(specular, dirt_colour, dirt_alpha_blend);

//	//Blend in dirt normal
//    onormal.xz += (dirt_normal.xy * mask_alpha * d_strength);
//    onormal = normalize(onormal);
//}

//void ps_common_blend_vfx(in out float3 colour, in out float3 normal, in float3 specular, out float3 ocolour, out float3 onormal, out float3 ospecular, in float2 uv, in float2 uv_offset)
//{
//	//offset masking:
//    uv_offset = uv_offset * float2(i_random_tile_u, i_random_tile_v);
//	// Compute Dirt, first get dirt mask that's associated with the rigid

//	//TEX float4 dirtmap = tex2D( s_decal_dirtmap, ((uv)+uv_offset)*float2(f_uv2_tile_interval_u,f_uv2_tile_interval_v)).xyzw;
//    float4 dirtmap = t_decal_dirtmap.Sample(s_decal_dirtmap, ((uv) + uv_offset) * float2(f_uv2_tile_interval_u, f_uv2_tile_interval_v)).xyzw;

//    ocolour = float4(lerp(colour.rgb, dirtmap.rgb, dirtmap.a), 1);
//	//ocolour = colour;
//    onormal = normal;
//    ospecular = specular;
//}

////////////////////////////////////////////////////////////////////////////////////////////////////
//	STANDARD_LIGHTING_MODEL
////////////////////////////////////////////////////////////////////////////////////////////////////

//	Taken straight from the game's lighting.hlsl.  Parameters that are not supported in Max are
//	commented out.
struct R2_5_StandardLightingModelMaterial_For_GBuffer
{
    //  Exogenous lighting parameters...
    float3 Base_Colour; // 0..1
    float3 Normal; // -1..+1
    float Roughness; // 0..1 (Smooth to Rough)
    float Metalness; // 0..1
    float Ambient_Occlusion; // 0..1 (Fully occluded to fully unoccluded)

    //  Misc...
//	uint    BitFlags;           //  0 - 255 only.  Ultimately stored into an 8-bit colour channel of a gbuffer RT.  Flags defined above.
//    uint    PixelSelectionID;
};

struct R2_5_StandardLightingModelMaterial_For_Lighting
{
    //  Exogenous lighting parameters...
    float3 Diffuse_Colour; // 0..1
    float3 Specular_Colour; // 0..1
    float3 Normal; // -1..+1
    float Smoothness; // 0..1 (Rough to Smooth)
    float Ambient_Occlusion; // 0..1 (Fully occluded to fully unoccluded)

    // Endogenous lighting params...
//	float   Depth;
    float Shadow; // 0..1
    float SSAO; // 0..1 (Fully occluded to fully unoccluded)

    //  Misc...
//	uint    BitFlags;           //  0 - 255 only.  Ultimately stored into an 8-bit colour channel of a gbuffer RT.  Flags defined above.
//    uint    PixelSelectionID;
};

R2_5_StandardLightingModelMaterial_For_GBuffer
R2_5_create_standard_lighting_material_for_gbuffer
(
    in float3 Base_Colour,
    in float3 Normal,
	in float Roughness,
	in float Metalness,
	in float Ambient_Occlusion
)
{
    R2_5_StandardLightingModelMaterial_For_GBuffer material;

    material.Base_Colour = Base_Colour;
    material.Normal = Normal;
    material.Roughness = Roughness;
    material.Metalness = Metalness;
    material.Ambient_Occlusion = Ambient_Occlusion;

    return material;
}

R2_5_StandardLightingModelMaterial_For_Lighting R2_5_get_slm_for_lighting(in const R2_5_StandardLightingModelMaterial_For_GBuffer slm_compressed)
{
    R2_5_StandardLightingModelMaterial_For_Lighting slm_uncompressed;

	//	This maths is needed to compensate for the current lighting model where the diffuse component is scaled by the reverse
	//	of the specular component.  In the metalness model, the metalness value is the arbiter of this ratio.  Since metalness
	//	is not (yet) passed down then this compensation is required to ensure that the diffuse component ends up as
	//	( 1.0f - metalness ) x ( 1.0f - Non_Metals_Reflectivity ) x Diffuse lighting.
    slm_uncompressed.Specular_Colour = lerp(Non_Metals_Reflectivity, slm_compressed.Base_Colour, slm_compressed.Metalness);
    slm_uncompressed.Diffuse_Colour = slm_compressed.Base_Colour * (1.0f.xxx - slm_compressed.Metalness.xxx) * (1.0f.xxx - Non_Metals_Reflectivity) / max((1.0f.xxx - slm_uncompressed.Specular_Colour.rgb), 0.001f.xxx);
    slm_uncompressed.Normal = slm_compressed.Normal;
    slm_uncompressed.Smoothness = saturate(1.0f - slm_compressed.Roughness);
    slm_uncompressed.Ambient_Occlusion = slm_compressed.Ambient_Occlusion;
    slm_uncompressed.Shadow = 1.0f;
    slm_uncompressed.SSAO = 1.0f;

    return slm_uncompressed;
}

//  From http://en.wikipedia.org/wiki/Error_function.
float get_error_func_a_value()
{
    return (8.0f * (pi - 3.0f)) / (3 * pi * (4.0f - pi));
}

//  From http://en.wikipedia.org/wiki/Error_function.
float erf(float x)
{
    float x_squared = x * x;

    float a = get_error_func_a_value();

    float a_times_x_squared = a * x_squared;

    float numerator = (4.0f * one_over_pi) + (a_times_x_squared);

    float denominator = 1.0f + a_times_x_squared;

    float main_term = -1.0f * x_squared * (numerator / denominator);

    return sign(x) * sqrt(1 - exp(main_term));
}

//  From http://en.wikipedia.org/wiki/Error_function.
//  For a given cumulative probability (y-axis value) finds the corresponding x-axis value (standard deviation) on
//  the error function that gives this y-axis probability value...
float erfinv(float x)
{
    float one_over_a = 1.0f / get_error_func_a_value();

    float log_1_minus_x_squared = log(1.0f - (x * x));

    float root_of_first_term = (2.0f * one_over_pi * one_over_a) + (log_1_minus_x_squared * 0.5f);

    float first_term = root_of_first_term * root_of_first_term;

    float second_term = log_1_minus_x_squared * one_over_a;

    float third_term = (2.0f * one_over_pi * one_over_a) + (log_1_minus_x_squared * 0.5f);

    float all_terms = first_term - second_term - third_term;

    return sign(x) * sqrt(sqrt(first_term - second_term) - third_term);
}

//  From http://en.wikipedia.org/wiki/Normal_distribution#Numerical_approximations_for_the_normal_CDF.
//  This is the integral from x (standard deviation) of minus infinity to a given x (standard deviation) value.
//  This integral tells  us the probability of finding a variable within this range...
float norm_cdf(float x, float sigma)
{
    float one_over_root_two = 0.70710678118654752440084436210485f;

    return 0.5f * (1.0f + erf(x * (1.0f / sigma) * one_over_root_two));
}

//  Same as the above but for only the area enclosed by ( x_min , x_max )...
float norm_cdf(float x_min, float x_max, float sigma)
{
    float min_summed_area = norm_cdf(x_min, sigma);
    float max_summed_area = norm_cdf(x_max, sigma);

    return max_summed_area - min_summed_area;
}

//  From http://en.wikipedia.org/wiki/Normal_distribution#Numerical_approximations_for_the_normal_CDF.
//  This function determines the sigma value of a normal distribution curve that has a given area under
//  the curve with a given x-axis (standard deviation) value.
float norminv_sigma(float x, float area_under_the_graph)
{
    //  We need to find the value that needs to be passed to the error function that
    //  gives us the area that we are looking for...
    float error_func_x = erfinv((2.0f * area_under_the_graph) - 1);

    //  We know that error_func_x = ( x / ( sigma * sqrt (2) ) ).  Rearrange for sigma...
    float sigma = x / (error_func_x * 1.4142135623730950488016887242097f);

    //  We're done!
    return sigma;
}

//  This returns the sigma value for the a normal distribution function such that the area bound
//  between -x_half_distance_from_origin and +x_half_distance_from_origin is area_under_graph_centred_around_origin.
float get_normal_distribution_sigma_about_origin(float area_under_graph_centred_around_origin, float x_half_distance_from_origin)
{
    float area_from_x_neg_infinity = 0.5f + (0.5f * area_under_graph_centred_around_origin);

    return norminv_sigma(x_half_distance_from_origin, area_from_x_neg_infinity);
}

//  This function returns a value in the range ( 0 - 1 )...
float determine_fraction_of_facets_at_reflection_angle(in float smoothness, in float light_vec_reflected_view_vec_angle, in float min_fraction)
{
    //  The sun's angular radius is important because it accounts for a small but
    //  significant divergence in the light rays that reach the surface of the earth...
    float sun_angular_radius = get_sun_angular_radius();

	//	Transform smoothness directly to sigma.  Sigma values chosen from excel
	//	plot of values from previous complicated calculation.  Sigmas consistent
	//	with R2_4...
    float rough_sigma = 1.1284f;
    float smooth_sigma = 0.00315102f;
    float sigma = lerp(rough_sigma, smooth_sigma, smoothness);

    //  Determine the proportion of the faces that will be seen by the viewer...

    float proportion_of_facets = norm_cdf(light_vec_reflected_view_vec_angle - sun_angular_radius, light_vec_reflected_view_vec_angle + sun_angular_radius, sigma);

    //	We need to normalise the result to the number of facets in the -half pi to + half pi range...
    float all_facets = norm_cdf(-pi * 0.5f, +pi * 0.5f, sigma);

	//	Determine number of facets in one dimension...
    float one_d_facets = saturate(proportion_of_facets / all_facets);

    //  The number of facets is a two dimensional function, which gives a circular area
	//	on the 3d guassian distribution.  Since the distribution is symmetrical, the
	//	area is the result of the one dimensional result is the area of the circle, which
	//	is the area of the sun projected onto the 2d guassian distribution...

    return pi * 0.25f * one_d_facets * one_d_facets;
}

// w is either Ln or Vn
float G1(float ndw, float k)
{
// One generic factor of the geometry function divided by ndw
// NB : We should have k > 0
    return 1.0f / (ndw * (1.0f - k) + k);
}

// Schlick with Smith-like choice of k
// cf http://blog.selfshadow.com/publications/s2013-shading-course/karis/s2013_pbs_epic_notes_v2.pdf p3
// visibility is a Cook-Torrance geometry function divided by (n.l)*(n.v)
float visibility(float ndl, float ndv, float Roughness)
{
    float k = Roughness * Roughness * 0.5f;
    return G1(ndl, k) * G1(ndv, k);
}

//	As a material becomes more rough, it also becomes more like a diffuse material.
float determine_facet_visibility_direct_light(in float roughness, in float3 normal_vec, in float3 light_vec, in float3 view_vec)
{
    float n_dot_l = saturate(dot(normal_vec, light_vec));
    float towards_diffuse_surface = sin(roughness * pi * 0.5f); //	( 0 - 1 ) output...
    float facet_visibility = lerp(1.0f, n_dot_l, towards_diffuse_surface);

    return saturate(visibility(n_dot_l, saturate(dot(normal_vec, view_vec)), roughness) / 4.0f) * facet_visibility;
}

float determine_facet_visibility_env_light(in float roughness, in float3 normal_vec, in float3 light_vec, in float3 view_vec)
{
    float n_dot_l = saturate(dot(normal_vec, light_vec));

    return saturate(visibility(n_dot_l, saturate(dot(normal_vec, view_vec)), roughness) / 4.0f);
}

//	http://blog.selfshadow.com/publications/s2013-shading-course/karis/s2013_pbs_epic_notes_v2.pdf
float3 fresnel_spherical_gaussian_schlick(in float3 material_reflectivity, in float3 light_vec, in float3 view_vec)
{
    float3 Hn = normalize(view_vec + light_vec);

    float vdh = saturate(dot(view_vec, Hn));

    float sphg = pow(2.0f, (-5.55473f * vdh - 6.98316f) * vdh);

    return material_reflectivity + (float3(1.0f, 1.0f, 1.0f) - material_reflectivity) * sphg;
}

//	https://en.wikipedia.org/wiki/Schlick's_approximation
float3 fresnel_schlick(in float3 material_reflectivity, in float3 light_vec, in float3 view_vec)
{
	//	R0 term...
    float3 R0 = material_reflectivity;

	//	Half vector (normal)...
    float3 N = normalize(light_vec + view_vec);

	//	Schlick Fresnel increase of reflectivity...
    float3 Fresnel = (((1.0f - R0) * pow(1.0f - saturate(dot(N, view_vec)), 5.0f)));

	//	Return the total reflectivity...
    return R0 + Fresnel;
}

// https://en.wikipedia.org/wiki/Schlick%27s_approximation, where n1 = 1.0f (air) n2 = 1.4
float3 determine_surface_reflectivity(in float3 material_reflectivity, in float3 light_vec, in float3 view_vec)
{
	//	Assumes an opengl like view vector...
    view_vec = -view_vec;

    return fresnel_spherical_gaussian_schlick(material_reflectivity, light_vec, view_vec);
}

//  Determines the reflectivity of a surface given light and view vectors.  At glancing angles all materials reflect fully...
//  Inspired by fresnel_optimized function.  Values chosen to match marmoset.
// AE EDIT: Added "roughness" param, to be able to use fallback "basic slick"
float3 determine_surface_reflectivity(in float3 material_reflectivity, in float roughness, in float3 light_vec, in float3 view_vec, in float3 normal_vec)
{
#if 0
	// Basic Schlick
	float eye_dot_normal = max(0, dot(normal_vec, view_vec));
	float smoothness = 1 - roughness;
	float smoothness2 = smoothness * smoothness;
	float f = 1 - eye_dot_normal;
	float f2 = f * f;
	float f4 = f2 * f2;

	return lerp(material_reflectivity, 1, f4 * smoothness2);
#else

    return determine_surface_reflectivity(material_reflectivity, light_vec, view_vec);

#endif
}

float4 plot_standard_lighting_model_test_func(in float4 vpos)
{
    float4 g_vpos_texel_offset = float4(0, 0, 0, 0);
    float4 g_screen_size_minus_one = float4(0, 0, 0, 0);

    vpos -= g_vpos_texel_offset.xxxx;

	//  Current normalised pixel coordinates with an origin at the bottom left of the screen...
    float xpos = vpos.x / g_screen_size_minus_one.x * 5.0f;
    float ypos = ((g_screen_size_minus_one.y - vpos.y) / g_screen_size_minus_one.y) * 10.0f;

	//  Plotted y-value...
    float y_value = norminv_sigma(lerp(0.01f, 5.0f, xpos), 0.7);

    return saturate((y_value * g_screen_size_minus_one.y) - (ypos * g_screen_size_minus_one.y)).xxxx;
}

//  Dx10/11 requires trig values to be in the range -1 to +1.  Values outside of this range in dx10/11 result in undefined returned
//  values from acos for example.
float ensure_correct_trig_value(in float value)
{
    return clamp(value, -1.0f, +1.0f);
}

float USER_DistributionGGX(float3 N, float3 H, float a)
{
    float a2 = a * a;
    float NdotH = max(dot(N, H), 0.0);
    float NdotH2 = NdotH * NdotH;

    float nom = a2;
    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
    denom = pi * denom * denom;

    return nom / denom;
}

//  Common functionality pulled out to facilitate minor optimisations.
float3 get_reflectivity_base(in float3 light_vec, in float3 normal_vec, in float3 view_vec, in float3 material_reflectivity, in float smoothness, in float light_vec_reflected_view_vec_angle, in float min_fraction)
{    
    float fraction_of_facets = determine_fraction_of_facets_at_reflection_angle(smoothness, light_vec_reflected_view_vec_angle, min_fraction);            
    
    float facet_visibility = determine_facet_visibility_direct_light(1.0f - smoothness, normal_vec, light_vec, view_vec);    
        
    float3 surface_reflectivity = determine_surface_reflectivity(material_reflectivity, 1 - smoothness, light_vec, view_vec, normal_vec);
    
    return fraction_of_facets * facet_visibility * surface_reflectivity;
}

//  Determines the reflectivity of a surface, for given light, normal, and view vectors, along with a material's standard reflectivity and smoothness.  The reflectivity
//  at red, green, and blue wavelengths can be different, which can result in colour shifts.
float3 get_reflectivity_dir_light(in float3 light_vec, in float3 normal_vec, in float3 view_vec, in float3 reflected_view_vec, in float3 material_reflectivity, in float smoothness)
{
    float light_vec_reflected_view_vec_angle = acos(ensure_correct_trig_value(dot(light_vec, reflected_view_vec)));

    return get_reflectivity_base(light_vec, normal_vec, view_vec, material_reflectivity, smoothness, light_vec_reflected_view_vec_angle, get_sun_diffuse_hemisphere_integral_normalised());
}

float determine_max_fraction_env_fresnel_facets(in float roughness)
{
    const float towards_diffuse_surface = sin(roughness * pi * 0.5f); //	( 0 - 1 ) output...
	// This value can be considered environment "Fresnel Strength" for 100% rough materials. It could be useful in the future to store this into the gbuffer.
    return lerp(1.0f, 0.0f, towards_diffuse_surface);
}

float3 get_reflectivity_env_light(in float3 light_vec, in float3 normal_vec, in float3 view_vec, in float3 material_reflectivity, in float smoothness)
{
    float roughness = 1.0f - smoothness;

    float facet_visibility = determine_facet_visibility_env_light(roughness, normal_vec, light_vec, view_vec);

    float facet_fresnel_facets_max_amount = determine_max_fraction_env_fresnel_facets(roughness);

    float3 surface_reflectivity = lerp(material_reflectivity, determine_surface_reflectivity(material_reflectivity, 1 - smoothness, light_vec, view_vec, normal_vec),
    facet_fresnel_facets_max_amount);

    return facet_visibility * surface_reflectivity;
}

float get_env_map_lod(in float roughness_in)
{
    float smoothness = pow(1.0 - roughness_in, 4.0);

    float roughness = 1.0 - smoothness;

	//	This must be the number of mip-maps in the environment map! EDIT: set slightly lower to "simulate" SSR.
    float texture_num_lods = 7.0f;

    float env_map_lod = roughness * (texture_num_lods - 1.0);

    return env_map_lod;
}

float3 sample_environment_specular(in float roughness_in, in float3 reflected_view_vec)
{
    float env_map_lod = get_env_map_lod(roughness_in);

    return get_environment_colour(reflected_view_vec, env_map_lod);
}

float3 standard_lighting_model_environment_light_SM4_private(in const float3 normalised_view_dir, in const float3 reflected_view_vec, in const R2_5_StandardLightingModelMaterial_For_Lighting material)
{
	//  Specular calculations for environmental light contribution...
    float3 ambient_colour = 0.0f.xxx;

    ambient_colour = cube_ambient(material.Normal);        
    
    float3 env_light_diffuse = ambient_colour * material.Diffuse_Colour * (1.0f - material.Specular_Colour);        
    
	// We always apply cubemap specular reflections here because it gives an initial bounce for ssr
	// SSR then blend with a term correcting for it
	{
		//	This function correctly matches both Marmoset and Substance Painter...
        float3 env_light_pixel_reflectivity = get_reflectivity_env_light(reflected_view_vec, material.Normal, normalised_view_dir, material.Specular_Colour, material.Smoothness);              
        
		//  Specular calculations for environmental light contribution...
        const float roughness = 1 - material.Smoothness;
        float3 environment_colour = sample_environment_specular(roughness, reflected_view_vec);
        float3 env_light_specular_colour = environment_colour * env_light_pixel_reflectivity;              

        float occlusion = min(material.SSAO, material.Ambient_Occlusion);     
                
        return occlusion * (env_light_diffuse + (lerp(occlusion, 1.0, material.Smoothness) * env_light_specular_colour));        
    }
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

float3 standard_lighting_model_directional_light_SM4_private(in const float3 LightColor, in const float3 normalised_light_dir, in const float3 normalised_view_dir, in const float3 reflected_view_vec, in R2_5_StandardLightingModelMaterial_For_Lighting material)
{
#ifndef DEBUGGING_BIT_FLAGS

	//	To match, as close as we can, Substance...
    material.Smoothness = substance_smoothness_get_our_smoothness(material.Smoothness);

    float normal_dot_light_vec = max(0.0f, dot(material.Normal, normalised_light_dir));

	//  Specular calculations for directional light contribution...
    float3 dlight_pixel_reflectivity = get_reflectivity_dir_light(normalised_light_dir, material.Normal, normalised_view_dir, reflected_view_vec, material.Specular_Colour, material.Smoothness);

    float3 dlight_specular_colour = dlight_pixel_reflectivity * LightColor;
    float3 dlight_material_scattering = 1.0f - max(dlight_pixel_reflectivity, material.Specular_Colour); //  All photons not accounted for by reflectivity are accounted by scattering. From the energy difference between in-coming light and emitted light we could calculate the amount of energy turned into heat. This energy would not be enough to make a viewable difference at standard illumination levels. 

	//  Diffuse contribution from directional light...
    float3 dlight_diffuse = material.Diffuse_Colour * normal_dot_light_vec * LightColor * dlight_material_scattering;

	//  Scale the diffuse component of the sun's light to account for the fact that the sun does not cover
	//	the entire hemisphere...
    dlight_diffuse *= get_sun_diffuse_hemisphere_integral_normalised();

	// Backscattering
    float3 backscattering = 0;
#if 0
	if (material.BitFlags & BACKSCATTERING)
	{
		const float curvature = 0.5f;
		const float scale = 0.2f;
		float distance = abs(dot(material.Normal, normalised_light_dir)) * scale;
		float3 extinction = pow(float3(0.4, 0.6f, 0.4f), distance + 0.0001f);
		const float inscattering = 0.25f;// (1 - exp(-max(distance, curvature * scale) * 0.04f)) * abs(dot(-material.Normal, normalised_light_dir));
										 //cw - 0.3f below chosen by artists
		float3 light = LightColor *0.3f;
		const float directionality = 0.75f;
		//cw - can this light/view term go through a power function to tighten the effect around the sun direction?
		light *= lerp(1, saturate(dot(-normalised_light_dir, normalised_view_dir)), directionality);
		backscattering = extinction * inscattering * light * get_sun_diffuse_hemisphere_integral_normalised() * material.Diffuse_Colour * min ( material.SSAO , material.Ambient_Occlusion );
	}
#endif

    return material.Shadow * (dlight_specular_colour + dlight_diffuse) + backscattering;

#else

	const float is_flag_set = material.BitFlags & IS_SECOND_CLASS_OBJECT;

	return is_flag_set.xxx * 10.0f;

#endif
}

float3 standard_lighting_model_directional_light(in float3 LightColor, in float3 normalised_light_dir, in float3 normalised_view_dir, in R2_5_StandardLightingModelMaterial_For_Lighting material)
{
    float normal_dot_light_vec = max(0.0f, dot(material.Normal, normalised_light_dir));

    float3 reflected_view_vec = reflect(normalised_view_dir, material.Normal);      
        
    float3 env_light = standard_lighting_model_environment_light_SM4_private(normalised_view_dir, reflected_view_vec, material);
    float3 dir_light = standard_lighting_model_directional_light_SM4_private(LightColor, normalised_light_dir, normalised_view_dir, reflected_view_vec, material);

    return env_light + dir_light;
}

void alpha_test(in const float pixel_alpha)
{
    if (UseAlpha == 1)
    {
        clip(pixel_alpha - texture_alpha_ref);
    }
}

//void apply_faction_colours(inout float3 diffuse_colour_rgb, in const float2 tex_coord, in const bool faction_colours)
//{
//    //if (faction_colours)
//    {
        
        
//        //float mask_p1 = t_mask1.Sample(s_mask1, tex_coord).r;
//        //float mask_p2 = t_mask2.Sample(s_mask2, tex_coord).r;
//        //float mask_p3 = t_mask3.Sample(s_mask3    , tex_coord).r;
//        float mask_p1 = shaderTextures[t_Mask].Sample(s_anisotropic, tex_coord).r;
//        float mask_p2 = shaderTextures[t_Mask].Sample(s_anisotropic, tex_coord).g;
//        float mask_p3 = shaderTextures[t_Mask].Sample(s_anisotropic, tex_coord).b;

//	    //faction colours
//        //diffuse_colour_rgb = lerp(diffuse_colour_rgb, diffuse_colour_rgb * _linear(vec4_colour_0.rgb), mask_p1);
//        //diffuse_colour_rgb = lerp(diffuse_colour_rgb, diffuse_colour_rgb * _linear(vec4_colour_1.rgb), mask_p2);
//        //diffuse_colour_rgb = lerp(diffuse_colour_rgb, diffuse_colour_rgb * _linear(vec4_colour_2.rgb), mask_p3);
//        diffuse_colour_rgb = lerp(diffuse_colour_rgb, diffuse_colour_rgb * _linear(mesh_faction_color1.rgb), mask_p1);
//        diffuse_colour_rgb = lerp(diffuse_colour_rgb, diffuse_colour_rgb * _linear(mesh_faction_color2.rgb), mask_p2);
//        diffuse_colour_rgb = lerp(diffuse_colour_rgb, diffuse_colour_rgb * _linear(mesh_faction_color3.rgb), mask_p3);
//    }
//}

//void ps30_get_shared_inputs(out float3 eye_vector, out float3 light_vector, out float4 base_colour, /*out float3 texture_normal, */out float roughness, out float metalness, out float occlusion, /*out float3x3 normal_basis, */in const PixelInputType input, in const bool faction_colours)
//{
//    base_colour = shaderTextures[t_BaseColor].Sample(s_anisotropic, input.tex1.xy);

//    alpha_test(base_colour.a);

//    //eye_vector = -normalize(vMatrixI[3] - input.Wpos);
//    eye_vector = -normalize(input.viewDirection);

//    //light_vector = lightDir;
//    light_vector = -normalize(lightData[0].lightDirection);

//    // metalness in "RED"
//    metalness = shaderTextures[t_MaterialMap].Sample(s_anisotropic, input.tex1.xy).x;

//    //roughness in "GREEN"
//    roughness = shaderTextures[t_MaterialMap].Sample(s_anisotropic, input.tex1.xy).y;
    
//    // Baked Ambient Occlusion in "ALPHA"
//    occlusion = shaderTextures[t_MaterialMap].Sample(s_anisotropic, input.tex1.xy).w;
                                                                                            
//    // TODO: REMOVE DEBUGGING CODE
//    //roughness = 1 - (1 - roughness) * (1 - roughness);

//	//  Get the pixel normal in world-space ensuring that it's in 3dsmax's coordinate system...
//    //normal_basis = MAXTBN
//    //texture_normal = shaderTextures[[t_Normal].Sample(s_anisotropic, input.tex1.xy).rgba;
//    //texture_normal = normalSwizzle_UPDATED((texture_normal.rgb * 2.0f) - 1.0f);

//    //texture_normal = getPixelNormal(input);
//}
////////////////////////////////////////////////////////////////////////////////////////////////////
//	Constants
////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////                                              ///////////////////////////
///////////////////////////    Shaders                                   ///////////////////////////
///////////////////////////                                              ///////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////////////////////////
//	Vertex Shader
////////////////////////////////////////////////////////////////////////////////////////////////////

#if 0
VS_OUTPUT vs30_main(in APP_INPUT input)
{
    VS_OUTPUT output;

    output.Position = mul(input.Position, wvpMatrix);
    output.TexCoord.xy = input.TexCoord0.xy;
    output.TexCoord.zw = input.TexCoord1.xy;

    output.TexCoord.y += 1;
    output.TexCoord.w += 1;

	//output.I = normalize(mul(input.Position,wMatrix) - wvMatrixI[3].xyz );
    //Out.eyeVec = ViewInv[3].xyz -  Out.worldSpacePos;
    // ////////////////////////////////////////////////////////////////////////////
    // this eye vector is correct for max, maybe not for anything else!
    // ////////////////////////////////////////////////////////////////////////////

    output.I = normalize(vMatrixI[3] - mul(input.Position, wMatrix));
	//output.I = vMatrixI[3].xyz;
    output.Tgt = mul(float4(input.Tangent.xyz, 0.0f), wMatrix).xyz;
    output.Btgt = mul(float4(input.Bitangent.xyz, 0.0f), wMatrix).xyz;
    output.Nml = mul(float4(input.Normal.xyz, 0.0f), wMatrix).xyz;
	//output.Nml = float3(0,0,1);
    output.Wpos = mul(input.Position, wMatrix);
    output.Color.rgb = input.Color.rgb;
    output.Color.a = input.Alpha.r;
    return output;
}

////////////////////////////////////////////////////////////////////////////////////////////////////
//	Pixel Shader
////////////////////////////////////////////////////////////////////////////////////////////////////

struct PIXELDATA
{
    float4 Colour : SV_Target;
};

PIXELDATA ps30_main(in VS_OUTPUT input, bool FrontFace : SV_IsFrontFace) : COLOR
{
    PIXELDATA OUT;

    float3 eye_vector;
    float3 light_vector;
    float4 base_colour;
    float3 normal;
    float roughness;
    float metalness;
    float occlusion;
    float3x3 basis;
    bool faction_colours = true;

	//	Get the inputs...
    ps30_get_shared_inputs(eye_vector, light_vector, base_colour, normal, roughness, metalness, occlusion, basis, input, faction_colours);

	//moving to allow blending
    float3 pixel_normal = normalize(mul(normalize(normal), basis));

    //  Create the standard material.  This is what gets written to the gbuffer...
    R2_5_StandardLightingModelMaterial_For_GBuffer standard_mat_compressed = R2_5_create_standard_lighting_material_for_gbuffer(base_colour, pixel_normal, roughness, metalness, occlusion);

	//	Create the uncompressed material.  This is what is read from the gbuffer...
    R2_5_StandardLightingModelMaterial_For_Lighting slm_uncompressed = R2_5_get_slm_for_lighting(standard_mat_compressed);

	//	Apply faction colours...
    //apply_faction_colours(slm_uncompressed.Diffuse_Colour.rgb, input.TexCoord.xy, faction_colours);

    //  Light the pixel...
    float3 hdr_linear_col = standard_lighting_model_directional_light(get_sun_colour(), light_vector, eye_vector, slm_uncompressed);

    //  Tone-map the pixel...
    float3 ldr_linear_col = saturate(tone_map_linear_hdr_to_linear_ldr(hdr_linear_col, Tone_Map_Black, Tone_Map_White));

    //  Return gamma corrected value...
    OUT.Colour = float4(_gamma(ldr_linear_col), 1.0f);
    return OUT;

}

/*

float4 ps30_main_decaldirt ( in VS_OUTPUT input ) : COLOR
{
	float3      eye_vector;
	float3 		light_vector;
	float4      base_colour;
	float3      normal;
	float		roughness;
	float		metalness;
	float		occlusion;
	float3x3    basis;
	bool		faction_colours	= true;

	//	Get the inputs...
	ps30_get_shared_inputs ( eye_vector , light_vector , base_colour, normal, roughness, metalness, occlusion, basis, input , faction_colours );

    //  Create the standard material.  This is what gets written to the gbuffer...
    R2_5_StandardLightingModelMaterial_For_GBuffer	standard_mat_compressed		= R2_5_create_standard_lighting_material_for_gbuffer ( base_colour, normal, roughness, metalness, occlusion );

	//	Create the uncompressed material.  This is what is read from the gbuffer...
	R2_5_StandardLightingModelMaterial_For_Lighting	slm_uncompressed			= R2_5_get_slm_for_lighting ( standard_mat_compressed );

	//	Apply faction colours...
	apply_faction_colours ( slm_uncompressed.Diffuse_Colour.rgb , input.TexCoord.xy , faction_colours );

	//decals/dirt - shield type
	if (b_do_decal)
	{
		ps_common_blend_decal(slm_uncompressed.Diffuse_Colour, normal, slm_uncompressed.Specular_Colour, slm_uncompressed.Diffuse_Colour, normal, slm_uncompressed.Specular_Colour, input.TexCoord.xy, 0, vec4_uv_rect, 1.0);
	}

	if (b_do_dirt)
	{
		ps_common_blend_dirtmap(slm_uncompressed.Diffuse_Colour, normal, slm_uncompressed.Specular_Colour, slm_uncompressed.Diffuse_Colour, normal, slm_uncompressed.Specular_Colour, input.TexCoord.xy, float2(f_uv_offset_u,f_uv_offset_v));
	}

	//moving to allow blending
	slm_uncompressed.Normal		= normalize ( mul ( normalize (normal), basis ) );

    //  Light the pixel...
    float3      hdr_linear_col  = standard_lighting_model_directional_light ( get_sun_colour() , light_vector , eye_vector , slm_uncompressed );

    //  Tone-map the pixel...
    float3      ldr_linear_col  = saturate ( tone_map_linear_hdr_to_linear_ldr ( hdr_linear_col , Tone_Map_Black , Tone_Map_White ) );

    //  Return gamma corrected value...
	return float4 ( _gamma ( ldr_linear_col ) , 1 );
}

float4 ps30_main_custom_terrain ( in VS_OUTPUT input ) : COLOR
{
	float3      eye_vector;
	float3 		light_vector;
	float4      base_colour;
	float3      normal;
	float		roughness;
	float		metalness;
	float		occlusion;
	float3x3    basis;
	bool		faction_colours	= true;

	//	Get the inputs...
	ps30_get_shared_inputs ( eye_vector , light_vector , base_colour, normal, roughness, metalness, occlusion, basis, input , faction_colours );

    //  Create the standard material.  This is what gets written to the gbuffer...
    R2_5_StandardLightingModelMaterial_For_GBuffer	standard_mat_compressed		= R2_5_create_standard_lighting_material_for_gbuffer ( base_colour, normal, roughness, metalness, occlusion );

	//	Create the uncompressed material.  This is what is read from the gbuffer...
	R2_5_StandardLightingModelMaterial_For_Lighting	slm_uncompressed			= R2_5_get_slm_for_lighting ( standard_mat_compressed );

	//	Apply faction colours...
	apply_faction_colours ( slm_uncompressed.Diffuse_Colour.rgb , input.TexCoord.xy , faction_colours );

	//decals/dirt - shield type
	ps_common_blend_decal(slm_uncompressed.Diffuse_Colour, normal, slm_uncompressed.Specular_Colour, slm_uncompressed.Diffuse_Colour, normal, slm_uncompressed.Specular_Colour, input.TexCoord.xy, 0, vec4_uv_rect, 1-input.Color.a);

	//moving to allow blending
	slm_uncompressed.Normal		= normalize ( mul ( normalize (normal), basis ) );

    //  Light the pixel...
    float3      hdr_linear_col  = standard_lighting_model_directional_light ( get_sun_colour() , light_vector , eye_vector , slm_uncompressed );

    //  Tone-map the pixel...
    float3      ldr_linear_col  = saturate ( tone_map_linear_hdr_to_linear_ldr ( hdr_linear_col , Tone_Map_Black , Tone_Map_White ) );

    //  Return gamma corrected value...
	return float4 ( _gamma ( ldr_linear_col ) , 1 );
}

float4 ps30_main_vfx ( in VS_OUTPUT input ) : COLOR
{
	float3      eye_vector;
	float3 		light_vector;
	float4      base_colour;
	float3      normal;
	float		roughness;
	float		metalness;
	float		occlusion;
	float3x3    basis;
	bool		faction_colours	= true;

	//	Get the inputs...
	ps30_get_shared_inputs ( eye_vector , light_vector , base_colour, normal, roughness, metalness, occlusion, basis, input , faction_colours );

    //  Create the standard material.  This is what gets written to the gbuffer...
    R2_5_StandardLightingModelMaterial_For_GBuffer	standard_mat_compressed		= R2_5_create_standard_lighting_material_for_gbuffer ( base_colour, normal, roughness, metalness, occlusion );

	//	Create the uncompressed material.  This is what is read from the gbuffer...
	R2_5_StandardLightingModelMaterial_For_Lighting	slm_uncompressed			= R2_5_get_slm_for_lighting ( standard_mat_compressed );

	//	Apply faction colours...
	apply_faction_colours ( slm_uncompressed.Diffuse_Colour.rgb , input.TexCoord.xy , faction_colours );

	//decals/dirt - shield type
	if (b_do_decal)
	{
		ps_common_blend_decal(slm_uncompressed.Diffuse_Colour, normal, slm_uncompressed.Specular_Colour, slm_uncompressed.Diffuse_Colour, normal, slm_uncompressed.Specular_Colour, input.TexCoord.xy, 0, vec4_uv_rect, 1.0);
	}

	if (b_do_dirt)
	{
		ps_common_blend_dirtmap(slm_uncompressed.Diffuse_Colour, normal, slm_uncompressed.Specular_Colour, slm_uncompressed.Diffuse_Colour, normal, slm_uncompressed.Specular_Colour, input.TexCoord.xy, float2(f_uv_offset_u,f_uv_offset_v));
	}

	ps_common_blend_vfx(slm_uncompressed.Diffuse_Colour, normal, slm_uncompressed.Specular_Colour, slm_uncompressed.Diffuse_Colour, normal, slm_uncompressed.Specular_Colour, input.TexCoord.xy, float2(f_uv_offset_u,f_uv_offset_v));

	//moving to allow blending
	slm_uncompressed.Normal		= normalize ( mul ( normalize (normal), basis ) );

    //  Light the pixel...
    float3      hdr_linear_col  = standard_lighting_model_directional_light ( get_sun_colour() , light_vector , eye_vector , slm_uncompressed );

    //  Tone-map the pixel...
    float3      ldr_linear_col  = saturate ( tone_map_linear_hdr_to_linear_ldr ( hdr_linear_col , Tone_Map_Black , Tone_Map_White ) );

    //  Return gamma corrected value...
	return float4 ( _gamma ( ldr_linear_col ) , 1 );
}

float4 ps30_full_tint_UPDATED ( in VS_OUTPUT input ) : COLOR
{
	return ps30_main(input);
}

float4 ps30_full_ao( in VS_OUTPUT input ) : COLOR
{
	return ps30_main(input);
}

float4 ps30_full_dirtmap( in VS_OUTPUT input ) : COLOR
{
	float3      eye_vector;
	float3 		light_vector;
	float4      base_colour;
	float3      normal;
	float		roughness;
	float		metalness;
	float		occlusion;
	float3x3    basis;
	bool		faction_colours	= true;

	//	Get the inputs...
	ps30_get_shared_inputs ( eye_vector , light_vector , base_colour, normal, roughness, metalness, occlusion, basis, input , faction_colours );

    //  Create the standard material.  This is what gets written to the gbuffer...
    R2_5_StandardLightingModelMaterial_For_GBuffer	standard_mat_compressed		= R2_5_create_standard_lighting_material_for_gbuffer ( base_colour, normal, roughness, metalness, occlusion );

	//	Create the uncompressed material.  This is what is read from the gbuffer...
	R2_5_StandardLightingModelMaterial_For_Lighting	slm_uncompressed			= R2_5_get_slm_for_lighting ( standard_mat_compressed );

	//	Apply faction colours...
	apply_faction_colours ( slm_uncompressed.Diffuse_Colour.rgb , input.TexCoord.xy , faction_colours );

	ps_common_blend_dirtmap(slm_uncompressed.Diffuse_Colour, normal, slm_uncompressed.Specular_Colour, slm_uncompressed.Diffuse_Colour, normal, slm_uncompressed.Specular_Colour, input.TexCoord.xy, float2(f_uv_offset_u,f_uv_offset_v));

	//moving to allow blending
	slm_uncompressed.Normal		= normalize ( mul ( normalize (normal), basis ) );

    //  Light the pixel...
    float3      hdr_linear_col  = standard_lighting_model_directional_light ( get_sun_colour() , light_vector , eye_vector , slm_uncompressed );

    //  Tone-map the pixel...
    float3      ldr_linear_col  = saturate ( tone_map_linear_hdr_to_linear_ldr ( hdr_linear_col , Tone_Map_Black , Tone_Map_White ) );

    //  Return gamma corrected value...
	return float4 ( _gamma ( ldr_linear_col ) , 1 );
}

*/

/////////////////////////////////////////////////////////////
//  TONE MAPPER     /////////////////////////////////////////
/////////////////////////////////////////////////////////////
#endif
//-----------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------
float3 tone_map_linear_hdr_to_linear_ldr_reinhard(in float3 linear_hdr)
{
    float ol = luma(linear_hdr.rgb);
    float l = ol * g_tone_mapping_brightness * g_tone_mapping_brightness;
    float factor = (1.0f / g_tone_mapping_burn) - 0.999f;
    float r = (l * (1 + (l / factor))) / (1.0f + l);
    return float3(linear_hdr.rgb * r / ol);
}

//-----------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------
float3 tone_map_linear_hdr_to_linear_ldr(in float3 linear_hdr, in float black_point, in float white_point)
{
    return tone_map_linear_hdr_to_linear_ldr_reinhard(linear_hdr);
}

////////////////////////////////////////////////////////////////////////////////////////////////////
//	Technique pixel shaders
////////////////////////////////////////////////////////////////////////////////////////////////////
#if 0
PIXELDATA ps30_flatdiffuse(in VS_OUTPUT input) : COLOR
{
    PIXELDATA OUT;
	//get albedo pixel
    float4 Ct = t_base_colour.Sample(MMMLWWWSampler, input.TexCoord.xy);

    alpha_test(Ct.a);

    OUT.Colour = float4(_gamma(Ct.rgb), 1);
    return OUT;
}

PIXELDATA ps30_albedo(in VS_OUTPUT input) : COLOR
{
    PIXELDATA OUT;
	//get albedo pixel
    float4 Ct = t_base_colour.Sample(MMMLWWWSampler, input.TexCoord.xy);
	// this should eventually include faction colouring or other modifiers to the albedo texture

    alpha_test(Ct.a);

    OUT.Colour = float4(_gamma(Ct.rgb), 1);
    return OUT;
}

PIXELDATA ps30_solidalpha(in VS_OUTPUT input) : COLOR
{
    PIXELDATA OUT;
	//get albedo pixel
    float4 Ct = t_base_colour.Sample(MMMLWWWSampler, input.TexCoord.xy);

    OUT.Colour = float4((Ct.aaa), 1.0f);
    return OUT;
}

PIXELDATA ps30_t_roughness(in VS_OUTPUT input) : COLOR
{
    PIXELDATA OUT;
	//get roughness pixel
    float4 roughness_p = t_roughness.Sample(MMMLWWWSampler, input.TexCoord.xy);
    OUT.Colour = float4(roughness_p.rrr, 1.0f);
    return OUT;
}

PIXELDATA ps30_t_specular(in VS_OUTPUT input) : COLOR
{
    PIXELDATA OUT;
	//get substance pixel
    float4 specular_p = t_metalness.Sample(MMMLWWWSampler, input.TexCoord.xy);
    OUT.Colour = float4(_gamma(specular_p.rgb), 1.0f);
    return OUT;
}

PIXELDATA ps30_t_ao(in VS_OUTPUT input) : COLOR
{
    PIXELDATA OUT;
    float3 ao = t_ambient_occlusion.Sample(MMMLWWWSampler, input.TexCoord.xy);
    OUT.Colour = float4(ao.rgb, 1.0f);
    return OUT;
}

PIXELDATA ps30_t_normal(in VS_OUTPUT input) : COLOR
{
    PIXELDATA OUT;
	// get normal and normal map.
	//const float3x3 basis = float3x3( normalize( input.Tgt ), normalize( input.Btgt ), normalize( input.Nml ) );
    float3 N = normalSwizzle(t_normal.Sample(MMMLWWWSampler, input.TexCoord.xy).rgb);

    OUT.Colour = float4(N.rgb, 1.0f);
    return OUT;
}

PIXELDATA ps30_t_mask1(in VS_OUTPUT input) : COLOR
{
    PIXELDATA OUT;
	//get faction mask pixel
    float4 faction_p = t_mask1.Sample(MMMLWWWSampler, input.TexCoord.xy);
    OUT.Colour = float4((faction_p.rrr), 1.0f);
    return OUT;
}

PIXELDATA ps30_t_mask2(in VS_OUTPUT input) : COLOR
{
    PIXELDATA OUT;
	//get faction mask pixel
    float4 faction_p = t_mask2.Sample(MMMLWWWSampler, input.TexCoord.xy);
    OUT.Colour = float4((faction_p.rrr), 1.0f);
    return OUT;
}

PIXELDATA ps30_t_mask3(in VS_OUTPUT input) : COLOR
{
    PIXELDATA OUT;
	//get faction mask pixel
    float4 faction_p = t_mask3.Sample(MMMLWWWSampler, input.TexCoord.xy);
    OUT.Colour = float4((faction_p.rrr), 1.0f);
    return OUT;
}

PIXELDATA ps30_t_cubemap(in VS_OUTPUT input) : COLOR
{
    PIXELDATA OUT;
	// get normal and normal map.
    const float3x3 basis = MAXTBN
    float3 N = normalSwizzle(t_normal.Sample(MMMLWWWSampler, input.TexCoord.xy).rgb * 2.0f - 1.0f);
	// get detail normal, combine.
    float3 N2 = normalSwizzle(t_detail_normal.Sample(MMMLWWWSampler, input.TexCoord.xy * 1.0).rgb * 2.0f - 1.0f);
    N = float3(N.x + (N2.x * 1.0), N.y + (N2.y * 1.0), N.z);

	//xform normal
    float3 nN = normalize(mul(normalize(N), basis));
    float3 env = get_environment_colour(reflect(-input.I, nN), 0.0);

    OUT.Colour = float4(_gamma(env.rgb), 1.0f);
    return OUT;
}

PIXELDATA ps30_t_ambient(in VS_OUTPUT input) : COLOR
{
    PIXELDATA OUT;
	// get normal and normal map.
    const float3x3 basis = MAXTBN
    float3 N = normalSwizzle(t_normal.Sample(MMMLWWWSampler, input.TexCoord.xy).rgb * 2.0f - 1.0f);
	// get detail normal, combine.
    float3 N2 = normalSwizzle(t_detail_normal.Sample(MMMLWWWSampler, input.TexCoord.xy * 1.0).rgb * 2.0f - 1.0f);
    N = float3(N.x + (N2.x * 1.0), N.y + (N2.y * 1.0), N.z);

	//xform normal
    float3 nN = normalize(mul(normalize(N), basis));
    float3 env = cube_ambient(reflect(-input.I, nN)) * 0.2f;

    OUT.Colour = float4(_gamma(env.rgb), 1.0f);
    return OUT;
}

PIXELDATA ps30_t_ws_normal_map(in VS_OUTPUT input) : COLOR
{
    PIXELDATA OUT;
	// get normal and normal map.
    const float3x3 basis = MAXTBN
    float3 N = normalSwizzle(t_normal.Sample(MMMLWWWSampler, input.TexCoord.xy).rgb * 2.0f - 1.0f);

	// get detail normal, combine.
    if (1.0)
    {
        float3 N2 = normalSwizzle(t_detail_normal.Sample(MMMLWWWSampler, input.TexCoord.xy * 1.0).rgb * 2.0f - 1.0f);
        N = float3(N.x + (N2.x * 1.0), N.y + (N2.y * 1.0), N.z);
    }

	//xform normal
    float3 nN = ((normalize(mul(normalize(N), basis))) * 0.5) + 0.5;
    OUT.Colour = float4(nN.rgb, 1.0f);
    return OUT;
}

PIXELDATA ps30_t_dirtmap(in VS_OUTPUT input) : COLOR
{
    PIXELDATA OUT;
	//get dirtmap pixel
    float2 dirt_scale = float2(f_uv2_tile_interval_u, f_uv2_tile_interval_v);
    float4 dirtmap_p = t_dirtmap_uv2.Sample(MMMLWWWSampler, input.TexCoord.xy * dirt_scale);
    float4 dirtmap_alpha_p = t_alpha_mask.Sample(MMMLWWWSampler, input.TexCoord.zw);

    OUT.Colour = float4(dirtmap_p.rgb, 1.0f);
    return OUT;
}

PIXELDATA ps30_t_alpha_uv2(in VS_OUTPUT input) : COLOR
{
    PIXELDATA OUT;
	//get dirtmap pixel
    float4 dirtmap_alpha_p = t_alpha_mask.Sample(MMMLWWWSampler, input.TexCoord.zw);

    OUT.Colour = float4(dirtmap_alpha_p.rgb, 1.0f);
    return OUT;
}

PIXELDATA ps30_valpha(in VS_OUTPUT input) : COLOR
{
    PIXELDATA OUT;
	//get albedo pixel
    float4 Ct = t_base_colour.Sample(MMMLWWWSampler, input.TexCoord.xy);
    OUT.Colour = float4(input.Color.aaa, Ct.a);
    return OUT;
}

PIXELDATA ps30_vcolour(in VS_OUTPUT input) : COLOR
{
    PIXELDATA OUT;
	//get albedo pixel
    float4 Ct = t_base_colour.Sample(MMMLWWWSampler, input.TexCoord.xy);

    OUT.Colour = float4(input.Color.rgb, Ct.a);
    return OUT;
}

PIXELDATA ps30_main_BACKGROUND_CUBE(in VS_OUTPUT input) : COLOR
{
    PIXELDATA OUT;
	//	Look up the environment map texture...
    float3 hdr_linear_col = get_environment_colour(normalize(input.Wpos), 0);

    //  Tone-map the pixel...
    float3 ldr_linear_col = saturate(tone_map_linear_hdr_to_linear_ldr(hdr_linear_col, Tone_Map_Black, Tone_Map_White));

    //  Return gamma corrected value...
    OUT.Colour = float4(_gamma(ldr_linear_col), 1.0f);
    return OUT;
}

PIXELDATA ps30_ndotl(in VS_OUTPUT input) : COLOR
{
    PIXELDATA OUT;
    float4 diffuse_colour = t_base_colour.Sample(MMMLWWWSampler, input.TexCoord.xy);
    float3 light_vector = lightDir;

	//  Get the pixel normal in world-space ensuring that it's in 3dsmax's coordinate system...
    float3x3 basis = MAXTBN
    float4 Np = t_normal.Sample(MMMLWWWSampler, input.TexCoord.xy);
    float3 N = normalSwizzle_UPDATED((Np.rgb * 2.0f) - 1.0f);
    float3 pixel_normal = normalize(mul(normalize(N), basis));

    float3 ndotl = saturate(dot(pixel_normal, light_vector));

    //  Return gamma corrected value...
    OUT.Colour = float4(_gamma(ndotl), diffuse_colour.a);
    return OUT;
}
#endif

// #if 0
//////////////////////////////////////////////////////////////////////////////////////////////////////
////	Techniques
//////////////////////////////////////////////////////////////////////////////////////////////////////

//technique11 Main_11 <
//	bool overridesDrawState = false;
//	int isTransparent = 1;
//	string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//		string Script = "Draw=geometry;";
//		string drawContext = "colorPass";
//	>
//    {
//        SetVertexShader(CompileShader(vs_4_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_4_0, ps30_main()));
//    }
//}

//technique11 Channel_Colour11 <
//	bool overridesDrawState = false;
//	int isTransparent = 1;
//	string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//		string Script = "Draw=geometry;";
//		string drawContext = "colorPass";
//	>
//    {
//        SetVertexShader(CompileShader(vs_4_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_4_0, ps30_flatdiffuse()));
//    }
//}

//technique11 Channel_Diffuse11 <
//	bool overridesDrawState = false;
//	int isTransparent = 1;
//	string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//		string Script = "Draw=geometry;";
//		string drawContext = "colorPass";
//	>
//    {
//        SetVertexShader(CompileShader(vs_4_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_4_0, ps30_albedo()));
//    }
//}

//technique11 Channel_Roughness11 <
//	bool overridesDrawState = false;
//	int isTransparent = 1;
//	string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//		string Script = "Draw=geometry;";
//		string drawContext = "colorPass";
//	>
//    {
//        SetVertexShader(CompileShader(vs_4_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_4_0, ps30_t_roughness()));
//    }
//}

//technique11 Channel_Specular11 <
//	bool overridesDrawState = false;
//	int isTransparent = 1;
//	string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//		string Script = "Draw=geometry;";
//		string drawContext = "colorPass";
//	>
//    {
//        SetVertexShader(CompileShader(vs_4_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_4_0, ps30_t_specular()));
//    }
//}

//technique11 Channel_Normal11 <
//	bool overridesDrawState = false;
//	int isTransparent = 1;
//	string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//		string Script = "Draw=geometry;";
//		string drawContext = "colorPass";
//	>
//    {
//        SetVertexShader(CompileShader(vs_4_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_4_0, ps30_t_normal()));
//    }
//}

//technique11 Channel_SolidAlpha11 <
//	bool overridesDrawState = false;
//	int isTransparent = 1;
//	string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//		string Script = "Draw=geometry;";
//		string drawContext = "colorPass";
//	>
//    {
//        SetVertexShader(CompileShader(vs_4_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_4_0, ps30_solidalpha()));
//    }
//}

//technique11 Channel_Ambient11 <
//	bool overridesDrawState = false;
//	int isTransparent = 1;
//	string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//		string Script = "Draw=geometry;";
//		string drawContext = "colorPass";
//	>
//    {
//        SetVertexShader(CompileShader(vs_4_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_4_0, ps30_t_ambient()));
//    }
//}

//technique11 Channel_Ao <
//	bool overridesDrawState = false;
//	int isTransparent = 1;
//	string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//		string Script = "Draw=geometry;";
//		string drawContext = "colorPass";
//	>
//    {
//        SetVertexShader(CompileShader(vs_4_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_4_0, ps30_t_ao()));
//    }
//}

//technique11 Channel_Mask111 <
//	bool overridesDrawState = false;
//	int isTransparent = 1;
//	string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//		string Script = "Draw=geometry;";
//		string drawContext = "colorPass";
//	>
//    {
//        SetVertexShader(CompileShader(vs_4_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_4_0, ps30_t_mask1()));
//    }
//}

//technique11 Channel_Mask211 <
//	bool overridesDrawState = false;
//	int isTransparent = 1;
//	string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//		string Script = "Draw=geometry;";
//		string drawContext = "colorPass";
//	>
//    {
//        SetVertexShader(CompileShader(vs_4_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_4_0, ps30_t_mask2()));
//    }
//}

//technique11 Channel_Mask311 <
//	bool overridesDrawState = false;
//	int isTransparent = 1;
//	string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//		string Script = "Draw=geometry;";
//		string drawContext = "colorPass";
//	>
//    {
//        SetVertexShader(CompileShader(vs_4_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_4_0, ps30_t_mask3()));
//    }
//}

//technique11 Channel_Cubemap11 <
//	bool overridesDrawState = false;
//	int isTransparent = 1;
//	string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//		string Script = "Draw=geometry;";
//		string drawContext = "colorPass";
//	>
//    {
//        SetVertexShader(CompileShader(vs_4_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_4_0, ps30_t_cubemap()));
//    }
//}

//technique11 Channel_WSNormal11 <
//	bool overridesDrawState = false;
//	int isTransparent = 1;
//	string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//		string Script = "Draw=geometry;";
//		string drawContext = "colorPass";
//	>
//    {
//        SetVertexShader(CompileShader(vs_4_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_4_0, ps30_t_ws_normal_map()));
//    }
//}

//technique11 Channel_Dirtmap11 <
//	bool overridesDrawState = false;
//	int isTransparent = 1;
//	string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//		string Script = "Draw=geometry;";
//		string drawContext = "colorPass";
//	>
//    {
//        SetVertexShader(CompileShader(vs_4_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_4_0, ps30_t_dirtmap()));
//    }
//}

//technique11 Channel_Alpha_UV211 <
//	bool overridesDrawState = false;
//	int isTransparent = 1;
//	string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//		string Script = "Draw=geometry;";
//		string drawContext = "colorPass";
//	>
//    {
//        SetVertexShader(CompileShader(vs_4_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_4_0, ps30_t_alpha_uv2()));
//    }
//}

//technique11 Channel_Vertex_alpha11 <
//	bool overridesDrawState = false;
//	int isTransparent = 1;
//	string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//		string Script = "Draw=geometry;";
//		string drawContext = "colorPass";
//	>
//    {
//        SetVertexShader(CompileShader(vs_4_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_4_0, ps30_valpha()));
//    }
//}

//technique11 Channel_Vertex_colour11 <
//	bool overridesDrawState = false;
//	int isTransparent = 1;
//	string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//		string Script = "Draw=geometry;";
//		string drawContext = "colorPass";
//	>
//    {
//        SetVertexShader(CompileShader(vs_4_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_4_0, ps30_vcolour()));
//    }
//}

//technique11 Channel_ndotl11 <
//	bool overridesDrawState = false;
//	int isTransparent = 1;
//	string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//		string Script = "Draw=geometry;";
//		string drawContext = "colorPass";
//	>
//    {
//        SetVertexShader(CompileShader(vs_4_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_4_0, ps30_ndotl()));
//    }
//}

//technique11 Full_standard_BACKGROUND_HDR_CUBE <
//	bool overridesDrawState = false;
//	int isTransparent = 1;
//	string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//		string Script = "Draw=geometry;";
//		string drawContext = "colorPass";
//	>
//    {
//        SetVertexShader(CompileShader(vs_4_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_4_0, ps30_main_BACKGROUND_CUBE()));
//    }
//}

#endif