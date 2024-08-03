#ifndef GPUSKINNING_HLSLI
#define GPUSKINNING_HLSLI

#include "../InputLayouts.hlsli"

// Input parameters
bool        CapabilityFlag_ApplyAnimation = false;
float4x4    Animation_Tranforms[256];
int         Animation_WeightCount = 0;


void DoSkinning(
	in VertexInputType input,
	out float4 position,
	out float3 normal,
	out float3 tangent,
	out float3 binormal)
{
    
    if (CapabilityFlag_ApplyAnimation == false)
    {
        position = input.position;
        normal = input.normal;
        tangent = input.tangent;
        binormal = input.binormal;
    }
    else
    {
        position = 0;
        normal = 0;
        tangent = 0;
        binormal = 0;
	
	    [unroll]
        for (int i = 0; i < Animation_WeightCount; i++)
        {
            float4x4 currentTransform4x4 = Animation_Tranforms[input.BoneIndices[i]];
            float3x3 currentTransform3x3 = currentTransform4x4;
            // TODO: sawp "FramePoseMatrices out with "tranforms[input.BoneIndices[i]]"
            // ---------------------------------------------------------------------------------
		    // transform vertex position
            // ---------------------------------------------------------------------------------
            position += input.Weights[i] * mul(float4(input.position.xyz, 1), currentTransform4x4);

		    // ---------------------------------------------------------------------------------
            // tranform lighting vectors, using only rotational part (3x3) of the matrix
            // ---------------------------------------------------------------------------------
            normal.xyz += input.Weights[i] * mul(input.normal.xyz, currentTransform3x3);
            tangent.xyz += input.Weights[i] * mul(input.tangent.xyz,  currentTransform3x3);
            binormal.xyz += input.Weights[i] * mul(input.binormal.xyz, currentTransform3x3);
        }
    }
}

#endif // GPUSKINNING_HLSLI