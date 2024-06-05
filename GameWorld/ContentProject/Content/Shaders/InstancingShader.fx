float4x4 WVP;
float3 VertexColour;

struct VSInstanceInputSimple
{
    float3 InstancePosition : POSITION1;
    float3 InstanceForward : NORMAL1;
    float3 InstanceUp : NORMAL2;
    float3 InstanceLeft : NORMAL3;

    float3 Colour : Normal4;

    //float InstanceTimeOrId : BLENDWEIGHT0;
};

struct InstancingVSinput
{
    float4 Position : POSITION0;
};

struct InstancingVSoutput
{
    float4 Position : POSITION0;
    float3 Colour: Normal4;
};

InstancingVSoutput InstancingVS(InstancingVSinput input, VSInstanceInputSimple instanceInput)
{
    InstancingVSoutput output;

    //float fac = atlasCoord;
    //float4x4 scale4x4 = float4x4(
    //    fac, 0, 0, 0,
    //    0, fac, 0, 0,
    //    0, 0, fac, 0,
    //    0, 0, 0, 1);
    //
    //float4 pos = (input.Position + instanceTransform);
    //pos = mul(pos, scale4x4);
    //pos = mul(pos, WVP);
    //
    //output.Position = pos;
    float3 instancePosition = instanceInput.InstancePosition;
    float4x4 world;
    world[0] = float4(instanceInput.InstancePosition, 0.0f);
    world[1] = float4(instanceInput.InstanceForward, 0.0f);
    world[2] = float4(instanceInput.InstanceUp, 0.0f);
    world[3] = float4(instanceInput.InstanceLeft, 1.0f); // <- i may need to zero this out first lets see.
    // here is the tricky part the intention is to put this into the proper wvp position in one shot.
    // however i might have to mult the world without translation.
    // then translate the vertexposition by the instanceposition and multiply.
    float4 worldViewProjection = mul(input.Position, world);
    float4 posVert = mul(worldViewProjection, WVP);
    output.Position = posVert;
    output.Colour = instanceInput.Colour;

    return output;
}

float4 InstancingPS(InstancingVSoutput input) : COLOR0
{
    return float4(input.Colour,1);
}

technique Instancing
{
    pass Pass0
    {
        VertexShader = compile vs_4_0 InstancingVS();
        PixelShader = compile ps_4_0 InstancingPS();
    }
}