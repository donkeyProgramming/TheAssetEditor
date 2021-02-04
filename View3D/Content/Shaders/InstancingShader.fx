float4x4 WVP;

struct InstancingVSinput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

struct InstancingVSoutput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

InstancingVSoutput InstancingVS(InstancingVSinput input, float4 instanceTransform : POSITION1,
    float atlasCoord : POSITION2)
{
    InstancingVSoutput output;

    float fac = atlasCoord;
    float4x4 scale4x4 = float4x4(
        fac, 0, 0, 0,
        0, fac, 0, 0,
        0, 0, fac, 0,
        0, 0, 0, 1);

    float4 pos = (input.Position + instanceTransform);
    pos = mul(pos, scale4x4);
    pos = mul(pos, WVP);

    output.Position = pos;
    return output;
}

float4 InstancingPS(InstancingVSoutput input) : COLOR0
{
    return float4(1,0,0,1);
}

technique Instancing
{
    pass Pass0
    {
        VertexShader = compile vs_4_0 InstancingVS();
        PixelShader = compile ps_4_0 InstancingPS();
    }
}