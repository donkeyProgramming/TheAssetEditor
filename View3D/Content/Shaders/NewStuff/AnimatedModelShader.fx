/*
Copyright(c) 2017 by kosmonautgames

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

// Uniform color for skinned meshes
// Draws a mesh with one color only

////////////////////////////////////////////////////////////////////////////////////////////////////////////
//  Variables
#include "helper.fx"

#define SKINNED_EFFECT_MAX_BONES   72

float3 CameraPosition;

float4x4 ViewProj;
float4x4 View;
float4x4 World;
float3x3 WorldIT;

float FarClip = 500;
bool EnableShit = true;

float4x3 Bones[SKINNED_EFFECT_MAX_BONES];

float Metallic = 0.3f;
bool UseMetallicMap = false;
float Roughness = 0.3f;
bool UseRoughnessMap = false;

float4 AlbedoColor = float4(1, 1, 1, 1);
bool UseAlbedoMap = false;

bool UseNormalMap = false;
bool UseLinear = true;
bool UseAo = false;
bool UsePOM = false;
bool UseBumpmap = false;
float POMScale = 0.05f;
float POMQuality = 1;
bool POMCutoff = true;


float CubeSize = 512;

Texture2D<float4> NormalMap;
Texture2D<float4> AlbedoMap;
Texture2D<float4> MetallicMap;
Texture2D<float4> RoughnessMap;
Texture2D<float4> AoMap;
Texture2D<float4> HeightMap;

Texture2D<float4> FresnelMap;
TextureCube<float4> EnvironmentMap;

SamplerState TextureSampler
{
	Texture = <AlbedoMap>;
	/*MinFilter = LINEAR;
	MagFilter = LINEAR;
	Mipfilter = LINEAR;*/
	Filter = Anisotropic;
	MaxAnisotropy = 8;
	AddressU = Wrap;
	AddressV = Wrap;
};

SamplerState FresnelSampler
{
	Texture = <FresnelMap>;
	MinFilter = LINEAR;
	MagFilter = LINEAR; 
	Mipfilter = LINEAR;

	AddressU = Clamp;
	AddressV = Clamp;
};

SamplerState AoSampler
{
	Texture = <AoMap>;
	MinFilter = LINEAR;
	MagFilter = POINT;
	Mipfilter = POINT;

	AddressU = Clamp;
	AddressV = Clamp;
};

SamplerState CubeMapSampler
{
	Texture = <EnvironmentMap>;
	AddressU = CLAMP;
	AddressV = CLAMP;
	MagFilter = LINEAR;
	MinFilter = LINEAR;
	Mipfilter = LINEAR;
};

////////////////////////////////////////////////////////////////////////////////////////////////////////////
//  Structs

struct VertexShaderInput
{
	float3 Position : POSITION0;
	float3 Normal   : NORMAL0;
	float2 TexCoord : TEXCOORD0;
};


struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
    float3 Normal : NORMAL;
	float2 TexCoord : TEXCOORD1;
	float3 WorldPosition : TEXCOORD2;
	float4 ScreenTexCoord :TEXCOORD3;
}; 

struct LightingParams
{
	float4 Color : COLOR0;
	float3 Normal : TEXCOORD0;
	float Metallic : TEXCOORD1;
	float Roughness : TEXCOORD2;
	float3 WorldPosition : TEXCOORD3;
	float2 ScreenTexCoord : TeXCOORD4;
};

////////////////////////////////////////////////////////////////////////////////////////////////////////////
//  Functions

////////////////////////////////////////////////////////////////////////////////////////////////////////////
//  VS

VertexShaderOutput Unskinned_VertexShaderFunction(VertexShaderInput input)
{
	VertexShaderOutput output;

	float4 WorldPosition = mul(float4(input.Position, 1), World);
	output.WorldPosition = WorldPosition.xyz;
	output.Position = mul(WorldPosition, ViewProj);
	output.Normal = mul(input.Normal, WorldIT).xyz;
	output.TexCoord = input.TexCoord;
	output.ScreenTexCoord = output.Position; /*0.5f * (float2(output.Position.x, -output.Position.y) / output.Position.w + float2(1, 1));*/
	return output;
}






////////////////////////////////////////////////////////////////////////////////////////////////////////////
//  PS



//http://the-witness.net/news/2012/02/seamless-cube-map-filtering/

float3 FixCubeLookup(float3 v, int level)
{
	float M = max(max(abs(v.x), abs(v.y)), abs(v.z));
	//float size = CubeSize >> level;
	//float scale = (size - 1) / size;
	float scale = 1 - exp2(level) / CubeSize;
	if (abs(v.x) != M) v.x *= scale;
	if (abs(v.y) != M) v.y *= scale;
	if (abs(v.z) != M) v.z *= scale;
	return v;
}

float4 Lighting(LightingParams input)
{
	float3 normal = normalize(input.Normal);

	float4 color = input.Color;
	if (UseLinear) color = pow(abs(color), 2.2f);

	float metallic = input.Metallic;
	float roughness = input.Roughness;

	float3 viewDir = normalize(input.WorldPosition - CameraPosition);

	float f0 = lerp(0.04f, color.g * 0.25 + 0.75, metallic);

	float2 fresnelFactor = FresnelMap.SampleLevel(FresnelSampler, float2(roughness, 1-dot(-viewDir, normal)), 0).rg;

	float3 reflectVector = -reflect(-viewDir, normal);

	float3 specularReflection = EnvironmentMap.SampleLevel(CubeMapSampler, FixCubeLookup(reflectVector.xzy, roughness * 7), roughness * 7).rgb;
	if (UseLinear) 
		specularReflection = pow(abs(specularReflection), 2.2f);

	specularReflection = specularReflection * (fresnelFactor.r * f0 + fresnelFactor.g);
	//specularReflection = lerp(float4(0, 0, 0, 0), specularReflection, fresnelFactor);

	float3 diffuseReflection = EnvironmentMap.SampleLevel(CubeMapSampler, FixCubeLookup(reflectVector.xzy, 7),7).rgb ;
	if (UseLinear) 
		diffuseReflection = pow(abs(diffuseReflection), 2.2f);

	diffuseReflection *= (1 - (fresnelFactor.r * f0 + fresnelFactor.g));

	float3 plasticFinal = color.rgb * (/*diffuseLight +*/ diffuseReflection)/*+specularLight*/ + specularReflection; //ambientSpecular;
	if (UseLinear) 
		plasticFinal = pow(abs(plasticFinal), 0.45454545f);

	float3 metalFinal = (/*specularLight +*/ specularReflection)* color.rgb;
	if (UseLinear)
		metalFinal = pow(abs(metalFinal), 0.45454545f);

	float3 finalValue = lerp(plasticFinal, metalFinal, metallic);

	[branch]
	if (UseAo)
	{
		float ao = AoMap.SampleLevel(AoSampler, input.ScreenTexCoord, 0).r;

		//increase ao
		ao = 1 - ((1 - ao) * 2);

		finalValue *= ao;
	}
	if (EnableShit)
		return float4(diffuseReflection.xyz, 1);
	else
		return float4(finalValue,1);
}

//http://www.rorydriscoll.com/2012/01/11/derivative-maps/




// Move the normal away from the surface normal in the opposite surface gradient direction





float4 PixelShaderFunction(VertexShaderOutput input) : SV_TARGET0
{
	float3 normal = normalize(input.Normal);

	float sampleLevel = AlbedoMap.CalculateLevelOfDetailUnclamped(TextureSampler, input.TexCoord);

	float4 albedo = AlbedoColor;

	[branch]
	if (UseAlbedoMap)
	{
		albedo = AlbedoMap.SampleLevel(TextureSampler, input.TexCoord, sampleLevel);
	}

	float roughness = Roughness;
	[branch]
	if (UseRoughnessMap)
	{
		roughness = RoughnessMap.SampleLevel(TextureSampler, input.TexCoord, sampleLevel).r;
	}

	float metallic = Metallic;
	[branch]
	if (UseMetallicMap)
	{
		metallic = MetallicMap.SampleLevel(TextureSampler, input.TexCoord, sampleLevel).r;
	}



	LightingParams renderParams;

	renderParams.Color = albedo;
	renderParams.Normal = normal;
	////renderParams.Depth = input.Depth;
	renderParams.Metallic = metallic;
	renderParams.Roughness = roughness;
	renderParams.WorldPosition = input.WorldPosition;
	renderParams.ScreenTexCoord = 0.5f * (float2(input.ScreenTexCoord.x, -input.ScreenTexCoord.y) / input.ScreenTexCoord.w + float2(1, 1));

	return Lighting(renderParams);
}


float2 CalculatePOM(float3 WorldPosition, float3x3 TangentSpace, float2 texCoords, float sampleLevel)
{
	float height_scale = POMScale / 5.0f;
	TangentSpace = transpose(TangentSpace);
	float3 cameraPositionTS = mul(CameraPosition, TangentSpace);
	float3 positionTS = mul(WorldPosition, TangentSpace);
	//Vector TO camera
	float3 viewDir = normalize(cameraPositionTS - positionTS);

	//steepness
	float steepness = (1 - viewDir.z);

	//float height = HeightMap.SampleLevel(TextureSampler, texCoords, sampleLevel);

	//texCoords = texCoords + viewDir.xy * height * height_scale;

	// number of depth layers
	float numLayers = lerp(10, 40, steepness) * POMQuality;
	// calculate the size of each layer
	float layerDepth = 1.0 / numLayers;
	// depth of current layer
	float currentLayerDepth = 0.0;

	float2 P = viewDir.xy/(max(viewDir.z, 0.2f)) * abs(height_scale);
	float2 deltaTexCoords = P / numLayers;

	float f1 = saturate(sign(height_scale));
	float f2 = sign(-height_scale);

	float2  currentTexCoords = texCoords;
	float currentDepthMapValue = f1 + f2 *HeightMap.SampleLevel(TextureSampler, currentTexCoords, sampleLevel).r;

	[loop]
	while (currentLayerDepth < currentDepthMapValue)
	{
		// shift texture coordinates along direction of P
		currentTexCoords -= deltaTexCoords;
		// get depthmap value at current texture coordinates
		currentDepthMapValue = f1 + f2 *HeightMap.SampleLevel(TextureSampler, currentTexCoords, sampleLevel).r;
		// get depth of next layer
		currentLayerDepth += layerDepth;
	}

	float2 prevTexCoords = currentTexCoords + deltaTexCoords;

	// get depth after and before collision for linear interpolation
	float afterDepth = currentDepthMapValue - currentLayerDepth;
	float beforeDepth = f1 + f2 *HeightMap.SampleLevel(TextureSampler, prevTexCoords, sampleLevel).r - currentLayerDepth + layerDepth;

	// interpolation of texture coordinates
	float weight = afterDepth / (afterDepth - beforeDepth);
	float2 finalTexCoords = lerp(currentTexCoords, prevTexCoords, weight); /* prevTexCoords * weight + currentTexCoords * (1.0 - weight);*/

	if (POMCutoff)
	if (finalTexCoords.x != saturate(finalTexCoords.x) || finalTexCoords.y != saturate(finalTexCoords.y))
	{
		discard;
	}

	return finalTexCoords;
}





////////////////////////////////////////////////////////////////////////////////////////////////////////////
//  Techniques

technique Unskinned
{
	pass Pass1
	{
		VertexShader = compile vs_5_0 Unskinned_VertexShaderFunction();
		PixelShader = compile ps_5_0 PixelShaderFunction();
	}
}
