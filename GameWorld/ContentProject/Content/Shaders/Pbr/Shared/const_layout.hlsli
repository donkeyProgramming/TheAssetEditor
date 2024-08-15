#ifndef CONST_LAYOUT_HLSLI
#define CONST_LAYOUT_HLSLI



float4x4 World;
float4x4 View;
float3 CameraPos;
float4x4 Projection;

float4x4 EnvMapTransform;
float4x4 DirLightTransform;

float LightMult = 1;

bool debug = true;
float exposure = 1;

float light0_roughnessFactor = 1;
float light0_radiannce = 1;
float light0_ambientFactor = 1;

float3 TintColour = float3(1, 1, 1);

// Textures
Texture2D<float4> DiffuseTexture;
Texture2D<float4> SpecularTexture;
Texture2D<float4> NormalTexture;
Texture2D<float4> GlossTexture;
Texture2D<float4> MaskTexture;

TextureCube<float4> tex_cube_diffuse;
TextureCube<float4> tex_cube_specular;
Texture2D<float4> specularBRDF_LUT;

bool UseDiffuse = true;
bool UseSpecular = true;
bool UseNormal = true;
bool UseGloss = true;
bool UseAlpha = false;
bool UseMask = true;



#endif // CONST_LAYOUT_HLSLI