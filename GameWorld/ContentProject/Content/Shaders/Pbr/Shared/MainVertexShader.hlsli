#ifndef MAINVERTEXSHADER_HLSLI
#define MAINVERTEXSHADER_HLSLI

#include "../Shared/const_layout.hlsli"
#include "../inputlayouts.hlsli"
#include "../Capabilites/Animation.hlsli"

// **************************************************************************************************************************************
// *		VERTEX SHADER CODE
// **************************************************************************************************************************************
PixelInputType MainVertexShader(in VertexInputType input)
{
    PixelInputType output = (PixelInputType) 0;

    DoSkinning(input, output.position, output.normal, output.tangent, output.binormal);

    output.position = mul(output.position, World);
    output.worldPosition = output.position.xyz;
    output.position = mul(output.position, View);
    output.position = mul(output.position, Projection);

    output.tex = input.tex;

    output.normal = normalize(mul(output.normal, (float3x3) World));
    output.tangent = normalize(mul(output.tangent, (float3x3) World));
    output.binormal = normalize(mul(output.binormal, (float3x3) World));

	// Calculate the position of the vertex in the world.
    output.viewDirection = normalize(CameraPos - output.worldPosition);
    return output;
}

#endif