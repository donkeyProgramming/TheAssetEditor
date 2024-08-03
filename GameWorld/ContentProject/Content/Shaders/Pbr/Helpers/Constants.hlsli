#ifndef CONSTANTS_HLSLI
#define CONSTANTS_HLSLI

// Direction for directional lighting source
static const float3 light_Direction_Constant = normalize(float3(0.1, 0.1, 1.f));

// directional light strength, dial from 0 - X
static const float Directional_Light_Raddiance = 5.0f;

// ambient light strength
static const float Ambient_Light_Raddiance = 1.0f;

// colors for lighting, set a color almost white, with a TINY blue tint to change mood in scene
// typically these two are simply te same color, unless one wanted to simular artifical light (like a lamp/flashlight etc)
static const float3 Directional_Light_color = float3(1, 1, 1);
static const float3 Ambient_Light_color = float3(1, 1, 1);

#endif