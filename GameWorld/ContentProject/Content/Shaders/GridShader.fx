////////////////////////////////////////////////////////////////////////////////////////////////////////////
//  Procedural infinite grid shader
//  Renders a ground-plane quad with analytically anti-aliased grid lines using
//  frac() + fwidth() + smoothstep(). This produces perfect AA at any zoom/angle.
//  Based on Blender's overlay_grid approach adapted for MonoGame HLSL.
////////////////////////////////////////////////////////////////////////////////////////////////////////////

#if OPENGL
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0
#define PS_SHADERMODEL ps_4_0
#endif

float4x4 World;
float4x4 View;
float4x4 Projection;
float3 CameraPosition;
float3 GridColor;
float  CameraDistance;
int    IsOrthographic;

////////////////////////////////////////////////////////////////////////////////////////////////////////////
//  STRUCTS
////////////////////////////////////////////////////////////////////////////////////////////////////////////

struct VertexShaderInput
{
    float3 Position : POSITION0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float3 WorldPos : TEXCOORD0;
    float  ViewDist : TEXCOORD1;
};

////////////////////////////////////////////////////////////////////////////////////////////////////////////
//  VERTEX SHADER
////////////////////////////////////////////////////////////////////////////////////////////////////////////

VertexShaderOutput GridVS(VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;

    float3 worldPos = input.Position;
    float4 viewPos = mul(float4(worldPos, 1.0), View);
    output.Position = mul(viewPos, Projection);
    output.WorldPos = worldPos;
    output.ViewDist = length(viewPos.xyz);

    return output;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////
//  PIXEL SHADER - Procedural grid with analytical anti-aliasing
////////////////////////////////////////////////////////////////////////////////////////////////////////////

float4 GridPS(VertexShaderOutput input) : COLOR0
{
    float2 coord = input.WorldPos.xz;

    // Screen-space derivatives for automatic LOD / anti-aliasing
    float2 dv = fwidth(coord);
    float2 dvHalf = dv * 0.5;

    // --- Fine grid (every 1 unit) ---
    float2 gridFrac = abs(frac(coord - 0.5) - 0.5);
    float2 fineLineSmooth = smoothstep(dvHalf, dvHalf * 2.0, gridFrac);
    float fineLine = 1.0 - min(fineLineSmooth.x, fineLineSmooth.y);

    // --- Emphasis grid (every 5 units) ---
    float2 coord5 = coord * 0.2;  // coord / 5.0
    float2 dv5 = fwidth(coord5);
    float2 dv5Half = dv5 * 0.5;
    float2 emphasisFrac = abs(frac(coord5 - 0.5) - 0.5);
    float2 emphasisSmooth = smoothstep(dv5Half, dv5Half * 2.0, emphasisFrac);
    float emphasisLine = 1.0 - min(emphasisSmooth.x, emphasisSmooth.y);

    // --- Axis indicators ---
    float xAxisLine = 1.0 - smoothstep(dvHalf.y, dv.y, abs(coord.y));
    float zAxisLine = 1.0 - smoothstep(dvHalf.x, dv.x, abs(coord.x));

    // --- Distance fadeout (proportional to camera distance) ---
    // Wide, gradual fade for natural appearance (Blender style)
    float fadeStart = CameraDistance * 0.3;
    float fadeEnd = CameraDistance * 4.5;
    float dist = length(input.WorldPos.xz - CameraPosition.xz);
    float distFade = 1.0 - smoothstep(fadeStart, fadeEnd, dist);
    distFade = pow(distFade, 0.6);  // Soften curve for more gradual falloff

    // --- Angle fadeout (grid fades when viewed nearly edge-on) ---
    // Softer threshold: only fade when angle is < ~8 degrees from horizontal (0.02)
    // instead of original < ~3 degrees (0.05). This keeps grid visible when camera
    // is slightly below ground plane (Y ≈ 0), common after model-focused positioning.
    float3 viewDir = normalize(CameraPosition - input.WorldPos);
    float angleFade = smoothstep(0.02, 0.15, abs(viewDir.y));

    // --- Compose final color and alpha ---
    float combinedFade = distFade * angleFade;

    // Fine grid: subtle
    float fineAlpha = fineLine * 0.25 * combinedFade;

    // Emphasis grid: stronger, uses brighter color
    float emphasisAlpha = emphasisLine * 0.5 * combinedFade;

    // Axes: most prominent with dedicated colors
    float xAxisAlpha = xAxisLine * 0.8 * combinedFade;
    float zAxisAlpha = zAxisLine * 0.8 * combinedFade;

    // Pick the dominant contribution
    float alpha = fineAlpha;
    float3 color = GridColor;

    // Emphasis overrides fine
    if (emphasisAlpha > alpha)
    {
        alpha = emphasisAlpha;
        color = GridColor * 1.6; // brighter for emphasis lines
    }

    // X axis = red
    if (xAxisAlpha > alpha)
    {
        alpha = xAxisAlpha;
        color = float3(0.9, 0.3, 0.3);
    }

    // Z axis = blue
    if (zAxisAlpha > alpha)
    {
        alpha = zAxisAlpha;
        color = float3(0.3, 0.5, 0.9);
    }

    // Discard fully transparent pixels for early-Z
    if (alpha < 0.001)
        discard;

    return float4(color, alpha);
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////
//  TECHNIQUES
////////////////////////////////////////////////////////////////////////////////////////////////////////////

technique Grid
{
    pass Pass0
    {
        VertexShader = compile VS_SHADERMODEL GridVS();
        PixelShader = compile PS_SHADERMODEL GridPS();
    }
}
