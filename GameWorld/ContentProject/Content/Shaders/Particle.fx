#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0
	#define PS_SHADERMODEL ps_4_0
#endif

// The VFX preview's particles, drawn by GameWorld.Core/Vfx/ParticleRenderItem.cs. Every reading of a CA fragment here
// is a guess, like VfxConventions' own; ParticleRenderItemTests draw each one on the GPU. ps_lighting
// is not a guess: it is CA's own ps_main (fx/particle_vfx_main in shaders.pack), disassembled.

#include "Pbr/Helpers/tone_mapping.hlsli"
#include "Pbr/Shared/light_falloff.hlsli"

float4x4 WorldViewProjection;
// The view depth comes off it, which a clip w gives only in perspective.
float4x4 WorldView;
float4x4 World;
// A vertex colour stops at 1: an HDR emitter's colours arrive divided by Scale and are multiplied back here.
float Scale = 1;
float Premultiplied = 1;
// ps_texture overlay_colour: the vertex colour comes straight and is overlaid on the texture; the chain's additive brightness
// and alpha weight (VfxConventions.Blend) go on after.
float Overlay = 0;
float OverlayBrightness = 1;
float OverlayAlphaWeight = 1;
// The glow pass, as CA's bloom bright pass: what a particle draws, weighed by how far the luminance of what it adds to the
// scene lies over the threshold, times the particles' strength (VfxConventions.BloomThreshold).
float Glow = 0;
float BloomThreshold = 4;
float BloomStrength = 0.5;
// ps_alpha_crush sharp_transition.
float CrushSharp = 0;
// ps_gradient: whether the chain has one, its alpha_mapping, and its strip at 16 points from 0 to 1.
float UseGradient = 0;
float GradientMapsAlpha = 0;
float4 GradientRamp[16];

Texture2D Texture;
sampler2D TextureSampler = sampler_state
{
	Texture = <Texture>;
};

// DISTORTION: the scene drawn so far, copied, its texel size, and the share of the view a full shift moves it, in texture
// units across and down (VfxConventions.DistortionShare).
Texture2D Scene;
sampler2D SceneSampler = sampler_state
{
	Texture = <Scene>;
	AddressU = Clamp;
	AddressV = Clamp;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Point;
};
float2 InverseSceneSize;
float2 Reach;

// ps_soft_edge against the solid scene: its view depth per pixel, drawn by the engine before the particles, that texture's
// texel size, and the emitter's soft_factor and scale_strength. UseSceneDepth is 0 when there is none to fade against.
// Occlude: the glow pass, whose target holds only the emissive models' depth, so the scene's depth hides what lies behind it.
Texture2D SceneDepth;
sampler2D SceneDepthSampler = sampler_state
{
	Texture = <SceneDepth>;
	AddressU = Clamp;
	AddressV = Clamp;
	MinFilter = Point;
	MagFilter = Point;
	MipFilter = Point;
};
float2 InverseDepthSize;
float UseSceneDepth = 0;
float Occlude = 0;
float SoftFactor = 1;
float SoftStrength = 1;

// ps_lighting on a chain blended by alpha (EmitterSettings.Lit), in the world: the way to the sun, the light a face square to
// it takes and the ambient cube's scale, both as Shaders/Pbr light a model; the camera's axis toward the camera; and the
// chain's small_particles, large_particles, directionality and emissive (xyzw) and average_size.
float Lit = 0;
float3 SunDirection;
float3 SunColour;
float3 AmbientColour;
float3 CameraBack;
float4 Scattering;
float AverageSize;
TextureCube AmbientCube;
samplerCUBE AmbientSampler = sampler_state
{
	Texture = <AmbientCube>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
};
// ps_normal: the chain's normal map, which shapes that light, and whether there is one.
float UseNormalMap = 0;
Texture2D NormalMap;
sampler2D NormalSampler = sampler_state
{
	Texture = <NormalMap>;
};

struct VertexShaderInput
{
	float4 Position : POSITION0;
	float4 Colour : COLOR0;
	float2 Uv : TEXCOORD0;
	// The next flipbook cell's texture coordinate (xy), how far to blend into it (z), the alpha crush level (w).
	float4 Extra : TEXCOORD1;
	// The particle's width and height, metres.
	float2 Size : TEXCOORD2;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Colour : COLOR0;
	float2 Uv : TEXCOORD0;
	float4 Extra : TEXCOORD1;
	// The view depth, for ps_soft_edge.
	float Depth : TEXCOORD2;
	float3 WorldPosition : TEXCOORD3;
	float2 Size : TEXCOORD4;
};

// sRGB to linear, as CA's ps_main decodes a texture's alpha, and back.
float Linear(float x)
{
	return x <= 0.04045 ? x / 12.92 : pow((x + 0.055) / 1.055, 2.4);
}

float3 Linear(float3 x)
{
	return float3(Linear(x.r), Linear(x.g), Linear(x.b));
}

float Gamma(float x)
{
	return x <= 0.0031308 ? x * 12.92 : 1.055 * pow(x, 1 / 2.4) - 0.055;
}

float3 Gamma(float3 x)
{
	return float3(Gamma(x.r), Gamma(x.g), Gamma(x.b));
}

// CA's process_texture_colour calls blend_overlay(texture, colour), which no shipped CA source defines: this is the standard
// overlay, with the texture as the base.
float3 BlendOverlay(float3 base, float3 blend)
{
	return lerp(2 * base * blend, 1 - 2 * (1 - base) * (1 - blend), step(0.5, base));
}

// The quad's own plane, on the side facing the camera, as CA's billboard faces it.
float3 Facing(float3 worldPosition)
{
	float3 n = normalize(cross(ddx(worldPosition), ddy(worldPosition)));
	return dot(n, CameraBack) < 0 ? -n : n;
}

// ps_normal, as CA's ps_main reads a DXT5nm texel: x in alpha and y in green, each half a step up, z what is left.
float3 TangentNormal(float2 uv)
{
	float2 xy = (tex2D(NormalSampler, uv).ag + 0.001961) * 2 - 1;
	return float3(xy, sqrt(max(1 - dot(xy, xy), 0)));
}

// The normal the light meets: the quad's plane, or with ps_normal its map's, x along the texture's u and y along its v (down).
// The u and v ways come off the screen derivatives (a cotangent frame), so a mirrored or turned texture turns its normals with it.
float3 Normal(VertexShaderOutput input)
{
	float3 n = Facing(input.WorldPosition);
	if (UseNormalMap <= 0)
		return n;
	float3 t = lerp(TangentNormal(input.Uv), TangentNormal(input.Extra.xy), input.Extra.z);
	float3 dp1 = ddx(input.WorldPosition);
	float3 dp2 = ddy(input.WorldPosition);
	float2 duv1 = ddx(input.Uv);
	float2 duv2 = ddy(input.Uv);
	float3 across = cross(dp2, n);
	float3 down = cross(n, dp1);
	float s = sign(dot(n, cross(dp1, dp2)));
	float3 u = normalize(across * duv1.x + down * duv2.x) * s;
	float3 v = normalize(across * duv1.y + down * duv2.y) * s;
	return normalize(t.x * u + t.y * v + t.z * n);
}

// ps_lighting, as CA's ps_main computes it for a particle of this width with this normal: sunlight scattered through it,
// Rayleigh's phase for small_particles and Henyey-Greenstein's, (1 - g)^2 over it, for large_particles at g = directionality,
// each weighted by its own amount, over the light let straight through, exp(-(small + large) x average_size x width x
// (1 - N.L) / 2); then drawn toward emissive by as much as emissive is, up to all of it; plus the sky's light from the
// normal's way. No particles to scatter is 0 / 0 in CA's, which the GPU saturates to no sun at all.
float3 ParticleLight(float3 n, float width)
{
	float small = Scattering.x;
	float large = Scattering.y;
	float g = Scattering.z;
	float sigma = small + large;
	float c = dot(-SunDirection, CameraBack);
	float through = exp(-sigma * AverageSize * width * (1 - dot(n, SunDirection)) * 0.5);
	float rayleigh = 0.059683 * (1 + c * c);
	float henyey = (1 - g) * (1 - g) * 0.079577 / pow(max(1 + g * g - 2 * g * c, 0.000001), 1.5);
	float sun = sigma > 0 ? saturate((small * rayleigh + large * henyey) / sigma * (1 - through) + through) : 0;
	return lerp(sun * SunColour, Scattering.www, saturate(Scattering.w)) + texCUBE(AmbientSampler, n).rgb * AmbientColour;
}

// Straight alpha adds its colour times its alpha, premultiplied its colour as drawn (ParticleRenderItem.BlendFor).
float4 Bright(float4 drawn)
{
	float3 added = Premultiplied > 0 ? drawn.rgb : drawn.rgb * drawn.a;
	if (Glow > 0)
		drawn.rgb *= saturate(dot(added, float3(0.2126, 0.7152, 0.0722)) - BloomThreshold) * BloomStrength;
	return drawn;
}

float4 Ramp(float t)
{
	float x = saturate(t) * 15;
	float i = min(floor(x), 14);
	return lerp(GradientRamp[(int)i], GradientRamp[(int)i + 1], x - i);
}

VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output;
	output.Position = mul(input.Position, WorldViewProjection);
	output.Colour = input.Colour * float4(Scale, Scale, Scale, 1);
	output.Uv = input.Uv;
	output.Extra = input.Extra;
	output.Depth = -mul(input.Position, WorldView).z;
	output.WorldPosition = mul(input.Position, World).xyz;
	output.Size = input.Size;
	return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	// vs_animated_texture cell_blend: this cell mixed into the next (VfxConventions.FlipbookFrame).
	float4 texel = lerp(tex2D(TextureSampler, input.Uv), tex2D(TextureSampler, input.Extra.xy), input.Extra.z);

	// The rest works on the straight colour: a premultiplied texel is divided out first and multiplied back after.
	float a = texel.a;
	float3 rgb = Premultiplied > 0 && a > 0 ? texel.rgb / a : texel.rgb;
	// ps_texture: CA reads the alpha as sRGB, as the hardware reads the colour off its BC7_UNORM_SRGB files, so a texel covers
	// its alpha linearised. The colour stays as stored: this shader works in the screen's gamma.
	a = Linear(a);

	// ps_gradient, read as a gradient map: the texel's red picks a colour off the strip. Its alpha is multiplied by the
	// strip's there, or with alpha_mapping is put through the strip's alpha itself.
	if (UseGradient > 0)
	{
		float4 picked = Ramp(rgb.r);
		rgb = picked.rgb;
		a = GradientMapsAlpha > 0 ? Ramp(a).a : a * picked.a;
	}

	// ps_alpha_crush: alpha under the particle's crush level goes. A sharp crush cuts there and keeps what is over; a soft
	// one stretches what is left back over 0..1, so the texture thins from its faint edges in.
	float crush = input.Extra.w;
	a = CrushSharp > 0 ? (a > crush ? a : 0) : saturate((a - crush) / max(1 - crush, 0.0001));

	// ps_soft_edge, as CA's ps_main reads it: fades in, linearly, over soft_factor metres of view depth in front of the solid
	// scene, shrunk for a particle under a metre across by scale_strength, as VfxConventions.SoftDistance has it in front of the
	// ground.
	if (UseSceneDepth > 0 || Occlude > 0)
	{
		float scene = tex2D(SceneDepthSampler, input.Position.xy * InverseDepthSize).r;
		if (UseSceneDepth > 0)
			a *= saturate((scene - input.Depth) / (SoftFactor * lerp(1, saturate(min(input.Size.x, input.Size.y)), SoftStrength) + 0.001));
		if (Occlude > 0 && scene < input.Depth)
			a = 0;
	}

	// ps_texture overlay_colour, as CA's process_texture_colour has it: the colour is overlaid on the texture, in linear as CA's
	// colours are, rather than multiplied in; the alpha still multiplies. Lit, the overlaid colour is the albedo (a lit chain has
	// no additive blend, so no brightness or alpha weight).
	if (Overlay > 0)
	{
		float3 overlaid = BlendOverlay(UseGradient > 0 ? rgb : saturate(Linear(rgb)), input.Colour.rgb);
		rgb = Lit > 0
			? Gamma(saturate(Uncharted2ToneMapping(overlaid * ParticleLight(Normal(input), input.Size.x))))
			: Gamma(saturate(overlaid)) * OverlayBrightness;
		a *= input.Colour.a;
		return Bright(float4(Premultiplied > 0 ? rgb * a : rgb, Premultiplied > 0 && Lit == 0 ? a * OverlayAlphaWeight : a));
	}

	// ps_lighting: the colour is linear, the texture's decoded (a gradient's strip is linear already), times the vertex colour,
	// lit, and tone-mapped and encoded as Shaders/Pbr bring a model's lit colour to the screen. The vertex colour comes straight.
	if (Lit > 0)
	{
		float3 albedo = (UseGradient > 0 ? rgb : saturate(Linear(rgb))) * input.Colour.rgb;
		rgb = Gamma(saturate(Uncharted2ToneMapping(albedo * ParticleLight(Normal(input), input.Size.x))));
		a *= input.Colour.a;
		return Bright(float4(Premultiplied > 0 ? rgb * a : rgb, a));
	}

	return Bright(float4(Premultiplied > 0 ? rgb * a : rgb, a) * input.Colour);
}

struct DistortionOutput
{
	float4 Position : SV_POSITION;
	float4 Colour : COLOR0;
	float2 Uv : TEXCOORD0;
};

DistortionOutput DistortionVS(in VertexShaderInput input)
{
	DistortionOutput output;
	output.Position = mul(input.Position, WorldViewProjection);
	output.Colour = input.Colour;
	output.Uv = input.Uv;
	return output;
}

// CA's ps_distortion_alphablend and fx_hdr_to_screen: the map is DXT5nm, x in alpha and y in green (red is unused, and 255 in
// shockwave_normal), made unit with z rebuilt. The particle's alpha times the tilt is its cover, and the pixel under it is read
// from Reach x cover^2 x tilt away along the tilt. Written back whole, alpha too, so the scene moves and leaves no copy behind;
// y is added down the screen as CA adds it. One particle's shift only; CA sums overlapping ones in its buffer first.
float4 DistortionPS(DistortionOutput input) : COLOR
{
	float2 xy = tex2D(TextureSampler, input.Uv).ag * 2 - 1;
	float3 n = normalize(float3(xy, sqrt(max(1 - dot(xy, xy), 0))));
	float cover = saturate(input.Colour.a) * length(n.xy);
	float2 offset = n.xy * cover * cover * Reach;
	return tex2D(SceneSampler, input.Position.xy * InverseSceneSize + offset);
}

// DEFERRED_DYNAMIC_LIGHT, as the light it casts on the ground: ParticleRenderItem.DrawGroundLight draws one quad over the ground
// the scene's light particles reach (PointLights, the engine's list of this frame's), and each pixel adds up all of them, as CA
// adds up its deferred lights before its one tone map. Colours are each light's times its alpha at CA's share of the sun;
// LightScale brings them to the light a ground of VfxConventions.GroundAlbedo takes, on the models' sun scale, which goes to
// the screen through their tone map, so the ground reads as lit and not as paint laid on.
float LightScale = 0;
// As many as CommonShaderParameters.MaxPointLights.
float4 PointLights[64];
float4 PointLightColours[64];
int PointLightCount = 0;

// A light of this colour at light.xyz, reaching light.w, on the ground at position.
float3 OnGround(float4 light, float3 colour, float3 position)
{
	float3 to = light.xyz - position;
	float d = length(to);
	return colour * saturate(to.y / max(d, 0.0001)) * LightFalloff(d / max(light.w, 0.0001));
}

float4 LightPS(VertexShaderOutput input) : COLOR
{
	float3 all = 0;
	for (int i = 0; i < PointLightCount; i++)
		all += OnGround(PointLights[i], PointLightColours[i].rgb, input.WorldPosition);
	return float4(Gamma(saturate(Uncharted2ToneMapping(all * LightScale))), 1);
}

technique ParticleDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};

technique Distortion
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL DistortionVS();
		PixelShader = compile PS_SHADERMODEL DistortionPS();
	}
};

technique Light
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL LightPS();
	}
};
