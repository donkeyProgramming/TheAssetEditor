// Camera ----------
float4x4 World;
float4x4 View;
float4x4 Projection;
float4x4 WorldInverseTranspose;
float3 CameraPosition;
// -----------------

// ShadingValues ---
float4 AmbientColor = float4(1, 1, 1, 1);
float AmbientIntensity = 0.4;
float3 DiffuseLightDirection = float3(1, 0, 0);
float4 DiffuseColor = float4(1, 1, 1, 1);
float DiffuseIntensity = 1.0;

float LightStrength = 5;
// -----------------

// Textues ---------
bool HasIBL = false;
Texture IBLTexture;
samplerCUBE IBLSampler = sampler_state
{
    texture = <IBLTexture>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = Mirror;
    AddressV = Mirror;
};


bool HasDiffuse = false;
texture DiffuseTexture;
sampler2D DiffuseSampler = sampler_state {
    Texture = (DiffuseTexture);
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

bool HasSpecular = false;
texture SpecularTexture;
sampler2D SpecularSampler = sampler_state {
    Texture = (SpecularTexture);
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

bool HasMask = false;
texture MaskTexture;
sampler2D MaskSampler = sampler_state {
    Texture = (MaskTexture);
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};
// -----------------


texture ModelTexture;
sampler2D textureSampler = sampler_state {
    Texture = (ModelTexture);
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

struct VertexShaderInput
{
    float4 Position : SV_POSITION;    
    float3 Normal : NORMAL0;
	float2 TextureCoordinate : TEXCOORD0;
};


struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float2 TextureCoordinate: TEXCOORD0;
    float3 Normal  : NORMAL0;
    float4 Color : COLOR0;
    float3 eyeVec : TEXCOORD1;
};


int AlphaMode;


void AlphaProcess(float alphaValue);


float ComputeIndirectDiffuse(float3 normal)
{
    float4 indirectDiffuse = texCUBE(IBLSampler, normal);
    float averageLightValue = (indirectDiffuse.x + indirectDiffuse.y + indirectDiffuse.z) / 3;
    return averageLightValue * LightStrength;
}

float ComputeIndirectSpecular(float3 normal, float3 eyeVector, float reflectivity)
{
    float3 R = reflect(eyeVector, normal);
    float4 indirectSpecular = texCUBE(IBLSampler, R) * reflectivity;
    float averageLightValue = (indirectSpecular.x + indirectSpecular.y + indirectSpecular.z) / 3;
    return averageLightValue * LightStrength;
}

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

    float3 normal = mul(input.Normal, WorldInverseTranspose);
    float lightIntensity = dot(normal.xyz, DiffuseLightDirection);
    output.Color = saturate(DiffuseColor * DiffuseIntensity * lightIntensity);
	output.Color.w = 1;
    output.TextureCoordinate = input.TextureCoordinate;
    output.Normal = normalize(mul(input.Normal, WorldInverseTranspose));

    output.eyeVec = normalize(worldPosition - float4(CameraPosition, 1));

    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : SV_TARGET0
{
    float Reflectivity = 0.5;

     float4 diffuseColour = float4(1,1,1,1);
     if(HasDiffuse) 
         diffuseColour = tex2D(DiffuseSampler, input.TextureCoordinate);

     AlphaProcess(diffuseColour.a);

     half oneMinusReflectivity = 1 - Reflectivity;
     diffuseColour.xyz = lerp(0, diffuseColour.xyz, oneMinusReflectivity);

     float4 specularColour = float4(1, 1, 1, 1);
     if (HasSpecular)
        specularColour = tex2D(SpecularSampler, input.TextureCoordinate);
     specularColour.a = 1;

     float3 N = input.Normal;
     float3 eyeVec = normalize(input.eyeVec);

     float indirectDiffuse = ComputeIndirectDiffuse(N);
     float4 diffuse = diffuseColour * indirectDiffuse;

     float indirectSpecular = ComputeIndirectSpecular(N, eyeVec, Reflectivity);
     float4 specular = specularColour * indirectSpecular;

     float4 finalColour =  saturate(diffuse + specular);// + AmbientColor * AmbientIntensity) * float4(input.TextureCoordinate.x, input.TextureCoordinate.y,0,1);
     finalColour.a = 1;
     return finalColour;
}

void AlphaProcess(float alphaValue)
{
    // Opaque = 0,
    // Alpha_Test = 1,
    // Alpha_Blend = -1

    if (AlphaMode != 0)
    {
        if (alphaValue != 1)
            clip(-1);
    }
}


technique Diffuse
{
    pass Pass1
    {
        //AlphaBlendEnable = TRUE;
        VertexShader = compile vs_4_0 VertexShaderFunction();
        PixelShader = compile ps_4_0 PixelShaderFunction();
    }
}