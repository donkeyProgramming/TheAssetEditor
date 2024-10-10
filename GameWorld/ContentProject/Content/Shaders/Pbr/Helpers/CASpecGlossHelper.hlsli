#ifndef _CA_SPEC_GLOS_HELPER_
#define _CA_SPEC_GLOS_HELPER_

#include "../Shared/const_layout.hlsli"
#include "../inputlayouts.hlsli"
#include "../TextureSamplers.hlsli"
#include "../helpers/mathfunctions.hlsli"
#include "../helpers/mathconstants.hlsli"

//string ParamID = "0x003"; //use dxsas compiler in 3dsmax

//// ----------------------------------------- Header ------------------------------------------
//#ifndef SFX_HLSL_5
//#define SFX_HLSL_5
//#endif
//#ifndef _3DSMAX_
//#define _3DSMAX_
//#endif

//#define MAXTBN float3x3 ( normalize ( input.Tgt ) , normalize ( input.Nml ) , normalize ( -input.Btgt ) );

//////////////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////                                              ///////////////////////////
/////////////////////////////    Parameters                                ///////////////////////////
/////////////////////////////                                              ///////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////////////

//float3 getPixelNormal(PixelInputType input)
//{
//    //float3x3 basis = float3x3(normalize(input.tangent), normalize(input.normal), normalize(input.binormal));
//    float3x3 basis = float3x3(normalize(input.tangent.xyz), normalize(input.binormal.xyz), normalize(input.normal.xyz)); // works in own shader

//    float4 NormalTex = shaderTextures[t_Normal].Sample(s_anisotropic, input.tex1);

//    float3 Np = 0;
//    Np.x = NormalTex.r * NormalTex.a;
//    Np.y = NormalTex.g;
//    Np = (Np * 2.0f) - 1.0f;

//    Np.z = sqrt(1 - Np.x * Np.x - Np.y * Np.y);

//    return normalize(mul(Np.xyz, basis));
//}

//float getSSAO(in float2 tex)
//{
//    return ao_mapTexture.SampleLevel(SamplerLinear, tex, 0).x;
//}

//float getSSAO(PixelInputType input)
//{
//    //float2 screenUV = (input.screenPos.xy / input.screenPos.w) * 0.5f + 0.5f;
//    //return ao_mapTexture.SampleLevel(SamplerLinear, screenUV, 0);

//    float2 screenUV;
//    screenUV.x = 0.5f + (input.screenPos.x / input.screenPos.w * 0.5f);
//    screenUV.y = 0.5f - (input.screenPos.y / input.screenPos.w * 0.5f);

//    //float2 screenUV = (input.screenPos.xy / input.screenPos.w) * 0.5f + 0.5f;
//    return ao_mapTexture.SampleLevel(SamplerLinear, screenUV, 0).x;
//}

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

/////////////////////////
// parameters
/////////////////////////
//static const float pi = 3.14159265;
//static const float one_over_pi = 1 / 3.14159265;
//static const float real_approx_zero = 0.001f;

//static const float texture_alpha_ref = 0.5f; //	This is a different value to that in game.  The game's value is in RigidUtil.hlsl.  This value might be fine though in max.  Speak to Chris W for more info.

//	Shadow stuff.
// #define light1Type 2
// #define light1attenType 0
// #define light1coneType 0
// #define light1CastShadows true

// #include <shadowMap.fxh>
// SHADOW_FUNCTOR(shadowTerm1);

//float4 light_position0 : POSITION <
//    string Object = "OmniLight";
//    string UIName =  "Light Position";
//    string Space = "World";
//	int refID = 0;
//> = { -0.5f, 2.0f, 1.25f, 1.0f };

//float4 light_color0 : LIGHTCOLOR
//<
//	int LightRef = 0;
//	int refID = 0;
//	string UIWidget = "None";
//> = float4(1.0f, 1.0f, 1.0f, 1.0f);

//float4 vec4_colour_0
//<
//	string type		= "Color";
//	string UIName	= "tint color 1";
//	string UIWidget = "Color";
//> = { 0.5, 0.1, 0.1, 1.0 };

//float4 vec4_colour_1
//<
//	string type		= "Color";
//	string UIName	= "tint color 2";
//	string UIWidget = "Color";
//> = { 0.3, 0.6, 0.5, 1.0 };

//float4 vec4_colour_2
//<
//	string type		= "Color";
//	string UIName	= "tint color 3";
//	string UIWidget = "Color";
//> = { 0.5, 0.2, 0.1, 1.0 };

////	Tone mapping values...
static const float Tone_Map_Black = 0.001;
static const float Tone_Map_White = 6.8f;
static const float low_tones_scurve_bias = 0.33f;
static const float high_tones_scurve_bias = 0.66f;
//static const float texture_alpha_ref = 0.5f;


////	Misc...
//const float env_lod_pow = 1.8f;

//float f_mass
//<
//  float UIMin     = 0.01;
//  float UIMax     = 1000.0;
//  float UIStep    = 0.1;
//  string UIName   = "Mass";
//> = 1.0;

//float f_flex
//<
//  float UIMin     = 0.01;
//  float UIMax     = 1.0;
//  float UIStep    = 0.1;
//  string UIName   = "Mass";
//> = 1.0;

//#define TEXTURE_FILTERING_MODE LINEAR

//// Orthogonalize tangent and bitangent on each pixel, otherwise use the interpolated values.
//// This parameter is controlled by 3ds Max according to the "Tangents and Bitangents" preference.
//bool orthogonalizeTangentBitangentPerPixel
//<
//    string UIName = "Orthogonalize per Pixel (Set by 3ds Max)";
//> = false;

//////////////////////////////////////////////////////////////////////////////////////////////////////
////	Textures
//////////////////////////////////////////////////////////////////////////////////////////////////////

//////////////////////////////////////////////////////////////////////////////////////////////////////
////	Textures - these are exported, the variable names are set by graphics team, change only with their
////  approval/consultation.
//////////////////////////////////////////////////////////////////////////////////////////////////////

//bool b_alpha_off
//<
//    string gui = "slider";
//    string UIName = "Alpha off";
//> = true;

//bool b_shadows
//<
//    string gui = "slider";
//    string UIName = "shadows";
//> = true;

////
////  SAMPLERS
////

//SamplerState MMMLWWWSampler
//{
//    Filter = MIN_MAG_MIP_LINEAR;
//    AddressU = WRAP;
//    AddressV = WRAP;
//    AddressW = WRAP;
//};

//Texture2D<float4> t_albedo
//<
//	string UIName = "diffuse";
//	string name = "test_gray.tga";
//>;

//Texture2D<float4> t_normal
//<
//	string UIName = "Normal";
//	string name = "flatnormal.tga";
//>;

//Texture2D<float4> t_detail_normal
//<
//	string UIName = "Detail N";
//	string name = "flatnormal.tga";
//>;

//Texture2D<float4> t_smoothness
//<
//	string UIName = "Smoothness";
//	string name = "test_gray.tga";
//>;

//Texture2D<float4> t_specular_colour
//<
//	string UIName = "specular";
//	string name = "test_gray.tga";
//>;

//Texture2D<float4> t_mask1
//<
//	string UIName = "mask1";
//	string name = "test_black.tga";
//>;

//Texture2D<float4> t_mask2
//<
//	string UIName = "mask2";
//	string name = "test_black.tga";
//>;

//Texture2D<float4> t_mask3
//<
//	string UIName = "mask3";
//	string name = "test_black.tga";
//>;

//Texture2D<float4> t_ambient_occlusion_uv2
//<
//	string UIName = "ambient_occlusion";
//	string name = "test_black.tga";
//>;

//////////////////////////////////////////////////////////////////////////////////////////////////////
////	Textures -not exported, for max preview only
//////////////////////////////////////////////////////////////////////////////////////////////////////

//TextureCube t_hdr_environment_map
//<
//	string UIName = "HDR Reflection";
//	string name = "game_hdr_cubemap.dds";
//	string type = "CUBE";
//	string ResourceName = "game_hdr_cubemap.dds";
//	string ResourceType = "Cube";
//>;

//TextureCube t_hdr_ambient
//<
//	string UIName = "HDR AmbiTexture";
//	string name = "game_hdr_ambient_map.dds";
//	string type = "CUBE";
//	string ResourceName = "game_hdr_ambient_map.dds";
//	string ResourceType = "Cube";
//>;

//// CUBE Samplers

//SamplerState s_cubemap
//{
//    Filter = ANISOTROPIC;
//    AddressU = CLAMP;
//    AddressV = CLAMP;
//    AddressW = CLAMP;
//};

//float f_uv2_tile_interval_u
//<
//  float UIMin     = -100;
//  float UIMax     = 100;
//  float UIStep    = 0.1;
//  string UIName   = "Detail Tile U";
//> = 4.0;

//float f_uv2_tile_interval_v
//<
//  float UIMin     = -100;
//  float UIMax     = 100;
//  float UIStep    = 0.1;
//  string UIName   = "Detail Tile V";
//> = 4.0;

//float f_dirtmap_hardness
//<
//  float UIMin     = -100;
//  float UIMax     = 100;
//  float UIStep    = 0.1;
//  string UIName   = "Dirtmap hardness";
//> = 0.0;

//float f_shadow_strength
//<
//  float UIMin     = -100;
//  float UIMax     = 100;
//  float UIStep    = 0.1;
//  string UIName   = "Shadow strength";
//> = 0.75;

//int i_alpha_mode
//<
//  float UIMin     = 0;
//  float UIMax     = 2;
//  float UIStep    = 1;
//  string UIName   = "Alpha mode";
//> = 0;

//Texture2D<float4> t_dirtmap_uv2
//<
//	string UIName = "tiling_dirt_uv2";
//	string name = "test_black.tga";
//>;

//Texture2D<float4> t_alpha_mask
//<
//	string UIName = "dirt alpha mask";
//	string name = "test_black.tga";
//>;

//Texture2D<float4> t_decal_diffuse
//<
//	string UIName = "decal_diffuse";
//	string name = "test_gray.tga";
//>;

//Texture2D<float4> t_decal_normal
//<
//	string UIName = "decal_normal";
//	string name = "test_gray.tga";
//>;

//Texture2D<float4> t_decal_mask
//<
//	string UIName = "decal_mask";
//	string name = "test_white.tga";
//>;

//Texture2D<float4> t_decal_dirtmap
//<
//	string UIName = "decal_dirtmap";
//	string name = "test_gray.tga";
//>;

//Texture2D<float4> t_decal_dirtmask
//<
//	string UIName = "decal_dirtmask";
//	string name = "test_black.tga";
//>;

//Texture2D<float4> t_diffuse_damage
//<
//    string UIName = "diffuse_damage";
//    string name = "test_gray.tga";
//>;

//Texture2D<float4> t_anisotropy
//<
//    string UIName = "anisotropy";
//    string name = "test_gray.tga";
//>;

//bool b_faction_colouring
//<
//    string gui = "slider";
//    string UIName = "faction_colouring";
//> = true;

//int i_bone_influences
//<
//  float UIMin     = 1;
//  float UIMax     = 4;
//  float UIStep    = 1;
//  string UIName   = "bone influences";
//> = 2;

//int i_random_tile_u
//<
//  float UIMin     = 0;
//  float UIMax     = 1;
//  float UIStep    = 1;
//  string UIName   = "Dirt tile U";
//> = 1;

//int i_random_tile_v
//<
//  float UIMin     = 0;
//  float UIMax     = 1;
//  float UIStep    = 1;
//  string UIName   = "Dirt tile V";
//> = 1;

//float4 vec4_uv_rect
//<
//	string type		= "color";
//	string UIName	= "Decal uv rect";
//	string UIWidget = "Color";
//> = { 0, 0, 1, 1 };

//float f_uv_offset_u
//<
//  float UIMin     = -500;
//  float UIMax     = 500;
//  float UIStep    = 0.01;
//  string UIName   = "dirt_uv_offset_u";
//> = 0.5;

//float f_uv_offset_v
//<
//  float UIMin     = -500;
//  float UIMax     = 500;
//  float UIStep    = 0.01;
//  string UIName   = "dirt_uv_offset_v";
//> = 0.5;

//bool b_do_dirt
//<
//    string gui = "slider";
//    string UIName = "dirt";
//> = false;

//bool b_do_decal
//<
//    string gui = "slider";
//    string UIName = "decal";
//> = false;

//bool b_light1_omni
//<
//    string gui = "slider";
//    string UIName = "light1_omni";
//> = false;

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

////////////////////////////////////////////////////////////////////////////////////////////////////
//	Colorimetry Functions
////////////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////
//  From http://en.wikipedia.org/wiki/SRGB                          //
//                                                                  //
//  It seems that gamma to linear and back again is not a simple    //
//  pow function.  The functions above are just a simplification    //
//  of what the spec is and what the hardware is doing, which is    //
//  following this spec.                                            //
//////////////////////////////////////////////////////////////////////
//S    return linear_accurate(vGamma);
//}

//float3 _gamma(in float3 vLinear)
//{
//    return gamma_accurate(vLinear);
//}

//float _gamma(in float fLinear)
//{
//    return gamma_accurate_component(fLinear);
//}

float get_diffuse_scale_factor()
{
    return 0.004 * 5.0f / 2.0f; //	Chosen to match marmoset
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
//this should match the order of the basis
    //return float3(-ref.x, ref.z, -ref.y);
    return float3(ref.x, ref.y, ref.z); // USER: phazer
}

float3 normalSwizzle(in float3 ref)
{
    //return float3(ref.y, ref.x, ref.z);

    return float3(ref.x, ref.y, ref.z); // USER: phazer
}

float3 normalSwizzle_UPDATED(in float3 ref)
{
    //eturn float3(ref.x, ref.z, ref.y);

    return float3(ref.x, ref.y, ref.z); // USER: phazer
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

float3 tone_map_linear_hdr_pixel_value(in float3 linear_hdr_pixel_val);
float4 HDR_RGB_To_HDR_CIE_Log_Y_xy(in float3 linear_colour_val);
float4 tone_map_HDR_CIE_Log_Y_xy_To_LDR_CIE_Yxy(in float4 hdr_LogYxy);
float4 LDR_CIE_Yxy_To_Linear_LDR_RGB(in float4 ldr_cie_Yxy);
float get_scurve_y_pos(const float x_coord);

//old///////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////
//	Lighting Functions
////////////////////////////////////////////////////////////////////////////////////////////////////

float3 get_environment_colour(in float3 direction, in float lod)
{
    const float specularCubeMapBrightness = 2.0f;
    
    return tex_cube_specular.SampleLevel(SampleType, (texcoordEnvSwizzle(direction)), lod).rgb * specularCubeMapBrightness * LightMult;
}

//	Ambient diffuse
float3 cube_ambient(in float3 N)
{
    const float diffuseCubeMapBrightness = 2.0f;
    
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

//void ps_common_blend_decal(in float4 colour, in float3 normal, in float3 specular, out float4 ocolour, out float3 onormal, out float3 ospecular, in float2 uv, in float decal_index, in float4 uv_rect_coords, in float valpha)
//{
//    float2 decal_top_left = uv_rect_coords.xy;
//    float2 decal_dimensions = uv_rect_coords.zw - uv_rect_coords.xy;

//	// Find corresponding decal uv sample coords
//    float2 decal_uv = (uv - decal_top_left) / decal_dimensions;

//    float4 decal_diffuse;
//    float3 decal_normal;

//    decal_diffuse = t_decal_diffuse.Sample(MMMLWWWSampler, decal_uv).rgba;
//    decal_normal = normalSwizzle_UPDATED((t_decal_normal.Sample(MMMLWWWSampler, decal_uv).xyz * 2.0f) - 1.0f);
//    float decal_mask = t_decal_mask.Sample(MMMLWWWSampler, uv).a;

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

//void ps_common_blend_decal_SKIN(in float4 colour, in float3 normal, in float3 specular, out float4 ocolour, out float3 onormal, out float3 ospecular, in float2 uv, in float decal_index, in float4 uv_rect_coords, in float valpha)
//{
//    float2 decal_top_left = uv_rect_coords.xy;
//    float2 decal_dimensions = uv_rect_coords.zw - uv_rect_coords.xy;

//	// Find corresponding decal uv sample coords
//    float2 decal_uv = (uv - decal_top_left) / decal_dimensions;

//    float4 decal_diffuse;
//    float3 decal_normal;

//    decal_diffuse = t_decal_diffuse.Sample(MMMLWWWSampler, decal_uv).rgba;
//    decal_normal = normalSwizzle_UPDATED((t_decal_normal.Sample(MMMLWWWSampler, decal_uv).xyz * 2.0f) - 1.0f);
//    float decal_mask = t_decal_mask.Sample(MMMLWWWSampler, uv).a;

//    float decalblend = decal_mask * decal_diffuse.a * valpha;
//    ocolour = float4(1, 1, 1, 1);
//    onormal = float4(0, 0, 1, 1);
//    ospecular = lerp(specular, decal_diffuse.rgb, decalblend);
//	// Blend diffuse
//    ocolour.rgb = lerp(colour.rgb, decal_diffuse.rgb, decalblend);
//	//ocolour = float4(0.2,0.2,0.2,1);
//	// Blend normal
//    onormal.xyz = lerp(onormal.xyz, decal_normal.rgb, decalblend);
//    onormal.xyz = float3(normal.xy + onormal.xy, normal.z);
//}

//void ps_common_blend_dirtmap(in out float4 colour, in out float3 normal, in float3 specular, out float4 ocolour, out float3 onormal, out float3 ospecular, in float2 uv, in float2 uv_offset)
//{
//	//offset masking:
//    uv_offset = uv_offset * float2(i_random_tile_u, i_random_tile_v);
//	// Compute Dirt, first get dirt mask that's associated with the rigid
//    float mask_alpha = t_decal_dirtmask.Sample(MMMLWWWSampler, uv).a;
//    float4 dirtmap = t_decal_dirtmap.Sample(MMMLWWWSampler, ((uv) + uv_offset) * float2(f_uv2_tile_interval_u, f_uv2_tile_interval_v)).xyzw;

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

//void ps_common_blend_dirtmap_SKIN(in out float4 colour, in out float3 normal, in float3 specular, out float4 ocolour, out float3 onormal, out float3 ospecular, in float2 uv, in float2 uv_offset)
//{
//	//offset masking:
//    uv_offset = uv_offset * float2(i_random_tile_u, i_random_tile_v);
//	// Compute Dirt, first get dirt mask that's associated with the rigid
//    float mask_alpha = t_decal_dirtmask.Sample(MMMLWWWSampler, uv).a;
//    float4 dirtmap = t_decal_dirtmap.Sample(MMMLWWWSampler, ((uv) + uv_offset) * float2(f_uv2_tile_interval_u, f_uv2_tile_interval_v)).xyzw;

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

//void ps_common_blend_vfx(in out float4 colour, in out float3 normal, in float3 specular, out float4 ocolour, out float3 onormal, out float3 ospecular, in float2 uv, in float2 uv_offset)
//{
//	//offset masking:
//    uv_offset = uv_offset * float2(i_random_tile_u, i_random_tile_v);
//	// Compute Dirt, first get dirt mask that's associated with the rigid
////	float mask_alpha = t_decal_dirtmask.Sample(MMMLWWWSampler, uv).a;
//    float4 dirtmap = t_decal_dirtmap.Sample(MMMLWWWSampler, ((uv) + uv_offset) * float2(f_uv2_tile_interval_u, f_uv2_tile_interval_v)).xyzw;
//	//float4 dirtmap = t_decal_dirtmap.Sample(MMMLWWWSampler, (uv)).xyzw;

//    ocolour = float4(lerp(colour.rgb, dirtmap.rgb, dirtmap.a), 1);
//	//ocolour = colour;
//    onormal = normal;
//    ospecular = specular;
//}

//void ps_common_blend_vfx_SKIN(in out float4 colour, in out float3 normal, in float3 specular, in out float reflectivity, out float4 ocolour, out float3 onormal, out float3 ospecular, out float oreflectivity, in float2 uv, in float2 uv_offset)
//{
//	//offset masking:
//    uv_offset = uv_offset * float2(i_random_tile_u, i_random_tile_v);
//	// Compute Dirt, first get dirt mask that's associated with the rigid
////	float mask_alpha = t_decal_dirtmask.Sample(MMMLWWWSampler, uv).a;
//    float4 dirtmap = t_decal_dirtmap.Sample(MMMLWWWSampler, ((uv) + uv_offset) * float2(f_uv2_tile_interval_u, f_uv2_tile_interval_v)).xyzw;
//	//float4 dirtmap = t_decal_dirtmap.Sample(MMMLWWWSampler, (uv)).xyzw;

//    ocolour = float4(lerp(colour.rgb, dirtmap.rgb, dirtmap.a), 1);
//	//ocolour = colour;
//    onormal = normal;
//    ospecular = specular;
//    oreflectivity = reflectivity;
//}

//////////////////////////////////////////////////////////////////////////////////////////////////////
////	SKIN_LIGHTING_MODEL	-
//////////////////////////////////////////////////////////////////////////////////////////////////////

//float g_hdr_on = 1.0f;

//float get_skin_dlight_specular_scaler()
//{
//    return g_hdr_on > 0.0 ? 2.0f : 2.0f;
//}

//float get_skin_dlight_rim_scaler()
//{
//    return g_hdr_on > 0.0 ? 1.0f : 0.65f;
//}

//float get_skin_dlight_diffuse_scaler()
//{
//    return g_hdr_on > 0.0 ? 3.0f : 0.9f;
//}

//struct SkinLightingModelMaterial
//{
//	// Per-Pixel Params
//    float RimMask; // 0..1
//    float SubSurfaceStrength; // 0..1
//    float BackScatterStrength; // 0..1
//    float Gloss; // 0..1 Gloss
//    float3 specular_colour;
//    float3 Color; // 0..1
//    float3 Normal; // 0..1
//	// Lighting Params
//    float Depth;
//    float Shadow; // 0..1
//    float SSAO; // 0..1
//};

//SkinLightingModelMaterial create_skin_lighting_material(in float _gloss, in float3 _SkinMap, in float3 _Color, in float3 _specular_colour, in float3 _Normal, in float4 _worldposition)
//{
//    SkinLightingModelMaterial material;
//    material.Gloss = _gloss;
//    material.RimMask = _SkinMap.x;
//    material.SubSurfaceStrength = _SkinMap.y;
//    material.BackScatterStrength = _SkinMap.z;
//    material.Color = _Color;
//    material.specular_colour = _specular_colour;
//    material.Normal = normalize(_Normal);
//    material.Depth = 1.0;
//    material.Shadow = 1.0f;
//#ifdef _MAX_
////		material.Shadow = shadowTerm1(float4(_worldposition.xyz,1));
//#endif
//    material.SSAO = 1.0;

//    return material;
//}

//float3 skin_shading(in float3 L, in float3 N, in float3 V, in float sss_strength, in float3 colour1, in float3 colour2)
//{
//    float ndotl = dot(N, -L);

//    float3 diff1 = ndotl * saturate(((ndotl * 0.8) + 0.3) / 1.44);
//    float3 diff2 = colour1 * (saturate(((ndotl * 0.9) + 0.5) / 1.44)) * saturate(1 - ((diff1 + 0.3)));
//    float3 diff3 = colour2 * (saturate(((ndotl * 0.3) + 0.3) / 2.25)) * (1 - diff1) * (1 - diff2);

//    float3 mix = (diff1 + diff2 + diff3);

//    float3 blendedDiff = lerp(float3(ndotl, ndotl, ndotl), (mix), sss_strength);
//    return saturate(float3(blendedDiff));
//}

//float3 skin_lighting_model_directional_light(in float3 LightColor, in float3 normalised_light_dir, in float3 normalised_view_dir, in SkinLightingModelMaterial skinlm_material)
//{
//	//max cludges///////
//    LightColor *= 500.0f; //	The game translates 1 LDR unit of light into 500 HDR units

//    normalised_light_dir = -normalised_light_dir;

//    float3 diffuse_scale_factor = get_diffuse_scale_factor().xxx;

//    float normal_dot_light_dir = max(dot(skinlm_material.Normal, -normalised_light_dir), 0);

//    float3 dlight_diffuse = skinlm_material.Color.rgb * skin_shading(normalised_light_dir, skinlm_material.Normal, normalised_view_dir, skinlm_material.SubSurfaceStrength, float3(0.612066, 0.456263, 0.05), float3(0.32, 0.05, 0.006)) * LightColor * diffuse_scale_factor;
//    dlight_diffuse *= get_skin_dlight_diffuse_scaler();

//	//backscattering
//	// powered ndot l for backfacing with smooth falloff, modulated by vdotl to limit effect to 'looking through' objects, plus mask control.
//    float backscatter = pow(saturate(dot(skinlm_material.Normal, normalised_light_dir)), 2.0) * pow(saturate(dot(normalised_view_dir, -normalised_light_dir)), 4.0f) * skinlm_material.BackScatterStrength;

//	//it is separated from the sss to allow glowing fringes on hair/feathers or cloth etc. as well as fleshy/waxy effects.
//    float3 backscatter_colour = lerp((LightColor), (LightColor * float3(0.7, 0, 0)), skinlm_material.SubSurfaceStrength) * diffuse_scale_factor * backscatter * skinlm_material.Shadow;

//    dlight_diffuse += (skinlm_material.Color.rgb * backscatter_colour * get_skin_dlight_diffuse_scaler());

//    float3 env_light_diffuse = skinlm_material.Color.rgb * cube_ambient(skinlm_material.Normal).rgb;

//    float kspec = phong_specular(normalised_view_dir, skinlm_material.Normal, lerp(1, 128, (skinlm_material.Gloss * skinlm_material.Gloss)), normalised_light_dir);

//    float3 dlight_specular = skinlm_material.specular_colour * kspec * LightColor * diffuse_scale_factor * get_skin_dlight_specular_scaler();

//    float3 reflected_view_vec = reflect(-normalised_view_dir, skinlm_material.Normal);

//    float3 rim_env_colour = cube_ambient(reflected_view_vec).rgb;

//	// Rim light
//    float rimfresnel = saturate(1 - (dot(-normalised_view_dir, skinlm_material.Normal)));

//    float riml = saturate(pow((rimfresnel), 2)) * skinlm_material.RimMask * 1.5 * get_skin_dlight_rim_scaler(); //1.5 is a scalar in order that we can get a brighter rim by setting the texture to be white.  Previously the maximum brightness was quite dull.

//	// Mask rim by up vector
//    float upness = max(dot(normalize(skinlm_material.Normal + float3(0, 0.75, 0)), float3(0, 1, 0)), 0);

//    float3 env_light_specular = (riml * upness * rim_env_colour);

//	//  Shadow...
//    float shadow_attenuation = skinlm_material.Shadow;

//    return (skinlm_material.SSAO * (env_light_diffuse + env_light_specular)) + (shadow_attenuation * (dlight_specular + dlight_diffuse));
//}

////////////////////////////////////////////////////////////////////////////////////////////////////
//	SKIN_LIGHTING_MODEL	- END
////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////////////////////////
//	STANDARD_LIGHTING_MODEL
////////////////////////////////////////////////////////////////////////////////////////////////////

//	Taken straight from the game's lighting.hlsl.  Parameters that are not supported in Max are
//	commented out.
struct R2_4_StandardLightingModelMaterial
{
	//  Exogenous lighting parameters...
    float3 Diffuse_Colour; // 0..1
    float3 Specular_Colour; // 0..1
    float3 Normal; // -1..+1
    float Smoothness; // 0..1 (Rough to Smooth)

								// Endogenous lighting params...
    float Depth;
    float Shadow; // 0..1
    float SSAO; // 0..1

								//  Misc...
								//	uint    BitFlags;           //  0 - 255 only.  Ultimately stored into an 8-bit colour channel of a gbuffer RT.  Flags defined above.
								//    uint    PixelSelectionID;
};

R2_4_StandardLightingModelMaterial R2_4_create_standard_lighting_material
(
	in float3 Diffuse_Colour,
	in float3 Specular_Colour,
	in float3 Normal,
	in float Smoothness,
	in float4 _worldposition,
	in float shadow,
	in float ambient_occlusion
)
{
    R2_4_StandardLightingModelMaterial material;

    material.Diffuse_Colour = Diffuse_Colour;
    material.Specular_Colour = Specular_Colour;
    material.Normal = Normal;
    material.Smoothness = Smoothness;
    material.Depth = 1.0f;
    material.Shadow = shadow;
    material.SSAO = ambient_occlusion;

#ifdef _MAX_
	//		material.Shadow = shadowTerm1(float4(_worldposition.xyz,1));
#endif

    return material;
}

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
float determine_fraction_of_facets_at_reflection_angle(in float smoothness, in float light_vec_reflected_view_vec_angle)
{
    //  The sun's angular radius is important because it accounts for a small but
    //  significant divergence in the light rays that reach the surface of the earth...
    float sun_angular_radius = get_sun_angular_radius();

    //  We need these min and max values as we will be sampling from a probability function in which
    //  100% and 0% cannot be represented.  The roughest shiny material is as reflecting as a diffuse material...
    float max_fraction_of_facets = 0.9999f;
    float min_fraction_of_facets = get_diffuse_scale_factor();

    //  This is the fraction of the facets that we expect to be in the direction of the
    //  pixel normal within the sun's angular diameter...
    float fraction_of_facets = lerp(min_fraction_of_facets, max_fraction_of_facets, smoothness * smoothness);    

    //  The fraction of the facets that this represents from negative infinity
    //  to sun_angular_radius is thus...
    float fraction_of_facets_to_look_for = 0.5f + (fraction_of_facets * 0.5f);

    //  Now we need to find a normal distribution function that will satisfy this proportion
    //  of facets within the sun's angular distribution...
    float sigma = max(norminv_sigma(sun_angular_radius, fraction_of_facets_to_look_for), 0.0001f);

    //  Determine the proportion of the faces that will be seen by the viewer...
    float proportion_of_facets = norm_cdf(light_vec_reflected_view_vec_angle - sun_angular_radius, light_vec_reflected_view_vec_angle + sun_angular_radius, sigma);

    //  We're done!
    return proportion_of_facets;
}

//	As a material becomes more rough, it also becomes more like a diffuse material.
float determine_facet_visibility(in float roughness, in float3 normal_vec, in float3 light_vec)
{
    const float n_dot_l = saturate(dot(normal_vec, light_vec));
    const float towards_diffuse_surface = sin(roughness * pi * 0.5f); //	( 0 - 1 ) output...
    const float facet_visibility = lerp(1.0f, n_dot_l, towards_diffuse_surface);

    return facet_visibility;
}

//  Determines the reflectivity of a surface given light and view vectors.  At glancing angles all materials reflect fully...
//  Inspired by fresnel_optimized function.  Values chosen to match marmoset.
float3 determine_surface_reflectivity(in float3 material_reflectivity, in float roughness, in float3 light_vec, in float3 view_vec)
{
    float fresnel_curve = 10;

    // TODO: Fix/improve/Research this further
    // AE-Change: Changes from "-view_vec", as dot product was 1 when lit+viewed from the SAME direction = WRONG (+looked bad)
    float val1 = max(0, dot(light_vec, view_vec)); //  Is one when light vector and view vector are completely opposed...

    float val2 = pow(val1, fresnel_curve);

    float fresnel_bias = 0.5f;

    float roughness_bias = 0.98f; //	We always have a minimum amount of fresnel, even for the roughest of surfaces...

    float smoothness_val = pow(cos(roughness * roughness_bias * pi * 0.5f), fresnel_bias); //	As a surface gets rougher, it's fresnel potential drops too...

    return lerp(material_reflectivity, saturate(60.0f * material_reflectivity), val2 * smoothness_val);
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

//  Common functionality pulled out to facilitate minor optimisations.
float3 get_reflectivity_base(in float3 light_vec, in float3 normal_vec, in float3 view_vec, in float3 material_reflectivity, in float smoothness, in float roughness, in float light_vec_reflected_view_vec_angle)
{
    float n_dot_l = dot(light_vec, normal_vec);

	//	If the fragment is facing away from the light source then there is nothing further to do...
    if (n_dot_l <= 0.0f)
        return float3(0, 0, 0);

    float fraction_of_facets = determine_fraction_of_facets_at_reflection_angle(smoothness, light_vec_reflected_view_vec_angle);

    float facet_visibility = determine_facet_visibility(roughness, normal_vec, light_vec); // Looks ok

    float3 surface_reflectivity = determine_surface_reflectivity(material_reflectivity, roughness, light_vec, view_vec);    

    return fraction_of_facets * facet_visibility * surface_reflectivity;
}

//  Dx10/11 requires trig values to be in the range -1 to +1.  Values outside of this range in dx10/11 result in undefined returned
//  values from acos for example.
float ensure_correct_trig_value(in float value)
{
    return clamp(value, -1.0f, +1.0f);
}

//  Determines the reflectivity of a surface, for given light, normal, and view vectors, along with a material's standard reflectivity and smoothness.  The reflectivity
//  at red, green, and blue wavelengths can be different, which can result in colour shifts.
float3 get_reflectivity_dir_light(in float3 light_vec, in float3 normal_vec, in float3 view_vec, in float3 reflected_view_vec, in float3 material_reflectivity, in float smoothness, in float roughness)
{
    float light_vec_reflected_view_vec_angle = acos(ensure_correct_trig_value(dot(light_vec, reflected_view_vec)));

    return get_reflectivity_base(light_vec, normal_vec, view_vec, material_reflectivity, smoothness, roughness, light_vec_reflected_view_vec_angle);
}

float3 get_reflectivity_env_light(in float3 light_vec, in float3 normal_vec, in float3 view_vec, in float3 material_reflectivity, in float smoothness, in float roughness)
{
    return determine_surface_reflectivity(material_reflectivity, roughness, light_vec, view_vec);
}

//  Determines the reflectivity of a surface given light and view vectors.  At glancing angles all materials reflect fully...
//  Inspired by fresnel_optimized function.  Values chosen to match marmoset.
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
    float fresnel_curve = 10;
        
    // TODO: Fix/improve/Research this further
    // AE-Change: Changes from "-view_vec", as dot product was 1 when lit+viewed from the SAME direction = WRONG (+looked bad)
    float val1 = max(0, dot(light_vec, view_vec)); //  Is one when light vector and view vector are completely opposed... 

    float val2 = pow(val1, fresnel_curve);

    float fresnel_bias = 0.5f;

    float roughness_bias = 0.98f; //	We always have a minimum amount of fresnel, even for the roughest of surfaces...

    float smoothness_val = pow(cos(roughness * roughness_bias * pi * 0.5f), fresnel_bias); //	As a surface gets rougher, it's fresnel potential drops too...

    return lerp(material_reflectivity, saturate(60.0f * material_reflectivity), val2 * smoothness_val);
#endif
}

float3 get_reflectivity_env_light(in float3 light_vec, in float3 normal_vec, in float3 view_vec, in float3 material_reflectivity, in float smoothness)
{
    return determine_surface_reflectivity(material_reflectivity, 1.0f - smoothness, light_vec, view_vec, normal_vec);
}

float3 get_reflectivity_env_light_material(in float3 light_vec, in float3 normal_vec, in float3 view_vec, in float roughness, in float3 specular_colour)
{
    float facet_visibility = determine_facet_visibility(roughness, normal_vec, view_vec);

	// saturate is added here to work around an nvidia bug: v_dot_n seemed to be
	// coming out as < 0, causing the val to be NaN.
    float v_dot_n = saturate(abs(dot(normal_vec, view_vec)));

    float fresnel_curve = 10;
    float val1 = 1 - v_dot_n;
    float val2 = pow(val1, fresnel_curve);

	// Masking based on smoothness, derived from Smith-GGX
    float alpha = roughness * roughness;
    float alpha2 = alpha * alpha;
    float masking = (2 * v_dot_n) / (v_dot_n + sqrt(alpha2 + (1 - alpha2) * v_dot_n * v_dot_n));

    masking = masking * masking;

    return lerp(specular_colour, saturate(60.0f * specular_colour), val2) * masking;
}

float3 get_reflectivity_env_light_material(in float3 light_vec, in float3 normal_vec, in float3 view_vec, in R2_4_StandardLightingModelMaterial material)
{
    return get_reflectivity_env_light_material(light_vec, normal_vec, view_vec, 1.f - material.Smoothness, material.Specular_Colour);
}

float adjust_linear_smoothness(in const float linear_smoothness)
{
	//return get_cubic_spline_adjusted_value(linear_smoothness, curve_y1_ctrl_pnt_env_smoothness, curve_y2_ctrl_pnt_env_smoothness, curve_y3_ctrl_pnt_env_smoothness);
    return linear_smoothness * linear_smoothness;
    //PHAZER//return _gamma(smoothness_curve);
}

float get_cube_env_scale_factor()
{
    return 1.0f;
}

float3 sample_environment_specular(in float roughness_in, in float3 reflected_view_vec)
{
#if 1
    const float env_lod_pow = 1.2;  //1.8f;
    const float env_map_lod_smoothness = adjust_linear_smoothness(1 - roughness_in);
    const float roughness = 1.0f - pow(env_map_lod_smoothness, env_lod_pow);

    //	This must be the number of mip-maps in the environment map! EDIT: set slightly lower to "simulate" SSR.
    float texture_num_lods = 7.0f;
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

float3 standard_lighting_model_environment_light_SM4_private( /*in const float3 Pws,*/in const float3 normalised_view_dir, in const float3 reflected_view_vec, in const R2_4_StandardLightingModelMaterial material)
{
	//  Specular calculations for environmental light contribution...
    float3 ambient_colour = 0.0f.xxx;

    ambient_colour = cube_ambient(material.Normal);     
    
    float3 env_light_diffuse = ambient_colour * material.Diffuse_Colour * (1.0f - material.Specular_Colour);

	// We always apply cubemap specular reflections here because it gives an initial bounce for ssr
	// SSR then blend with a term correcting for it
	{
        float3 env_light_pixel_reflectivity = get_reflectivity_env_light_material(reflected_view_vec, material.Normal, normalised_view_dir, material);

		//  Specular calculations for environmental light contribution...
        const float roughness = 1 - material.Smoothness;
        float3 environment_colour = sample_environment_specular(roughness, reflected_view_vec);
        float3 env_light_specular_colour = environment_colour * env_light_pixel_reflectivity;

        return material.SSAO * env_light_diffuse + env_light_specular_colour;
    }
}

//  Common functionality pulled out to facilitate minor optimisations.
float3 get_reflectivity_base(in float3 light_vec, in float3 normal_vec, in float3 view_vec, in float3 material_reflectivity, in float smoothness, in float light_vec_reflected_view_vec_angle)
{
    float n_dot_l = dot(light_vec, normal_vec);

	//	If the fragment is facing away from the light source then there is nothing further to do...
    if (n_dot_l <= 0.0f)
        return float3(0, 0, 0);

    float fraction_of_facets = determine_fraction_of_facets_at_reflection_angle(smoothness, light_vec_reflected_view_vec_angle);    
    
    float facet_visibility = determine_facet_visibility(1.0f - smoothness, normal_vec, light_vec);
       
    float3 surface_reflectivity = determine_surface_reflectivity(material_reflectivity, 1.0f - smoothness, light_vec, view_vec, normal_vec);
    
    return fraction_of_facets * facet_visibility * surface_reflectivity;
}

//  Determines the reflectivity of a surface, for given light, normal, and view vectors, along with a material's standard reflectivity and smoothness.  The reflectivity
//  at red, green, and blue wavelengths can be different, which can result in colour shifts.
float3 get_reflectivity_dir_light(in float3 light_vec, in float3 normal_vec, in float3 view_vec, in float3 reflected_view_vec, in float3 material_reflectivity, in float smoothness)
{
    float light_vec_reflected_view_vec_angle = acos(ensure_correct_trig_value(dot(light_vec, reflected_view_vec)));

    return get_reflectivity_base(light_vec, normal_vec, view_vec, material_reflectivity, smoothness, light_vec_reflected_view_vec_angle);
}

float3 standard_lighting_model_directional_light_SM4_private(in const float3 LightColor, in const float3 normalised_light_dir, in const float3 normalised_view_dir, in const float3 reflected_view_vec, in R2_4_StandardLightingModelMaterial material)
{
#ifndef DEBUGGING_BIT_FLAGS

	//	To match marmoset...
   material.Smoothness = _linear(material.Smoothness);
   material.Smoothness = adjust_linear_smoothness(material.Smoothness);    
    
    float normal_dot_light_vec = max(0.0f, dot(material.Normal, normalised_light_dir));

	//  Specular calculations for directional light contribution...
    float3 dlight_pixel_reflectivity = get_reflectivity_dir_light(normalised_light_dir, material.Normal, normalised_view_dir, reflected_view_vec, material.Specular_Colour, material.Smoothness);
    float3 dlight_specular_colour = dlight_pixel_reflectivity * LightColor;
    float3 dlight_material_scattering = 1.0f - max(dlight_pixel_reflectivity, material.Specular_Colour); //  All photons not accounted for by reflectivity are accounted by scattering. From the energy difference between in-coming light and emitted light we could calculate the amount of energy turned into heat. This energy would not be enough to make a viewable difference at standard illumination levels.    																											  //  Diffuse contribution from directional light...
    
																											  //  Diffuse contribution from directional light...
    float3 dlight_diffuse = material.Diffuse_Colour * normal_dot_light_vec * LightColor * dlight_material_scattering / pi;

	//  Scale the diffuse components in order to simulate scattering from all directions...        
    dlight_diffuse *= get_diffuse_scale_factor().xxx;        
    
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
		backscattering = extinction * inscattering * light * get_diffuse_scale_factor() * material.Diffuse_Colour * material.SSAO;
	}
#endif

    return material.Shadow * (dlight_specular_colour + dlight_diffuse) + backscattering;

#else

	const float is_flag_set = material.BitFlags & IS_SECOND_CLASS_OBJECT;

	return is_flag_set.xxx * 10.0f;

#endif
}

float3 standard_lighting_model_directional_light(in float3 LightColor, in float3 normalised_light_dir, in float3 normalised_view_dir, in R2_4_StandardLightingModelMaterial material)
{
    float direct_light_scale = 500.0f* LightMult; //	The game is 500 units of HDR light for each 1 unit of LDR

											//  Cludges for max...
    LightColor *= direct_light_scale; //  Same as in game.
//    float3 diffuse_scale_factor = get_diffuse_scale_factor().xxx;

    float normal_dot_light_vec = max(0.0f, dot(material.Normal, normalised_light_dir));

    float3 reflected_view_vec = reflect(normalised_view_dir, material.Normal);
        
    const float3 env_light = standard_lighting_model_environment_light_SM4_private( /*Pws,*/normalised_view_dir, reflected_view_vec, material);
    const float3 dir_light = standard_lighting_model_directional_light_SM4_private(LightColor, normalised_light_dir, normalised_view_dir, reflected_view_vec, material);

    return env_light + dir_light;
}

////////////////////////////////////////////////////////////////////////////////////////////////////
//	Inputs/Outputs
////////////////////////////////////////////////////////////////////////////////////////////////////

struct APP_INPUT
{
    float4 Position : POSITION;
    float3 Normal : NORMAL;
    float3 Tangent : TANGENT;
    float3 Bitangent : BINORMAL;
    float4 TexCoord0 : TEXCOORD0;
    float4 TexCoord1 : TEXCOORD1;
    float3 Color : TEXCOORD4;
    float Alpha : TEXCOORD5;
};

struct VS_OUTPUT
{
    float4 Position : POSITION;
    float4 TexCoord : TEXCOORD0;
    float3 I : TEXCOORD1;
    float3 Tgt : TEXCOORD2;
    float3 Btgt : TEXCOORD3;
    float3 Nml : TEXCOORD4;
    float3 Wpos : TEXCOORD5;
    float4 Color : TEXCOORD6;
};

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

//VS_OUTPUT vs30_main(in APP_INPUT input)
//{
//    VS_OUTPUT output;

//    output.Position = mul(input.Position, wvpMatrix);
//    output.TexCoord.xy = input.TexCoord0.xy;
//    output.TexCoord.zw = input.TexCoord1.xy;

//    output.TexCoord.y += 1;
//    output.TexCoord.w += 1;

//	//output.I = normalize(mul(input.Position,wMatrix) - wvMatrixI[3].xyz );
//    //Out.eyeVec = ViewInv[3].xyz -  Out.worldSpacePos;
//    // ////////////////////////////////////////////////////////////////////////////
//    // this eye vector is correct for max, maybe not for anything else!
//    // ////////////////////////////////////////////////////////////////////////////

//    output.I = normalize(vMatrixI[3] - mul(input.Position, wMatrix));
//	//output.I = vMatrixI[3].xyz;
//    output.Tgt = mul(float4(input.Tangent.xyz, 0.0f), wMatrix).xyz;
//    output.Btgt = mul(float4(input.Bitangent.xyz, 0.0f), wMatrix).xyz;
//    output.Nml = mul(float4(input.Normal.xyz, 0.0f), wMatrix).xyz;
//	//output.Nml = float3(0,0,1);
//    output.Wpos = mul(input.Position, wMatrix);
//    output.Color.rgb = input.Color.rgb;
//    output.Color.a = input.Alpha.r;
//    return output;
//}

////////////////////////////////////////////////////////////////////////////////////////////////////
//	Pixel Shader
////////////////////////////////////////////////////////////////////////////////////////////////////

void alpha_test(in const float pixel_alpha)
{
    if (UseAlpha == true)
    {
        clip(pixel_alpha - texture_alpha_ref);
    }
}

//void apply_faction_colours(inout float3 diffuse_colour_rgb, in const float2 tex_coord, in const bool faction_colours)
//{
//    if (faction_colours)
//    {
//        float mask_p1 = shaderTextures[t_Mask].Sample(SamplerLinear, tex_coord).r;
//        float mask_p2 = shaderTextures[t_Mask].Sample(SamplerLinear, tex_coord).g;
//        float mask_p3 = shaderTextures[t_Mask].Sample(SamplerLinear, tex_coord).b;

//	    //faction colours
//        diffuse_colour_rgb = lerp(diffuse_colour_rgb, diffuse_colour_rgb * /*_linear*/(mesh_faction_color1.rgb), mask_p1);
//        diffuse_colour_rgb = lerp(diffuse_colour_rgb, diffuse_colour_rgb * /* _linear*/(mesh_faction_color2.rgb), mask_p2);
//        diffuse_colour_rgb = lerp(diffuse_colour_rgb, diffuse_colour_rgb * /* _linear*/(mesh_faction_color1.rgb), mask_p3);
//    }
//}

//void ps30_get_shared_inputs(out float3 eye_vector, out float3 light_vector, out float4 diffuse_colour, out float4 specular_colour, out float smoothness, out float3x3 normal_basis, out float3 texture_normal, in const PixelInputType input, in const bool faction_colours)
//{
// //   diffuse_colour = shaderTextures[t_Diffuse].Sample(SamplerLinear, input.TexCoord.xy);
// //   diffuse_colour.rgb = _linear(diffuse_colour.rgb);

// //   alpha_test(diffuse_colour.a);

// //   eye_vector = -normalize(vMatrixI[3] - input.Wpos);

// //   light_vector = normalize(light_position0.xyz - input.Wpos);

// //   specular_colour = t_specular_colour.Sample(MMMLWWWSampler, input.TexCoord.xy);
//    //specular_colour.rgb = _linear(specular_colour.rgb);

//	////	This value should be in gamma space...
// //   smoothness = ((t_smoothness.Sample(MMMLWWWSampler, input.TexCoord.xy).rgb)).x;

//	////	Apply faction colours...
// //   apply_faction_colours(diffuse_colour.rgb, input.TexCoord.xy, faction_colours);

//	////  Get the pixel normal in world-space ensuring that it's in 3dsmax's coordinate system...
// //   normal_basis = MAXTBN

//	//// some 3dsmax special stuff here //
//	////#define MAXTBN float3x3 ( normalize ( input.Tgt ) , normalize ( input.Btgt ) , normalize ( input.Nml ) );
// //   if (orthogonalizeTangentBitangentPerPixel)
// //   {
// //       float3 Tn = normal_basis[0];
// //       float3 Bn = normal_basis[1];
// //       float3 Nn = normal_basis[2];
// //       float3 bitangent = normalize(cross(Nn, Tn));
// //       Tn = normalize(cross(bitangent, Nn));
//	//	// Bitangent need to be flipped if the map face is flipped. We don't have map face handedness in shader so make
//	//	// the calculated bitangent point in the same direction as the interpolated bitangent which has considered the flip.
// //       Bn = sign(dot(bitangent, Bn)) * bitangent;
// //       normal_basis = float3x3(Tn, Bn, Nn);
// //   }
// //   texture_normal = (t_normal.Sample(MMMLWWWSampler, input.TexCoord.xy).rgb);
// //   texture_normal = normalSwizzle_UPDATED((texture_normal.rgb * 2.0f) - 1.0f);

//    normal_basis = float3x3(
    
//        0, 0, 0,
//        0, 0, 0,
//        0, 0, 0
//    );
    
//    eye_vector = -normalize(input.viewDirection);
//    light_vector = -normalize(lightData[0].lightDirection);

//    diffuse_colour = shaderTextures[t_Diffuse].Sample(SamplerLinear, input.tex1.xy);
//    diffuse_colour.rgb = _linear(diffuse_colour.rgb);
//    alpha_test(diffuse_colour.a);      

//    specular_colour = shaderTextures[t_Specular].Sample(SamplerLinear, input.tex1.xy);
//    specular_colour.rgb = _linear(specular_colour.rgb);

//	//	This value should be in gamma space...
//    smoothness = /*_linear*/(shaderTextures[t_Specular].Sample(SamplerLinear, input.tex1.xy).r);
//    smoothness = (smoothness); // added by PHAZER, as smooth is so high that specular highlight are so small they are almost invisible   
//    //_gamma(shaderTextures[t_Specular].Sample(SamplerLinear, input.tex1.xy).a);

//	//	Apply faction colours...
//    apply_faction_colours(diffuse_colour.rgb, input.tex1.xy, faction_colours);

//	//  Get the pixel normal in world-space ensuring that it's in 3dsmax's coordinate system...
//   // normal_basis = MAXTBN

//    texture_normal = getPixelNormal(input);
//	// some 3dsmax special stuff here //
//	//#define MAXTBN float3x3 ( normalize ( input.Tgt ) , normalize ( input.Btgt ) , normalize ( input.Nml ) );
//  //  if (orthogonalizeTangentBitangentPerPixel)
//  //  {
//  //      float3 Tn = normal_basis[0];
//  //      float3 Bn = normal_basis[1];
//  //      float3 Nn = normal_basis[2];
//  //      float3 bitangent = normalize(cross(Nn, Tn));
//  //      Tn = normalize(cross(bitangent, Nn));
//		//// Bitangent need to be flipped if the map face is flipped. We don't have map face handedness in shader so make
//		//// the calculated bitangent point in the same direction as the interpolated bitangent which has considered the flip.
//  //      Bn = sign(dot(bitangent, Bn)) * bitangent;
//  //      normal_basis = float3x3(Tn, Bn, Nn);
//  //  }
//  //  texture_normal = (t_normal.Sample(SamplerLinear, input.TexCoord.xy).rgb);
//  //  texture_normal = normalSwizzle_UPDATED((texture_normal.rgb * 2.0f) - 1.0f);

//}

//void ps30_get_shared_inputs_WH(out float3 eye_vector, out float3 light_vector, out float4 diffuse_colour, out float4 specular_colour, out float smoothness, out float3x3 normal_basis, out float3 texture_normal, in const PixelInputType input, in const bool faction_colours)
//{
// //   diffuse_colour = shaderTextures[t_Diffuse].Sample(SamplerLinear, input.TexCoord.xy);
// //   diffuse_colour.rgb = _linear(diffuse_colour.rgb);

// //   alpha_test(diffuse_colour.a);

// //   eye_vector = -normalize(vMatrixI[3] - input.Wpos);

// //   light_vector = normalize(light_position0.xyz - input.Wpos);

// //   specular_colour = t_specular_colour.Sample(MMMLWWWSampler, input.TexCoord.xy);
// //   specular_colour.rgb = _linear(specular_colour.rgb);

//	////	This value should be in gamma space...
// //   smoothness = ((t_smoothness.Sample(MMMLWWWSampler, input.TexCoord.xy).rgb)).x;

//	////	Apply faction colours...
// //   apply_faction_colours(diffuse_colour.rgb, input.TexCoord.xy, faction_colours);

//	////  Get the pixel normal in world-space ensuring that it's in 3dsmax's coordinate system...
// //   normal_basis = MAXTBN

//	//// some 3dsmax special stuff here //
//	////#define MAXTBN float3x3 ( normalize ( input.Tgt ) , normalize ( input.Btgt ) , normalize ( input.Nml ) );
// //   if (orthogonalizeTangentBitangentPerPixel)
// //   {
// //       float3 Tn = normal_basis[0];
// //       float3 Bn = normal_basis[1];
// //       float3 Nn = normal_basis[2];
// //       float3 bitangent = normalize(cross(Nn, Tn));
// //       Tn = normalize(cross(bitangent, Nn));
//	//	// Bitangent need to be flipped if the map face is flipped. We don't have map face handedness in shader so make
//	//	// the calculated bitangent point in the same direction as the interpolated bitangent which has considered the flip.
// //       Bn = sign(dot(bitangent, Bn)) * bitangent;
// //       normal_basis = float3x3(Tn, Bn, Nn);
// //   }
// //   texture_normal = (t_normal.Sample(MMMLWWWSampler, input.TexCoord.xy).rgb);
// //   texture_normal = normalSwizzle_UPDATED((texture_normal.rgb * 2.0f) - 1.0f);

//    normal_basis = float3x3(
    
//        0, 0, 0,
//        0, 0, 0,
//        0, 0, 0
//    );
//    diffuse_colour = shaderTextures[t_Diffuse].Sample(SamplerLinear, input.tex1.xy);
//    //diffuse_colour.rgb = _linear(diffuse_colour.rgb);

//    alpha_test(diffuse_colour.a);

//    eye_vector = -normalize(input.eyePos - input.Wpos.xyz);
//    //eye_vector = -normalize(input.viewDirection);

//    //light_vector = normalize(light_position0.xyz - input.Wpos);
//    light_vector = -normalize(lightData[0].lightDirection);

//    specular_colour = shaderTextures[t_Specular].Sample(SamplerLinear, input.tex1.xy);
//    //specular_colour.rgb = _linear(specular_colour.rgb);

//	//	This value should be in gamma space...
//    smoothness = _linear(shaderTextures[t_GlossMap].Sample(SamplerLinear, input.tex1.xy).r);
//    //smoothness = 0;
//    //_gamma(shaderTextures[t_Specular].Sample(SamplerLinear, input.tex1.xy).a);

//	//	Apply faction colours...
//    apply_faction_colours(diffuse_colour.rgb, input.tex1.xy, faction_colours);

//	//  Get the pixel normal in world-space ensuring that it's in 3dsmax's coordinate system...
//   // normal_basis = MAXTBN

//    texture_normal = getPixelNormal(input);
//	// some 3dsmax special stuff here //
//	//#define MAXTBN float3x3 ( normalize ( input.Tgt ) , normalize ( input.Btgt ) , normalize ( input.Nml ) );
//  //  if (orthogonalizeTangentBitangentPerPixel)
//  //  {
//  //      float3 Tn = normal_basis[0];
//  //      float3 Bn = normal_basis[1];
//  //      float3 Nn = normal_basis[2];
//  //      float3 bitangent = normalize(cross(Nn, Tn));
//  //      Tn = normalize(cross(bitangent, Nn));
//		//// Bitangent need to be flipped if the map face is flipped. We don't have map face handedness in shader so make
//		//// the calculated bitangent point in the same direction as the interpolated bitangent which has considered the flip.
//  //      Bn = sign(dot(bitangent, Bn)) * bitangent;
//  //      normal_basis = float3x3(Tn, Bn, Nn);
//  //  }
//  //  texture_normal = (t_normal.Sample(SamplerLinear, input.TexCoord.xy).rgb);
//  //  texture_normal = normalSwizzle_UPDATED((texture_normal.rgb * 2.0f) - 1.0f);

//}

//float4 ps30_main(in VS_OUTPUT input) : COLOR
//{
//    float3 eye_vector;
//    float3 light_vector;
//    float4 diffuse_colour;
//    float4 specular_colour;
//    float smoothness;
//    float3x3 basis;
//    float3 N;
//    bool faction_colours = true;

//	//	Get the inputs...
//    ps30_get_shared_inputs(eye_vector, light_vector, diffuse_colour, specular_colour, smoothness, basis, N, input, faction_colours);

//	//moving to allow blending
//    float3 pixel_normal = normalize(mul(normalize(N), basis));

//    float shadow = 1.0f;
//    float ambient_occlusion = 1.0f;

//    //  Create the standard material...
//    R2_4_StandardLightingModelMaterial standard_mat = R2_4_create_standard_lighting_material(diffuse_colour, specular_colour, pixel_normal, smoothness, float4(input.Wpos.xyz, 1), shadow, ambient_occlusion);

//    //  Light the pixel...
//    float3 hdr_linear_col = standard_lighting_model_directional_light(light_color0, light_vector, eye_vector, standard_mat);

//    //  Tone-map the pixel...
//    float3 ldr_linear_col = saturate(tone_map_linear_hdr_pixel_value(hdr_linear_col));

//    //  Return gamma corrected value...
//    return float4(_gamma(ldr_linear_col), 1.0f);
//}

//float4 ps30_main_decaldirt(in VS_OUTPUT input) : COLOR
//{
//    float3 eye_vector;
//    float3 light_vector;
//    float4 diffuse_colour;
//    float4 specular_colour;
//    float smoothness;
//    float3x3 basis;
//    float3 N;
//    bool faction_colours = true;

//	//	Get the inputs...
//    ps30_get_shared_inputs(eye_vector, light_vector, diffuse_colour, specular_colour, smoothness, basis, N, input, faction_colours);

//	//decals/dirt - shield type
//    if (b_do_decal)
//    {
//        ps_common_blend_decal(diffuse_colour, N, specular_colour.rgb, diffuse_colour, N, specular_colour.rgb, input.TexCoord.xy, 0, vec4_uv_rect, 1.0);
//    }

//    if (b_do_dirt)
//    {
//        ps_common_blend_dirtmap(diffuse_colour, N, specular_colour.rgb, diffuse_colour, N, specular_colour.rgb, input.TexCoord.xy, float2(f_uv_offset_u, f_uv_offset_v));
//    }

//	//moving to allow blending
//    float3 pixel_normal = normalize(mul(normalize(N), basis));

//    float shadow = 1.0f;
//    float ambient_occlusion = 1.0f;

//	//return float4(pixel_normal,1);
//    //  Create the standard material...
//    R2_4_StandardLightingModelMaterial standard_mat = R2_4_create_standard_lighting_material(diffuse_colour, specular_colour, pixel_normal, smoothness, float4(input.Wpos.xyz, 1), shadow, ambient_occlusion);

//    //  Light the pixel...
//    float3 hdr_linear_col = standard_lighting_model_directional_light(light_color0, light_vector, eye_vector, standard_mat);

//    //  Tone-map the pixel...
//    float3 ldr_linear_col = saturate(tone_map_linear_hdr_pixel_value(hdr_linear_col));

//    //  Return gamma corrected value...
//    return float4(_gamma(ldr_linear_col), 1);
//}

//float4 ps30_main_custom_terrain(in VS_OUTPUT input) : COLOR
//{
//    float3 eye_vector;
//    float3 light_vector;
//    float4 diffuse_colour;
//    float4 specular_colour;
//    float smoothness;
//    float3x3 basis;
//    float3 N;
//    bool faction_colours = false;

//	//	Get the inputs...
//    ps30_get_shared_inputs(eye_vector, light_vector, diffuse_colour, specular_colour, smoothness, basis, N, input, faction_colours);

//	//decals/dirt - shield type
//    ps_common_blend_decal(diffuse_colour, N, specular_colour.rgb, diffuse_colour, N, specular_colour.rgb, input.TexCoord.xy, 0, vec4_uv_rect, 1 - input.Color.a);

//	//moving to allow blending
//    float3 pixel_normal = normalize(mul(normalize(N), basis));

//    float shadow = 1.0f;
//    float ambient_occlusion = 1.0f;

//    //  Create the standard material...
//    R2_4_StandardLightingModelMaterial standard_mat = R2_4_create_standard_lighting_material(diffuse_colour, specular_colour, pixel_normal, smoothness, float4(input.Wpos.xyz, 1), shadow, ambient_occlusion);

//    //  Light the pixel...
//    float3 hdr_linear_col = standard_lighting_model_directional_light(light_color0, light_vector, eye_vector, standard_mat);

//    //  Tone-map the pixel...
//    float3 ldr_linear_col = saturate(tone_map_linear_hdr_pixel_value(hdr_linear_col));

//    //  Return gamma corrected value...
//    return float4(_gamma(ldr_linear_col), 1);
//}

//float4 ps30_main_vfx(in VS_OUTPUT input) : COLOR
//{
//    float3 eye_vector;
//    float3 light_vector;
//    float4 diffuse_colour;
//    float4 specular_colour;
//    float smoothness;
//    float3x3 basis;
//    float3 N;
//    bool faction_colours = true;

//	//	Get the inputs...
//    ps30_get_shared_inputs(eye_vector, light_vector, diffuse_colour, specular_colour, smoothness, basis, N, input, faction_colours);

//	//decals/dirt - shield type
//    if (b_do_decal)
//    {
//        ps_common_blend_decal(diffuse_colour, N, specular_colour.rgb, diffuse_colour, N, specular_colour.rgb, input.TexCoord.xy, 0, vec4_uv_rect, 1.0);
//    }

//    if (b_do_dirt)
//    {
//        ps_common_blend_dirtmap(diffuse_colour, N, specular_colour.rgb, diffuse_colour, N, specular_colour.rgb, input.TexCoord.xy, float2(f_uv_offset_u, f_uv_offset_v));
//    }

//    ps_common_blend_vfx(diffuse_colour, N, specular_colour.rgb, diffuse_colour, N, specular_colour.rgb, input.TexCoord.xy, float2(f_uv_offset_u, f_uv_offset_v));

//	//moving to allow blending
//    float3 pixel_normal = normalize(mul(normalize(N), basis));

//    float shadow = 1.0f;
//    float ambient_occlusion = 1.0f;

//    //  Create the standard material...
//    R2_4_StandardLightingModelMaterial standard_mat = R2_4_create_standard_lighting_material(diffuse_colour, specular_colour, pixel_normal, smoothness, float4(input.Wpos.xyz, 1), shadow, ambient_occlusion);

//    //  Light the pixel...
//    float3 hdr_linear_col = standard_lighting_model_directional_light(light_color0, light_vector, eye_vector, standard_mat);

//    //  Tone-map the pixel...
//    float3 ldr_linear_col = saturate(tone_map_linear_hdr_pixel_value(hdr_linear_col));

//    //  Return gamma corrected value...
//    return float4(_gamma(ldr_linear_col), 1);
//}

//float4 ps30_full_tint_UPDATED(in VS_OUTPUT input) : COLOR
//{
//    float3 eye_vector;
//    float3 light_vector;
//    float4 diffuse_colour;
//    float4 specular_colour;
//    float smoothness;
//    float3x3 basis;
//    float3 N;
//    bool faction_colours = true;

//	//	Get the inputs...
//    ps30_get_shared_inputs(eye_vector, light_vector, diffuse_colour, specular_colour, smoothness, basis, N, input, faction_colours);

//    float3 pixel_normal = normalize(mul(normalize(N), basis));

//    float shadow = 1.0f;
//    float ambient_occlusion = 1.0f;

//    //  Create the standard material...
//    R2_4_StandardLightingModelMaterial standard_mat = R2_4_create_standard_lighting_material(diffuse_colour, specular_colour, pixel_normal, smoothness, float4(input.Wpos.xyz, 1), shadow, ambient_occlusion);

//    //  Light the pixel...
//    float3 hdr_linear_col = standard_lighting_model_directional_light(light_color0, light_vector, eye_vector, standard_mat);

//    //  Tone-map the pixel...
//    float3 ldr_linear_col = saturate(tone_map_linear_hdr_pixel_value(hdr_linear_col));

//    //  Return gamma corrected value...
//    return float4(_gamma(ldr_linear_col), 1.0f);
//}

//float4 ps30_full_ao(in VS_OUTPUT input) : COLOR
//{
//    float3 eye_vector;
//    float3 light_vector;
//    float4 diffuse_colour;
//    float4 specular_colour;
//    float smoothness;
//    float3x3 basis;
//    float3 N;
//    bool faction_colours = false;

//	//	Get the inputs...
//    ps30_get_shared_inputs(eye_vector, light_vector, diffuse_colour, specular_colour, smoothness, basis, N, input, faction_colours);

//    float3 pixel_normal = normalize(mul(normalize(N), basis));

//	//AO texture
//    float3 ao = t_ambient_occlusion_uv2.Sample(MMMLWWWSampler, input.TexCoord.zw);

//    float shadow = 1.0f;
//    float ambient_occlusion = ao;

//    //  Create the standard material...
//    R2_4_StandardLightingModelMaterial standard_mat = R2_4_create_standard_lighting_material(diffuse_colour, specular_colour, pixel_normal, smoothness, float4(input.Wpos.xyz, 1), shadow, ambient_occlusion);

//    //  Light the pixel...
//    float3 hdr_linear_col = standard_lighting_model_directional_light(light_color0, light_vector, eye_vector, standard_mat);

//    //  Tone-map the pixel...
//    float3 ldr_linear_col = saturate(tone_map_linear_hdr_pixel_value(hdr_linear_col));

//    //  Return gamma corrected value...
//    return float4(_gamma(ldr_linear_col), 1.0f);
//}

//float4 ps30_full_dirtmap(in VS_OUTPUT input) : COLOR
//{
//    float3 eye_vector;
//    float3 light_vector;
//    float4 diffuse_colour;
//    float4 specular_colour;
//    float smoothness;
//    float3x3 basis;
//    float3 N;
//    bool faction_colours = false;

//	//	Get the inputs...
//    ps30_get_shared_inputs(eye_vector, light_vector, diffuse_colour, specular_colour, smoothness, basis, N, input, faction_colours);

//    ps_common_blend_dirtmap(diffuse_colour, N, specular_colour.rgb, diffuse_colour, N, specular_colour.rgb, input.TexCoord.xy, float2(f_uv_offset_u, f_uv_offset_v));

//    float3 pixel_normal = normalize(mul(normalize(N), basis));

//    float shadow = 1.0f;
//    float ambient_occlusion = 1.0f;

//    //  Create the standard material...
//    R2_4_StandardLightingModelMaterial standard_mat = R2_4_create_standard_lighting_material(diffuse_colour, specular_colour, pixel_normal, smoothness, float4(input.Wpos.xyz, 1), shadow, ambient_occlusion);

//    //  Light the pixel...
//    float3 hdr_linear_col = standard_lighting_model_directional_light(light_color0, light_vector, eye_vector, standard_mat);

//    //  Tone-map the pixel...
//    float3 ldr_linear_col = saturate(tone_map_linear_hdr_pixel_value(hdr_linear_col));

//    //  Return gamma corrected value...
//    return float4(_gamma(ldr_linear_col), 1.0f);
//}

/////////////////////////////////////////////////////////////
//  TONE MAPPER     /////////////////////////////////////////
/////////////////////////////////////////////////////////////

float3 tone_map_linear_hdr_pixel_value(in float3 linear_hdr_pixel_val)
{
	//	Determine the HDR CIE Log(Y)xy colour of this pixel in the input_val image...
    float4 hdr_CIE_LogYxy_pixel = HDR_RGB_To_HDR_CIE_Log_Y_xy(linear_hdr_pixel_val);

    //  Tone map this pixel and convert to LDR CIE Yxy value...
    float4 tone_mapped_ldr_CIE_Yxy_pixel = tone_map_HDR_CIE_Log_Y_xy_To_LDR_CIE_Yxy(hdr_CIE_LogYxy_pixel);

    //  Convert this LDR CIE Yxy value to linear RGB...
    float4 tone_mapped_ldr_linear_rgb = LDR_CIE_Yxy_To_Linear_LDR_RGB(tone_mapped_ldr_CIE_Yxy_pixel);

    //  Return tone mapped pixel...
    return tone_mapped_ldr_linear_rgb.rgb;
}

////-----------------------------------------------------------------------------------------
////-----------------------------------------------------------------------------------------
float4 HDR_RGB_To_HDR_CIE_Log_Y_xy(in float3 linear_colour_val)
{
	//	First convert to CIE XYZ...(I NEED TO VERIFY THESE NUMBERS FROM ANOTHER UNRELATED SOURCE!)
    float3x3 cie_transform_mat =
    {
        0.4124f, 0.3576f, 0.1805f,
										0.2126f, 0.7152f, 0.0722f,
										0.0193f, 0.1192f, 0.9505f
    };

    float3 cie_XYZ = mul(cie_transform_mat, linear_colour_val);

	//	Now transform this into CIE Yxy...
    float denominator = cie_XYZ.x + cie_XYZ.y + cie_XYZ.z;

    float x = cie_XYZ.x / max(denominator, real_approx_zero);
    float y = cie_XYZ.y / max(denominator, real_approx_zero);

	//	Return CIE Log(Y)xy...
    return float4(log10(max(cie_XYZ.y, real_approx_zero)), x, y, cie_XYZ.y);
}

//-----------------------------------------------------------------------------------------
//	Bias HDR CIE Log(Y)xy with an s-curve, then convert it to LDR CIE Yxy.  This
//	function guarantees that Y >= 0.0f *but* Y can still be over-bright i.e. go above 1.0f.
//	Any pixels with Y above 1.0f will contribute to bloom later in the pipeline.
//-----------------------------------------------------------------------------------------
float4 tone_map_HDR_CIE_Log_Y_xy_To_LDR_CIE_Yxy(in float4 hdr_LogYxy)
{
    //  Store these values in user-friendly variables.  These will be needed later in the tone-mapping process...
    float black_point = Tone_Map_Black;
    float white_point = Tone_Map_White;
    float log_Y_black_point = log10(Tone_Map_Black);
    float log_Y_white_point = log10(Tone_Map_White);

    //  Ensure that the brightness of the pixel is at least as bright as our black point...
    hdr_LogYxy.x = max(hdr_LogYxy.x, log_Y_black_point);

	//	Determine the Log(Y) value in terms of black and white points.  If the original value
	//	is within the black and white points then this value will be between zero and one
	//	otherwise it will be out of this range...
    float log_y_display_range = log_Y_white_point - log_Y_black_point;

    //  Determine the log_y value in the space of the display range...
    float log_y_in_white_black = (hdr_LogYxy.x - log_Y_black_point) / log_y_display_range;

	//	Now bias this value by the s-curve...
    float log_y_in_white_black_scurve_biased = get_scurve_y_pos(log_y_in_white_black);

    //  Convert back to real Log(Y)...
    float biased_log_y = log_Y_black_point + (log_y_in_white_black_scurve_biased * log_y_display_range);

    //  Now convert this value from Log(Y) to just Y...
    float biased_y = pow(10.0f, biased_log_y);

    //  Determine where this is within the linear luminance range in units of this range...
    float ldr_y = (biased_y - black_point) / (white_point - black_point);

    //  Return the LDR adjusted LDR CIE Yxy colour...
    return float4(ldr_y, hdr_LogYxy.yzw);
}

//-----------------------------------------------------------------------------------------
//	Convert LDR CIE Yxy pixel to linear LDR RGB.  Luminance values above 1.0f are clamped
//  to 1.0f.  It might be that a better method is to convert to RGB with unclamped
//  luminance and then clamp the RGB colour, but my hunch at this time is that clamping the
//  luminance is the correct thing to do.  THIS NEEDS TO BE EVALUATED!!!
//  See http://wiki.gamedev.net/index.php/D3DBook:High-Dynamic_Range_Rendering for more
//  details.
//-----------------------------------------------------------------------------------------
float4 LDR_CIE_Yxy_To_Linear_LDR_RGB(in float4 ldr_cie_Yxy)
{
    float Y = ldr_cie_Yxy[0];
    float x = ldr_cie_Yxy[1];
    float y = ldr_cie_Yxy[2];

    float safe_denominator = max(y, real_approx_zero);

    //  First get back the CIE XYZ values...
    float cie_Y = Y;

    float3 cie_XYZ = { x * cie_Y / safe_denominator, cie_Y, (1 - x - y) * cie_Y / safe_denominator };

    //  Now convert this back to RGB...(I NEED TO VERIFY THESE NUMBERS FROM ANOTHER UNRELATED SOURCE! THESE ARE CORRECTLY THE INVERSE OF THE
    //  OTHER VALUE FROM NEAR TOP OF PAGE. SO IT'S THOSE ABOVE VALUES THAT NEED TO BE VERIFIED.)
    float3x3 cie_XYZ_toRGB_transform_mat =
    {
        +3.2405f, -1.5372f, -0.4985f,
										            -0.9693f, +1.8760f, +0.0416f,
										            +0.0556f, -0.2040f, +1.0572f
    };

    float3 rgb = mul(cie_XYZ_toRGB_transform_mat, cie_XYZ);

    rgb.xyz = max(float3(0, 0, 0), rgb);

    return float4(rgb.xyz, 1.0f);
}

float get_scurve_y_pos(const float x_coord)
{
    float point0_y = 0.0f;
    float point1_y = low_tones_scurve_bias;
    float point2_y = high_tones_scurve_bias;
    float point3_y = 1.0f;

    float4 t = { x_coord * x_coord * x_coord, x_coord * x_coord, x_coord, 1.0f };

    float4x4 BASIS =
    {
        -1.0f, +3.0f, -3.0f, +1.0f,
        			    +3.0f, -6.0f, +3.0f, +0.0f,
        			    -3.0f, +3.0f, +0.0f, +0.0f,
                        +1.0f, +0.0f, +0.0f, +0.0f
    };

    float4 g = mul(t, BASIS); //  Hope this is round the right way!!!!!!!!!!!!!!!!!

    //  Because the points are laid out with equal spaces between the x coords, then
    //  t and x are the same thing...
    return (point0_y * g.x) + (point1_y * g.y) + (point2_y * g.z) + (point3_y * g.w);
}

//////////////////////////////////////////////////////////////////////////////////////////////////////
////	technique11 pixel shaders
//////////////////////////////////////////////////////////////////////////////////////////////////////

//float4 ps30_flatdiffuse(in VS_OUTPUT input) : COLOR
//{
//	//get albedo pixel
//    float4 Ct = t_albedo.Sample(MMMLWWWSampler, input.TexCoord.xy);

//    alpha_test(Ct.a);

//    return float4(Ct.rgb, 1);
//}

//float4 ps30_albedo(in VS_OUTPUT input) : COLOR
//{
//	//get albedo pixel
//    float4 Ct = t_albedo.Sample(MMMLWWWSampler, input.TexCoord.xy);
//	// this should eventually include faction colouring or other modifiers to the albedo texture

//    alpha_test(Ct.a);

//    return float4(Ct.rgb, 1);
//}

//float4 ps30_solidalpha(in VS_OUTPUT input) : COLOR
//{
//	//get albedo pixel
//    float4 Ct = t_albedo.Sample(MMMLWWWSampler, input.TexCoord.xy);

//    return float4((Ct.aaa), 1.0f);
//}

//float4 ps30_t_roughness(in VS_OUTPUT input) : COLOR
//{
//	//get roughness pixel
//    float4 roughness_p = t_smoothness.Sample(MMMLWWWSampler, input.TexCoord.xy);
//    return float4(roughness_p.rrr, 1.0f);
//}

//float4 ps30_t_specular(in VS_OUTPUT input) : COLOR
//{
//	//get substance pixel
//    float4 specular_p = t_specular_colour.Sample(MMMLWWWSampler, input.TexCoord.xy);
//    return float4(specular_p.rgb, 1.0f);
//}

//float4 ps30_t_ao(in VS_OUTPUT input) : COLOR
//{
//    float3 ao = t_ambient_occlusion_uv2.Sample(MMMLWWWSampler, input.TexCoord.zw);
//    return float4(ao.rgb, 1.0f);
//}

//float4 ps30_t_normal(in VS_OUTPUT input) : COLOR
//{
//	// get normal and normal map.
//	//const float3x3 basis = float3x3( normalize( input.Tgt ), normalize( input.Btgt ), normalize( input.Nml ) );
//    float3 N = normalSwizzle(t_normal.Sample(MMMLWWWSampler, input.TexCoord.xy).rgb);

//    return float4(N.rgb, 1.0f);
//}

//float4 ps30_t_mask1(in VS_OUTPUT input) : COLOR
//{
//	//get faction mask pixel
//    float4 faction_p = t_mask1.Sample(MMMLWWWSampler, input.TexCoord.xy);
//    return float4(faction_p.rrr, 1.0f);
//}

//float4 ps30_t_mask2(in VS_OUTPUT input) : COLOR
//{
//	//get faction mask pixel
//    float4 faction_p = t_mask2.Sample(MMMLWWWSampler, input.TexCoord.xy);
//    return float4(faction_p.rrr, 1.0f);
//}

//float4 ps30_t_mask3(in VS_OUTPUT input) : COLOR
//{
//	//get faction mask pixel
//    float4 faction_p = t_mask3.Sample(MMMLWWWSampler, input.TexCoord.xy);
//    return float4(faction_p.rrr, 1.0f);
//}

//float4 ps30_t_cubemap(in VS_OUTPUT input) : COLOR
//{
//	// get normal and normal map.
//    const float3x3 basis = MAXTBN

//    float3 N = normalSwizzle_UPDATED(t_normal.Sample(MMMLWWWSampler, input.TexCoord.xy).rgb * 2.0f - 1.0f);
//	// get detail normal, combine.
//    float3 N2 = normalSwizzle_UPDATED(t_detail_normal.Sample(MMMLWWWSampler, input.TexCoord.xy * 1.0).rgb * 2.0f - 1.0f);
//    N = float3(N.x + (N2.x * 1.0), N.y + (N2.y * 1.0), N.z);

//	//xform normal
//    float3 nN = normalize(mul(normalize(N), basis));
//    float3 env = get_environment_colour(reflect(-input.I, nN), 0.0);

//    return float4(env.rgb, 1.0f);
//}

//float4 ps30_t_ambient(in VS_OUTPUT input) : COLOR
//{
//	// get normal and normal map.
//    const float3x3 basis = MAXTBN

//    float3 N = normalSwizzle_UPDATED(_gamma(t_normal.Sample(MMMLWWWSampler, input.TexCoord.xy).rgb) * 2.0f - 1.0f);
//	// get detail normal, combine.
//    float3 N2 = normalSwizzle_UPDATED(_gamma(t_detail_normal.Sample(MMMLWWWSampler, input.TexCoord.xy * 1.0).rgb) * 2.0f - 1.0f);
//    N = float3(N.x + (N2.x * 1.0), N.y + (N2.y * 1.0), N.z);

//	//xform normal
//    float3 nN = normalize(mul(normalize(N), basis));
//    float3 env = cube_ambient(reflect(-input.I, nN)) * 0.2f;

//    return float4(env.rgb, 1.0f);
//}

//float4 ps30_t_ws_normal_map(in VS_OUTPUT input) : COLOR
//{
//	// get normal and normal map.
//    const float3x3 basis = MAXTBN

//    float3 N = normalSwizzle_UPDATED(t_normal.Sample(MMMLWWWSampler, input.TexCoord.xy).rgb * 2.0f - 1.0f);

//	// get detail normal, combine.
//    if (1.0)
//    {
//        float3 N2 = normalSwizzle_UPDATED(t_detail_normal.Sample(MMMLWWWSampler, input.TexCoord.xy * 1.0).rgb * 2.0f - 1.0f);
//        N = float3(N.x + (N2.x * 1.0), N.y + (N2.y * 1.0), N.z);
//    }

//	//xform normal
//    float3 nN = ((normalize(mul(normalize(N), basis))) * 0.5) + 0.5;
//    return float4(nN.rgb, 1.0f);
//}

//float4 ps30_t_dirtmap(in VS_OUTPUT input) : COLOR
//{
//	//get dirtmap pixel
//    float2 dirt_scale = float2(f_uv2_tile_interval_u, f_uv2_tile_interval_v);
//    float4 dirtmap_p = t_dirtmap_uv2.Sample(MMMLWWWSampler, input.TexCoord.xy * dirt_scale);
//    float4 dirtmap_alpha_p = t_alpha_mask.Sample(MMMLWWWSampler, input.TexCoord.zw);

//    return float4(dirtmap_p.rgb, 1.0f);
//}

//float4 ps30_t_alpha_uv2(in VS_OUTPUT input) : COLOR
//{
//	//get dirtmap pixel
//    float4 dirtmap_alpha_p = t_alpha_mask.Sample(MMMLWWWSampler, input.TexCoord.zw);

//    return float4(dirtmap_alpha_p.rgb, 1.0f);
//}

//float4 ps30_valpha(in VS_OUTPUT input) : COLOR
//{
//	//get albedo pixel
//    float4 Ct = t_albedo.Sample(MMMLWWWSampler, input.TexCoord.xy);
//    return float4(input.Color.aaa, Ct.a);
//}

//float4 ps30_vcolour(in VS_OUTPUT input) : COLOR
//{
//	//get albedo pixel
//    float4 Ct = t_albedo.Sample(MMMLWWWSampler, input.TexCoord.xy);

//    return float4(input.Color.rgb, Ct.a);
//}

//float4 ps30_full_skin(in VS_OUTPUT input) : COLOR
//{
//    float3 eye_vector = -normalize(vMatrixI[3] - input.Wpos);

//    float3 light_vector = normalize(light_position0.xyz - input.Wpos);

//    float4 diffuse_colour = t_albedo.Sample(MMMLWWWSampler, input.TexCoord.xy);

//    alpha_test(diffuse_colour.a);

//    float4 specular_colour = t_specular_colour.Sample(MMMLWWWSampler, input.TexCoord.xy);

//    float smoothness = _linear(t_smoothness.Sample(MMMLWWWSampler, input.TexCoord.xy).x);

////cw additions for completeness
//    float3 ao = t_ambient_occlusion_uv2.Sample(MMMLWWWSampler, input.TexCoord.zw).rgb;
//    float mask_p1 = t_mask1.Sample(MMMLWWWSampler, input.TexCoord.xy).r;
//    float mask_p2 = t_mask2.Sample(MMMLWWWSampler, input.TexCoord.xy).r;
//    float mask_p3 = t_mask3.Sample(MMMLWWWSampler, input.TexCoord.xy).r;

//	//  Get the pixel normal in world-space ensuring that it's in 3dsmax's coordinate system...
//    float3x3 basis = MAXTBN

//    float4 Np = (t_normal.Sample(MMMLWWWSampler, input.TexCoord.xy));
//    float3 N = normalSwizzle_UPDATED((Np.rgb * 2.0f) - 1.0f);

//    if (b_do_decal)
//    {
//        ps_common_blend_decal_SKIN(diffuse_colour, N, specular_colour.rgb, diffuse_colour, N, specular_colour.rgb, input.TexCoord.xy, 0, vec4_uv_rect, 1.0);
//    }

//    if (b_do_dirt)
//    {
//        ps_common_blend_dirtmap_SKIN(diffuse_colour, N, specular_colour.rgb, diffuse_colour, N, specular_colour.rgb, input.TexCoord.xy, float2(f_uv_offset_u, f_uv_offset_v));
//    }

//    float3 pixel_normal = normalize(mul(normalize(N), basis));

//    //  Create the standard material...
//    SkinLightingModelMaterial skin_mat = create_skin_lighting_material(smoothness, float3(mask_p1, mask_p2, mask_p3), diffuse_colour, specular_colour, pixel_normal, float4(input.Wpos.xyz, 1));

//    //  Light the pixel...
//    float3 hdr_linear_col = skin_lighting_model_directional_light(light_color0, light_vector, eye_vector, skin_mat);

//    //  Tone-map the pixel...
//    float3 ldr_linear_col = saturate(tone_map_linear_hdr_pixel_value(hdr_linear_col));

//    //  Return gamma corrected value...
//    return float4((ldr_linear_col), 1.0);
//}

//float4 ps30_main_BACKGROUND_CUBE(in VS_OUTPUT input) : COLOR
//{
//	//	Look up the environment map texture...
//    float3 hdr_linear_col = get_environment_colour(normalize(input.Wpos), 0);

//    //  Tone-map the pixel...
//    float3 ldr_linear_col = saturate(tone_map_linear_hdr_pixel_value(hdr_linear_col));

//    //  Return gamma corrected value...
//    return float4((ldr_linear_col), 1.0f);
//}

//float4 ps30_ndotl(in VS_OUTPUT input) : COLOR
//{
//    float4 diffuse_colour = t_albedo.Sample(MMMLWWWSampler, input.TexCoord.xy);
//    float3 light_vector = normalize(light_position0.xyz - input.Wpos);

//	//  Get the pixel normal in world-space ensuring that it's in 3dsmax's coordinate system...
//    float3x3 basis = MAXTBN

//    float4 Np = float4(_gamma(t_normal.Sample(MMMLWWWSampler, input.TexCoord.xy).rgb), 1.0);
//    float3 N = normalSwizzle_UPDATED((Np.rgb * 2.0f) - 1.0f);
//    float3 pixel_normal = normalize(mul(normalize(N), basis));

//    float3 ndotl = saturate(dot(pixel_normal, light_vector));

//    //  Return gamma corrected value...
//    return float4((ndotl), diffuse_colour.a);
//}

////////////////////////////////////////////////////////////////////////////////////////////////////
//	Techniques
////////////////////////////////////////////////////////////////////////////////////////////////////

// technique11 Full
// {
	// pass P0
	// {
		// VertexShader		= compile vs_3_0 vs30_main();
		// PixelShader			= compile ps_3_0 ps30_main();

		// CullMode			= CW;

		// ZEnable				= TRUE;
		// ZFunc				= LESSEQUAL;

		// ALPHATESTENABLE		= TRUE;
		// ALPHABLENDENABLE	= FALSE;
		// ALPHAFUNC			= GREATER;
		// ALPHAREF			= 128.f;
		// alphafunc 			= greater;

		// ZWriteEnable		= TRUE;
	// }
// }

//technique11 Full_standard <
//    bool overridesDrawState = false;
//    int isTransparent = 1;
//    string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//        string Script = "Draw=geometry;";
//        string drawContext = "colorPass";
//    >
//    {
//        SetVertexShader(CompileShader(vs_5_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_5_0, ps30_main()));
//    }
//}

//technique11 Full_standard_alphablend<
//    bool overridesDrawState = false;
//    int isTransparent = 1;
//    string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//        string Script = "Draw=geometry;";
//        string drawContext = "colorPass";
//    >
//    {
//        SetVertexShader(CompileShader(vs_5_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_5_0, ps30_main()));
//    }
//}

//technique11 Full_standard_tint<
//    bool overridesDrawState = false;
//    int isTransparent = 1;
//    string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//        string Script = "Draw=geometry;";
//        string drawContext = "colorPass";
//    >
//    {
//        SetVertexShader(CompileShader(vs_5_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_5_0, ps30_full_tint_UPDATED()));
//    }
//}

//technique11 Full_standard_decaldirt<
//    bool overridesDrawState = false;
//    int isTransparent = 1;
//    string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//        string Script = "Draw=geometry;";
//        string drawContext = "colorPass";
//    >
//    {
//        SetVertexShader(CompileShader(vs_5_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_5_0, ps30_main_decaldirt()));
//    }
//}

//technique11 Full_custom_terrain<
//    bool overridesDrawState = false;
//    int isTransparent = 1;
//    string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//        string Script = "Draw=geometry;";
//        string drawContext = "colorPass";
//    >
//    {
//        SetVertexShader(CompileShader(vs_5_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_5_0, ps30_main_custom_terrain()));
//    }
//}

//technique11 Full_ao<
//    bool overridesDrawState = false;
//    int isTransparent = 1;
//    string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//        string Script = "Draw=geometry;";
//        string drawContext = "colorPass";
//    >
//    {
//        SetVertexShader(CompileShader(vs_5_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_5_0, ps30_full_ao()));
//    }
//}

//technique11 Full_dirtmap<
//    bool overridesDrawState = false;
//    int isTransparent = 1;
//    string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//        string Script = "Draw=geometry;";
//        string drawContext = "colorPass";
//    >
//    {
//        SetVertexShader(CompileShader(vs_5_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_5_0, ps30_full_dirtmap()));
//    }
//}

//technique11 Full_skin<
//    bool overridesDrawState = false;
//    int isTransparent = 1;
//    string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//        string Script = "Draw=geometry;";
//        string drawContext = "colorPass";
//    >
//    {
//        SetVertexShader(CompileShader(vs_5_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_5_0, ps30_full_skin()));
//    }
//}

//technique11 Channel_Colour<
//    bool overridesDrawState = false;
//    int isTransparent = 1;
//    string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//        string Script = "Draw=geometry;";
//        string drawContext = "colorPass";
//    >
//    {
//        SetVertexShader(CompileShader(vs_5_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_5_0, ps30_flatdiffuse()));
//    }
//}

//technique11 Channel_Diffuse<
//    bool overridesDrawState = false;
//    int isTransparent = 1;
//    string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//        string Script = "Draw=geometry;";
//        string drawContext = "colorPass";
//    >
//    {
//        SetVertexShader(CompileShader(vs_5_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_5_0, ps30_albedo()));
//    }
//}

//technique11 Channel_Roughness<
//    bool overridesDrawState = false;
//    int isTransparent = 1;
//    string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//        string Script = "Draw=geometry;";
//        string drawContext = "colorPass";
//    >
//    {
//        SetVertexShader(CompileShader(vs_5_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_5_0, ps30_t_roughness()));
//    }
//}

//technique11 Channel_Specular<
//    bool overridesDrawState = false;
//    int isTransparent = 1;
//    string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//        string Script = "Draw=geometry;";
//        string drawContext = "colorPass";
//    >
//    {
//        SetVertexShader(CompileShader(vs_5_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_5_0, ps30_t_specular()));
//    }
//}

//technique11 Channel_Normal<
//    bool overridesDrawState = false;
//    int isTransparent = 1;
//    string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//        string Script = "Draw=geometry;";
//        string drawContext = "colorPass";
//    >
//    {
//        SetVertexShader(CompileShader(vs_5_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_5_0, ps30_t_normal()));
//    }
//}

//technique11 Channel_SolidAlpha<
//    bool overridesDrawState = false;
//    int isTransparent = 1;
//    string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//        string Script = "Draw=geometry;";
//        string drawContext = "colorPass";
//    >
//    {
//        SetVertexShader(CompileShader(vs_5_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_5_0, ps30_solidalpha()));
//    }
//}

//technique11 Channel_Ambient<
//    bool overridesDrawState = false;
//    int isTransparent = 1;
//    string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//        string Script = "Draw=geometry;";
//        string drawContext = "colorPass";
//    >
//    {
//        SetVertexShader(CompileShader(vs_5_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_5_0, ps30_t_ambient()));
//    }
//}

//technique11 Channel_Ao<
//    bool overridesDrawState = false;
//    int isTransparent = 1;
//    string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//        string Script = "Draw=geometry;";
//        string drawContext = "colorPass";
//    >
//    {
//        SetVertexShader(CompileShader(vs_5_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_5_0, ps30_t_ao()));
//    }
//}

//technique11 Channel_Mask1<
//    bool overridesDrawState = false;
//    int isTransparent = 1;
//    string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//        string Script = "Draw=geometry;";
//        string drawContext = "colorPass";
//    >
//    {
//        SetVertexShader(CompileShader(vs_5_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_5_0, ps30_t_mask1()));
//    }
//}

//technique11 Channel_Mask2<
//    bool overridesDrawState = false;
//    int isTransparent = 1;
//    string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//        string Script = "Draw=geometry;";
//        string drawContext = "colorPass";
//    >
//    {
//        SetVertexShader(CompileShader(vs_5_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_5_0, ps30_t_mask2()));
//    }
//}

//technique11 Channel_Mask3<
//    bool overridesDrawState = false;
//    int isTransparent = 1;
//    string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//        string Script = "Draw=geometry;";
//        string drawContext = "colorPass";
//    >
//    {
//        SetVertexShader(CompileShader(vs_5_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_5_0, ps30_t_mask3()));
//    }
//}

//technique11 Channel_Cubemap<
//    bool overridesDrawState = false;
//    int isTransparent = 1;
//    string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//        string Script = "Draw=geometry;";
//        string drawContext = "colorPass";
//    >
//    {
//        SetVertexShader(CompileShader(vs_5_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_5_0, ps30_t_cubemap()));
//    }
//}

//technique11 Channel_WSNormal<
//    bool overridesDrawState = false;
//    int isTransparent = 1;
//    string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//        string Script = "Draw=geometry;";
//        string drawContext = "colorPass";
//    >
//    {
//        SetVertexShader(CompileShader(vs_5_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_5_0, ps30_t_ws_normal_map()));
//    }
//}

//technique11 Channel_Dirtmap<
//    bool overridesDrawState = false;
//    int isTransparent = 1;
//    string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//        string Script = "Draw=geometry;";
//        string drawContext = "colorPass";
//    >
//    {
//        SetVertexShader(CompileShader(vs_5_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_5_0, ps30_t_dirtmap()));
//    }
//}

//technique11 Channel_Alpha_UV2<
//    bool overridesDrawState = false;
//    int isTransparent = 1;
//    string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//        string Script = "Draw=geometry;";
//        string drawContext = "colorPass";
//    >
//    {
//        SetVertexShader(CompileShader(vs_5_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_5_0, ps30_t_alpha_uv2()));
//    }
//}

//technique11 Channel_Vertex_alpha<
//    bool overridesDrawState = false;
//    int isTransparent = 1;
//    string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//        string Script = "Draw=geometry;";
//        string drawContext = "colorPass";
//    >
//    {
//        SetVertexShader(CompileShader(vs_5_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_5_0, ps30_valpha()));
//    }
//}

//technique11 Channel_Vertex_colour<
//    bool overridesDrawState = false;
//    int isTransparent = 1;
//    string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//        string Script = "Draw=geometry;";
//        string drawContext = "colorPass";
//    >
//    {
//        SetVertexShader(CompileShader(vs_5_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_5_0, ps30_vcolour()));
//    }
//}

//technique11 Full_decaldirt_vfx<
//    bool overridesDrawState = false;
//    int isTransparent = 1;
//    string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//        string Script = "Draw=geometry;";
//        string drawContext = "colorPass";
//    >
//    {
//        SetVertexShader(CompileShader(vs_5_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_5_0, ps30_main_vfx()));
//    }
//}

//technique11 Channel_ndotl<
//    bool overridesDrawState = false;
//    int isTransparent = 1;
//    string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//        string Script = "Draw=geometry;";
//        string drawContext = "colorPass";
//    >
//    {
//        SetVertexShader(CompileShader(vs_5_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_5_0, ps30_ndotl()));
//    }
//}

//technique11 Full_standard_BACKGROUND_HDR_CUBE<
//    bool overridesDrawState = false;
//    int isTransparent = 1;
//    string Script = "Pass=p0;";
//>
//{
//    pass p0 <
//        string Script = "Draw=geometry;";
//        string drawContext = "colorPass";
//    >
//    {
//        SetVertexShader(CompileShader(vs_5_0, vs30_main()));
//        SetHullShader(NULL);
//        SetDomainShader(NULL);
//        SetGeometryShader(NULL);
//        SetPixelShader(CompileShader(ps_5_0, ps30_main_BACKGROUND_CUBE()));
//    }
//}

#endif