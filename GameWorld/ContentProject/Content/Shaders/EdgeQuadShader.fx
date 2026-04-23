float4x4 View;
float4x4 Projection;
float ViewportWidth;
float ViewportHeight;

struct VSInput
{
    float3 Position : POSITION0;
    float3 P0       : POSITION1;
    float3 P1       : POSITION2;
    float3 C0       : COLOR1;
    float3 C1       : COLOR2;
    float  Width    : BLENDWEIGHT0;
};

struct VSOutput
{
    float4 Position : SV_POSITION;
    float3 Color    : COLOR0;
    float  Edge     : TEXCOORD0;
};

float2 WorldToScreen(float3 worldPos)
{
    float4 clip = mul(mul(float4(worldPos, 1), View), Projection);
    float2 ndc  = clip.xy / clip.w;
    return float2((ndc.x * 0.5 + 0.5) * ViewportWidth,
                  (0.5 - ndc.y * 0.5) * ViewportHeight);
}

float WorldToClipW(float3 worldPos)
{
    float4 clip = mul(mul(float4(worldPos, 1), View), Projection);
    return clip.w;
}

float4 ScreenToClip(float2 screen, float w)
{
    float2 ndc = float2(screen.x / ViewportWidth * 2 - 1,
                        1 - screen.y / ViewportHeight * 2);
    return float4(ndc * w, 0, w);
}

VSOutput EdgeQuadVS(VSInput input)
{
    VSOutput output;

    float2 s0 = WorldToScreen(input.P0);
    float2 s1 = WorldToScreen(input.P1);

    float2 dir  = s1 - s0;
    float  len  = length(dir);

    if (len < 0.001)
    {
        dir = float2(1, 0);
        len = 0.001;
    }
    else
    {
        dir /= len;
    }

    float2 perp = float2(-dir.y, dir.x);

    float baseWidth = 1.2;
    float halfW = baseWidth * 0.5 + 0.5;

    float t = input.Position.x;
    float side = input.Position.y;

    float2 screenPos = lerp(s0, s1, t) + perp * side * halfW;

    float w0 = WorldToClipW(input.P0);
    float w1 = WorldToClipW(input.P1);
    float w  = lerp(w0, w1, t);

    float4 clipPos = ScreenToClip(screenPos, w);

    float bias = -0.00005;
    float4 clipP0 = mul(mul(float4(input.P0, 1), View), Projection);
    float4 clipP1 = mul(mul(float4(input.P1, 1), View), Projection);
    float z0 = clipP0.z / clipP0.w;
    float z1 = clipP1.z / clipP1.w;
    float z  = lerp(z0, z1, t) + bias;

    clipPos.z = z * w;

    output.Position = clipPos;
    output.Color = lerp(input.C0, input.C1, t);
    output.Edge = side * 2.0;

    return output;
}

float4 EdgeQuadPS(VSOutput input) : SV_Target
{
    float dist = abs(input.Edge);
    float alpha = 1.0 - smoothstep(0.6, 1.0, dist);
    if (alpha < 0.01)
        discard;

    return float4(input.Color, alpha);
}

technique EdgeQuad
{
    pass P0
    {
        VertexShader = compile vs_4_0 EdgeQuadVS();
        PixelShader  = compile ps_4_0 EdgeQuadPS();
    }
};
