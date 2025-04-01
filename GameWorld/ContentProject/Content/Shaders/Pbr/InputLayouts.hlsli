#ifndef INPUTLAYOUT_HSLSI
#define INPUTLAYOUT_HSLSI

struct VertexInputType
{
    float4 position : POSITION;
    float3 normal : NORMAL0;
    float2 tex : TEXCOORD0;

    float3 tangent : TANGENT;
    float3 binormal : BINORMAL;

    float4 Weights : COLOR;
    float4 BoneIndices : BLENDINDICES0;
    
    float2 tex1 : TEXCOORD0;
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

struct GBufferMaterial
{
    float4 position;
    
    float3 pixelNormal;
    float metalness;
    
    float4 diffuse;
    float3 specular;
    float roughness;    
    float4 maskValue;
};    
#endif