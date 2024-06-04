
void DoSkinning(
	in VertexInputType input,
    int vertexWeightCount,
	out float4 position,
	out float3 normal,
	out float3 tangent,
	out float3 binormal)
{
    // the matrices used for this vertex' transformation
    float4x4 FramePoseMatrices[4];
    FramePoseMatrices[0] = tranforms[input.BoneIndices.x];
    FramePoseMatrices[1] = tranforms[input.BoneIndices.y];
    FramePoseMatrices[2] = tranforms[input.BoneIndices.z];
    FramePoseMatrices[3] = tranforms[input.BoneIndices.w];

    position = 0;
    normal = 0;
    tangent = 0;
    binormal = 0;
	
	[unroll]
    for (int i = 0; i < vertexWeightCount; i++)
    {
		// transform vertex position
        position +=
			input.Weights[i] * mul(float4(input.position.xyz, 1), FramePoseMatrices[i]);

		// tranform lighting vectors, only use rotation part (3x3) of matrices 
        normal.xyz +=
			input.Weights[i] * mul(input.normal.xyz, (float3x3) FramePoseMatrices[i]);
        tangent.xyz +=
			input.Weights[i] * mul(input.tangent.xyz, (float3x3) FramePoseMatrices[i]);
        binormal.xyz +=
			input.Weights[i] * mul(input.binormal.xyz, (float3x3) FramePoseMatrices[i]);

    }
}