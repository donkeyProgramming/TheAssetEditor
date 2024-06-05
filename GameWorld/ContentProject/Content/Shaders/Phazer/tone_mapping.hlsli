/*

This shader experiments the effect of different tone mapping operators.
This is still a work in progress.

More info:
http://slideshare.net/ozlael/hable-john-uncharted2-hdr-lighting
http://filmicgames.com/archives/75
http://filmicgames.com/archives/183
http://filmicgames.com/archives/190
http://imdoingitwrong.wordpress.com/2010/08/19/why-reinhard-desaturates-my-blacks-3/
http://mynameismjp.wordpress.com/2010/04/30/a-closer-look-at-tone-mapping/
http://renderwonk.com/publications/s2010-color-course/

--
Zavie

*/

//static const float gamma = 2.2;
static const float gamma = 1;

float3 getBaseColor(int i)
{
    if (i == 0)
        return float3(1.0, 0.4, 0.0);
    if (i == 1)
        return float3(0.4, 1.0, 0.0);
    if (i == 2)
        return float3(0.0, 1.0, 0.4);
    if (i == 3)
        return float3(0.0, 0.4, 1.0);
    if (i == 4)
        return float3(0.4, 0.0, 1.0);
    if (i == 5)
        return float3(1.0, 0.0, 0.4);

    return (1.);
}

float3 getBaseColor()
{
    float colorPerSecond = 0.5;
    int i = int(fmod(colorPerSecond * 1, 7.));
    int j = int(fmod(float(i) + 1., 7.));

    return lerp(getBaseColor(i), getBaseColor(j), frac(colorPerSecond * 1));
}

float3 linearToneMapping(float3 color)
{
    float exposure = 1.;
    color = clamp(exposure * color, 0., 1.);
    color = pow(color, (1. / gamma));
    return color;
}

float3 simpleReinhardToneMapping(float3 color)
{
    float exposure = 1.5;
    color *= exposure / (1. + color / exposure);
    color = pow(color, (1. / gamma));
    return color;
}

float3 lumaBasedReinhardToneMapping(float3 color)
{
    float luma = dot(color, float3(0.2126, 0.7152, 0.0722));
    float toneMappedLuma = luma / (1. + luma);
    color *= toneMappedLuma / luma;
    color = pow(color, (1. / gamma));
    return color;
}

float3 whitePreservingLumaBasedReinhardToneMapping(float3 color)
{
    float white = 2.;
    float luma = dot(color, float3(0.2126, 0.7152, 0.0722));
    float toneMappedLuma = luma * (1. + luma / (white * white)) / (1. + luma);
    color *= toneMappedLuma / luma;
    color = pow(color, (1. / gamma));
    return color;
}

float3 RomBinDaHouseToneMapping(float3 color)
{
    color = exp(-1.0 / (2.72 * color + 0.15));
    color = pow(color, (1. / gamma));
    return color;
}

float3 filmicToneMapping(float3 color)
{
    color = max((0.), color - (0.004));
    color = (color * (6.2 * color + .5)) / (color * (6.2 * color + 1.7) + 0.06);
    return color;
}

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

