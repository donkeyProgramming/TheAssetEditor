
float4 DiffuseColor             ;
float3 EmissiveColor            ;
float3 SpecularColor            ;
float  SpecularPower            ;
								;
float3 DirLight0Direction       ;
float3 DirLight0DiffuseColor    ;
float3 DirLight0SpecularColor   ;
								;
float3 DirLight1Direction       ;
float3 DirLight1DiffuseColor    ;
float3 DirLight1SpecularColor   ;
								;
float3 DirLight2Direction       ;
float3 DirLight2DiffuseColor    ;
float3 DirLight2SpecularColor   ;

float3 EyePosition              ;
float4x4 World;
float3x3 WorldInverseTranspose;
float4x4 WorldViewProj;

Texture2D<float4> NormalTexture;

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

	//////////Helper function
struct ColorPair
{
	float3 Diffuse;
	float3 Specular;
};

void AddSpecular(inout float4 color, float3 specular)
{
	color.rgb += specular * color.a;
}

ColorPair ComputeLights(float3 eyeVector, float3 worldNormal, uniform int numLights)
{
	float3x3 lightDirections = 0;
	float3x3 lightDiffuse = 0;
	float3x3 lightSpecular = 0;
	float3x3 halfVectors = 0;

	[unroll]
	for (int i = 0; i < numLights; i++)
	{
		lightDirections[i] = float3x3(DirLight0Direction, DirLight1Direction, DirLight2Direction)[i];
		lightDiffuse[i] = float3x3(DirLight0DiffuseColor, DirLight1DiffuseColor, DirLight2DiffuseColor)[i];
		lightSpecular[i] = float3x3(DirLight0SpecularColor, DirLight1SpecularColor, DirLight2SpecularColor)[i];

		halfVectors[i] = normalize(eyeVector - lightDirections[i]);
	}

	float3 dotL = mul(-lightDirections, worldNormal);
	float3 dotH = mul(halfVectors, worldNormal);

	float3 zeroL = step(0, dotL);

	float3 diffuse = zeroL * dotL;
	float3 specular = pow(max(dotH, 0) * zeroL, SpecularPower);

	ColorPair result;

	result.Diffuse = mul(diffuse, lightDiffuse) * DiffuseColor.rgb + EmissiveColor;
	result.Specular = mul(specular, lightSpecular) * SpecularColor;

	return result;
}
//
//CommonVSOutput ComputeCommonVSOutputWithLighting(float4 position, float3 normal, uniform int numLights)
//{
//	CommonVSOutput vout;
//
//	float4 pos_ws = mul(position, World);
//	float3 eyeVector = normalize(EyePosition - pos_ws.xyz);
//	float3 worldNormal = normalize(mul(normal, WorldInverseTranspose));
//
//	ColorPair lightResult = ComputeLights(eyeVector, worldNormal, numLights);
//
//	vout.Pos_ps = mul(position, WorldViewProj);
//	vout.Diffuse = float4(lightResult.Diffuse, DiffuseColor.a);
//	vout.Specular = lightResult.Specular;
//
//	return vout;
//}

//---------------------


struct VertexInputType
{
	float4 Position : POSITION;
	
	float2 tex : TEXCOORD0;

	float3 normal : NORMAL0;
	float3 tangent : TANGENT;
	float3 binormal : BINORMAL;

	float4 Weights : COLOR;
	float4 BoneIndices : BLENDINDICES0;
};

struct PixelInputType
{
	float4 Position : SV_POSITION;
	float2 tex : TEXCOORD0;

	float3 normal : NORMAL0;
	float3 tangent : TANGENT;
	float3 binormal : BINORMAL;

	float4 WorldPosition : TEXCOORD5;
};


PixelInputType MainVS(in VertexInputType input)
{
	PixelInputType output = (PixelInputType)0;

	output.WorldPosition = mul(input.Position, World);
	output.Position = mul(input.Position, WorldViewProj);


	output.normal = normalize(mul(input.normal, WorldInverseTranspose));	// World
	output.tangent = normalize(mul(input.tangent, WorldInverseTranspose));	// World
	output.binormal = normalize(mul(input.binormal, WorldInverseTranspose));	// World

    output.tex = input.tex;

	return output;
}

float4 MainPS(PixelInputType pin) : COLOR
{

	float4 color = float4(0.5f,0,0,1);///pin.Diffuse;

	
	//float3 worldNormal = normalize(pin.normal);

	float4 NormalTex = NormalTexture.Sample(s_normal, pin.tex);
	float3 Np;
	Np.x = NormalTex.r * NormalTex.a;
	Np.y = NormalTex.g;
	
	Np = (Np * 2.0f) - 1.0f;
	Np.z = sqrt(1 - Np.x * Np.x - Np.y * Np.y);
	float3 Normal = Np.yzx; // Works

	float3x3 basis = float3x3(normalize(pin.tangent), normalize(pin.normal), normalize(pin.binormal)); // -- WOWRK2!pp§
	float3 worldNormal = normalize(mul(normalize(Np), basis));
	
	float3 eyeVector = normalize(EyePosition - pin.WorldPosition.xyz);
	ColorPair lightResult = ComputeLights(eyeVector, worldNormal, 3);

	color.rgb *= lightResult.Diffuse;

	AddSpecular(color, lightResult.Specular);

	return color;
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile vs_5_0 MainVS();
		PixelShader = compile ps_5_0 MainPS();
	}
};