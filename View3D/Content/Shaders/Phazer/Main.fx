
#include "pbr_lib.fx"
#include "tone_mapping.fx"

//#include "vs_const_buffers.hlsli"
//#include "ps_vs_structs.hlsli"
//#include "common_functions.hlsli"

bool doAnimation = true;

float4x4 World;
float4x4 View;
float4x4 Projection;


float4x4  EnvMapTransform;


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


// Textures
Texture2D<float4> DiffuseTexture;
Texture2D<float4> SpecularTexture;
Texture2D<float4> NormalTexture;
Texture2D<float4> GlossTexture;

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


sampler2D DiffuseSampler = sampler_state {

    Texture = <DiffuseTexture>;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
    Mipfilter = LINEAR;
    Filter = Anisotropic;
    MaxAnisotropy = 16;
    AddressU = Wrap;
    AddressV = Wrap;
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
	return (tex_cube_specular.SampleLevel(SampleType, direction , lod).rgb);
}


float3 sample_environment_specular(in float roughness_in, in float3 reflected_view_vec)
{  
    const float env_lod_pow = 1.8f;
    const float env_map_lod_smoothness = adjust_linear_smoothness(1 - roughness_in);
    const float roughness = 1.0f - pow(env_map_lod_smoothness, env_lod_pow);

    float texture_num_lods = 3;
    float env_map_lod = roughness * (texture_num_lods - 1);//<------- LOWER = more reflective
    float3 environment_colour = get_environment_colour(reflected_view_vec, env_map_lod);


    float3 result = environment_colour * get_cube_env_scale_factor();

    return result;
}



// --------------------- Vertex shader
PixelInputType main(in VertexInputType input) // main is the default function name
{
	PixelInputType output;

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

		for (int i = 0; i < WeightCount; i++)
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

	output.position = mul(output.position, World);
    output.worldPosition = output.position.xyz;
	output.position = mul(output.position, View);
	output.position = mul(output.position, Projection);

    output.tex = input.tex;

	output.normal = normalize(mul(output.normal, (float3x3) World));
    output.tangent = normalize(mul(output.tangent, (float3x3) World));
    output.binormal = normalize(mul(output.binormal, (float3x3) World));

	// Calculate the position of the vertex in the world.
	output.viewDirection = normalize(View[3] - output.worldPosition);
	return output;

}
// --------------------- Vertex shader End

// --------------------- Pixel shader
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

static const float texture_alpha_ref = 0.7f;

void alpha_test(in const float pixel_alpha)
{
	clip(pixel_alpha - texture_alpha_ref);
}




float4 mainPs(in PixelInputType _input, bool bIsFrontFace : SV_IsFrontFace) : SV_TARGET0
{
    PixelInputType input;
    input = _input;

    if (bIsFrontFace)
    {
        input.normal *= -1;
        input.tangent *= -1;
        input.binormal *= -1;
    }

    float4 SpecTex = float4(0, 0, 0, 1);
    if (UseSpecular)
    {
        SpecTex = SpecularTexture.Sample(SampleType, input.tex);
        SpecTex = pow(SpecTex, 2.2);
    }


    float4 DiffuseTex = float4(0.5f, 0.5f, 0.5f, 1);
    if (UseDiffuse)
    {
        DiffuseTex = tex2D(DiffuseSampler, input.tex);
        DiffuseTex = pow(DiffuseTex, 2.2);
        DiffuseTex.rgb = DiffuseTex.rgb * (1 - max(SpecTex.b, max(SpecTex.r, SpecTex.g)));
    }


    float4 GlossTex = float4(0, 0, 0, 1);
    if (UseGloss)
        GlossTex = GlossTexture.Sample(SampleType, input.tex);

    float4 NormalTex = float4(0, 0, 0, 1);
    if (UseNormal)
        NormalTex = NormalTexture.Sample(s_normal, input.tex);

    
    
    float smoothness = substance_smoothness_get_our_smoothness(GlossTex.r);  
	float roughness = saturate((1 - smoothness));



    // Deccode the TW nortex_cube_specular with orthogonal projection
    float3 Np;

    Np.x = NormalTex.r* NormalTex.a;
    Np.y = NormalTex.g;
    Np = (Np * 2.0f) - 1.0f;
    Np.z = sqrt(1 - Np.x * Np.x - Np.y * Np.y);
    float3 _N = Np.yzx; // Works

    //float3x3 basis = float3x3(normalize(input.tangent), normalize(input.normal), normalize(input.binormal)); 
    //float3 bumpNormal = normalize(mul(normalize(_N), basis));

    float3x3 basis = float3x3(normalize(input.tangent), normalize(input.binormal), normalize(input.normal));
    float3 bumpNormal = normalize(mul(normalize(Np), basis));


    // ************************************************************************
    //bumpNormal = input.normal; // enable to DISABLE normal tex_cube_specular

    // ************************************************************************	
    float3 N = normalize(bumpNormal);
    float3 N2 = mul(bumpNormal, (float3x3) EnvMapTransform);
    N2 = normalize(N2);

    //float3 Lo = float3(0,0,1);
    float3 Lo = normalize(input.viewDirection);

    // Angle between surface normal and outgoing light direction.
    float cosLo = max(0.0, dot(N, Lo));

    // Specular reflection vector.
   // float3 Lr = 2.0 * cosLo * N- Lo;  // written out reflect formula
    float3 Lr = reflect(N, Lo); // HLSL intrisic reflection function  

    // rotate refletion map by rotating the reflect vector
    Lr = mul(Lr, (float3x3) EnvMapTransform);

    // specular    
    float3 F0 = SpecTex.rgb;

    float3 irradiance = tex_cube_diffuse.Sample(SampleType, N2).rgb;
    float3 F = fresnelSchlickRoughness(cosLo, F0, roughness);

    float3 kS = F;
    float3 kD = 1.0 - kS;

    float3 diffuseIBL = kD * DiffuseTex.rgb * irradiance;
    float3 specularIrradiance = sample_environment_specular(roughness, normalize(Lr));
    float2 brdf = specularBRDF_LUT.Sample(spBRDF_Sampler, float2(cosLo, (1.0 - roughness))).xy;
    float3 specularIBL = F0 * (brdf.x + brdf.y) * specularIrradiance;  
    float3 ambientLighting = (specularIBL + diffuseIBL);// * light[0].ambientFactor;


    float4 color = float4(ambientLighting, 1.0);

    if (UseAlpha == 1)
    {
        alpha_test(DiffuseTex.a);
    }

    const float gamma_value = 2.2;

    float3 hdrColor = color.rgb * exposure * 0.9;
    float3 mapped = Uncharted2ToneMapping(hdrColor);
    mapped = pow(mapped, 1.0 / gamma_value);
    color = float4(mapped, 1);

    return color;

}
float4 SimplePixel(in PixelInputType _input/*, bool bIsFrontFace : SV_IsFrontFace*/) : SV_TARGET0
{
	return float4(1,0,0,1);
}


technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile vs_5_0 main();
		PixelShader = compile ps_5_0 mainPs();
	}
};