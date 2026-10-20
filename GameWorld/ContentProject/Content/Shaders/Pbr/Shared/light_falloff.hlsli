#ifndef LIGHT_FALLOFF_HLSLI
#define LIGHT_FALLOFF_HLSLI

// A light particle's falloff with d, its distance from the light over its reach (the particle's width), as CA's
// ps_main_deferred_dynamic_light falls off with g_vfx_deferred_dynamic_lights_smooth_falloff set: (1 - d)^2, nothing past
// its reach. The pool on the ground (Particle.fx LightPS) and the models (PointLightAt) both light with it.
float LightFalloff(float d)
{
    float f = saturate(1 - d);
    return f * f;
}

#endif // LIGHT_FALLOFF_HLSLI
