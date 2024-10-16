#ifndef MATHCONSTANTS_HLSLI
#define MATHCONSTANTS_HLSLI


static const float pi = 3.14159265;
static const float one_over_pi = 1 / 3.14159265;
static const float real_approx_zero = 0.001f;

static const float texture_alpha_ref = 0.5f;

static const float Epsilon = 0.00001;


// Constant normal incidence Fresnel factor for all dielectrics.
static const float3 Fdielectric = 0.04;


#endif