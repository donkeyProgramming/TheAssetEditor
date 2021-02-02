#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D SpriteTexture;
bool OnlyRed = false;
bool OnlyGreen = false;
bool OnlyBlue = false;
bool ApplyOnlyAlpha = false;


sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TextureCoordinates : TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float4 texel = tex2D(SpriteTextureSampler,input.TextureCoordinates);

	float4 output = float4(0,0,0,0);
	if (ApplyOnlyAlpha)
	{
		output.xyz = 1.0f - texel.w;
		output.a = 1;
	}
	else
	{
		output.a = 1;

		if (OnlyRed)
			output.x = texel.x;
		else if (OnlyGreen)
			output.y = texel.y;
		else if (OnlyBlue)
			output.z = texel.z;
		else
			output = texel;
	}
	return output;
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};