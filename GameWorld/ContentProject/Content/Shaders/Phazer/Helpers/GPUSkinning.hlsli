#ifndef GPUSKINNING_HLSLI
#define GPUSKINNING_HLSLI

#include "../InputLayouts.hlsli"

#include "../Shared/AnimationConstants.hlsli"


void DoSkinning(
	in VertexInputType input,
    int vertexWeightCount,
	out float4 position,
	out float3 normal,
	out float3 tangent,
	out float3 binormal)
{       
    position = 0;
    normal = 0;
    tangent = 0;
    binormal = 0;
	
	[unroll]
    for (int i = 0; i < vertexWeightCount; i++)
    {
        // TODO: sawp "FramePoseMatrices out with "tranforms[input.BoneIndices[i]]"
        // ---------------------------------------------------------------------------------
		// transform vertex position
        // ---------------------------------------------------------------------------------
        position += input.Weights[i] * mul(float4(input.position.xyz, 1), tranforms[input.BoneIndices[i]]);

		// ---------------------------------------------------------------------------------
        // tranform lighting vectors, using only rotational part (3x3) of the matrix
        // ---------------------------------------------------------------------------------
        normal.xyz += input.Weights[i] * mul(input.normal.xyz, (float3x3) tranforms[input.BoneIndices[i]]);
        tangent.xyz += input.Weights[i] * mul(input.tangent.xyz, (float3x3) tranforms[input.BoneIndices[i]]);
        binormal.xyz += input.Weights[i] * mul(input.binormal.xyz, (float3x3) tranforms[input.BoneIndices[i]]);
    }
}

#endif // GPUSKINNING_HLSLI