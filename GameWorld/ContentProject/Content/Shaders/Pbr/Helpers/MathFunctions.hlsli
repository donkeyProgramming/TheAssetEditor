#ifndef __MATHFUNF_HLSLI__
#define __MATHFUNF_HLSLI__

#include "../inputlayouts.hlsli"
#include "../TextureSamplers.hlsli"

float nfmod(float a, float b)
{
    return a - b * floor(a / b);
}

//  From http://en.wikipedia.org/wiki/SRGB                          //
//                                                                  //
//  It seems that gamma to linear and back again is not a simple    //
//  pow function.  The functions above are just a simplification    //
//  of what the spec is and what the hardware is doing, which is    //
//  following this spec.                                            //
//////////////////////////////////////////////////////////////////////
float gamma_accurate_component(in const float linear_val)
{
    const float srgb_gamma_ramp_inflection_point = 0.0031308f;

    if (linear_val <= srgb_gamma_ramp_inflection_point)
    {
        return 12.92f * linear_val;
    }
    else
    {
        const float a = 0.055f;

        return ((1.0f + a) * pow(linear_val, 1.0f / 2.4f)) - a;
    }
}

float3 gamma_accurate(in const float3 vLinear)
{
    return float3(gamma_accurate_component(vLinear.r), gamma_accurate_component(vLinear.g), gamma_accurate_component(vLinear.b));
}


float3 _gamma(in float3 vLinear)
{
    return gamma_accurate(vLinear);
}

float _gamma(in float fLinear)
{
    return gamma_accurate_component(fLinear);
}


float linear_accurate_component(in const float srgb_val)
{
    const float inflection_point = 0.04045;

    if (srgb_val <= inflection_point)
    {
        return srgb_val / 12.92f;
    }
    else
    {
        const float a = 0.055f;

        return pow((srgb_val + a) / (1.0f + a), 2.4f);
    }
}

float3 linear_accurate(in const float3 fGamma)
{
    return float3(linear_accurate_component(fGamma.r), linear_accurate_component(fGamma.g), linear_accurate_component(fGamma.b));
}

float _linear(in float fGamma)
{
    return linear_accurate_component(fGamma);
}

float3 _linear(in float3 vGamma)
{
    return linear_accurate(vGamma);
}

float luma(in float3 gamma_colour)
{
    const float3 sc_luma = float3(0.2126, 0.7152, 0.0722);

    return dot(gamma_colour, sc_luma);
}

float3 GetPixelNormal(PixelInputType input)
{
	//float3x3 basis = float3x3(normalize(input.tangent), normalize(input.normal), normalize(input.binormal));      // TODO: some games have this, sometimes enables, sometimes as as comment,
    float3x3 basis = float3x3(normalize(input.tangent.xyz), normalize(input.binormal.xyz), normalize(input.normal.xyz)); // works in own shader

    float4 NormalTex = NormalTexture.Sample(s_normal, input.tex);

    // -------------------------------------------------------------------------------------------------------------------
    // decode the "orange 2d normal map", into a standard "blue" 3d normal map using an othogonal projection
    // -------------------------------------------------------------------------------------------------------------------
    float3 deQuantN;
    deQuantN.x = NormalTex.r * NormalTex.a;
    deQuantN.y = 1.0 - NormalTex.g;
    
    deQuantN = (deQuantN * 2.0f) - 1.0f;
    deQuantN.z = sqrt(1 - deQuantN.x * deQuantN.x - deQuantN.y * deQuantN.y);
    	
    return normalize(mul(normalize(deQuantN.xyz), basis));
}

#endif