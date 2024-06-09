    


struct VertexInputType
{
	
    float4 position : POSITION;
    float2 tex : TEXCOORD0;
    float3 normal : NORMAL;
	
    float3 tangent : TANGENT;
    float3 binormal : BINORMAL;


    //float4 bin : TEXCOORD1;
    float4 Weights : BLENDWEIGHTS;
    uint4 BoneIndices : BLENDINDICES;
};



struct PixelInputType
{
    float4 position : SV_POSITION;
    float2 tex : TEXCOORD0;
    float3 normal : NORMAL0;
    float3 normal2 : NORMAL1;

    float3 tangent : TANGENT;
    float3 binormal : BINORMAL;

    float3 viewDirection : TEXCOORD1;
    float4 viewPos : TEXCOORD2;
    float4 norm : TEXCOORD3;
    float3 eye : TEXCOORD4;
    float3 worldPosition : TEXCOORD5;
        
    float4 color1 : COLOR1;
    float4 rig_colors[4] : COLOR2;    
    
};
