#ifndef TONE_MAPPING_HLSLI
#define TONE_MAPPING_HLSLI

static const float gamma = 1;

float3 Uncharted2ToneMapping(float3 color)
{
    float A = 0.15;
    float B = 0.50;
    float C = 0.10;
    float D = 0.20;
    float E = 0.02;
    float F = 0.30;
    float W = 11.2;
    float exposure = 2.;
    color *= exposure;
    color = ((color * (A * color + C * B) + D * E) / (color * (A * color + B) + D * F)) - E / F;
    float white = ((W * (A * W + C * B) + D * E) / (W * (A * W + B) + D * F)) - E / F;
    color /= white;
    color = pow(color, (1. / gamma));
    return color;
}

float4 DoToneMapping(float3 hdrColor, float exposureFactor = 1.0, float gamma_value = 2.2)
{
    float exposure = 2.0;
    float3 hdrColorExposed = hdrColor.rgb * exposure * exposureFactor;
    float3 toneMappedColor = Uncharted2ToneMapping(hdrColorExposed);
    float3 gammmaCorrectedColor = pow(toneMappedColor, 1.0 / gamma_value);

    return float4(gammmaCorrectedColor.rgb, 1);
}

#endif // TONE_MAPPING_HLSLI