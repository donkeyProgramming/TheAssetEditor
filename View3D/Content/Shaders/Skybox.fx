#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif


//Exception levée : 'SharpDX.SharpDXException' dans SharpDX.dll
//Warning: Vertex shader uses semantic 'SV_Position' for input register. Please update the shader and use the semantic 'POSITION0' instead. //The semantic 'SV_Position' should only be used for the vertex shader output or pixel shader input!

float4x4 World;
float4x4 View;
float4x4 Projection;
 
float3 CameraPosition;
 
Texture SkyBoxTexture; 
samplerCUBE SkyBoxSampler = sampler_state 
{ 
   texture = <SkyBoxTexture>; 
   magfilter = LINEAR; 
   minfilter = LINEAR; 
   mipfilter = LINEAR; 
   AddressU = Mirror; 
   AddressV = Mirror; 
};

struct VertexShaderInput
{
    float4 Position : POSITION0;
};
 
struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float3 TextureCoordinate : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;
 
    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);
 
    float4 VertexPosition = mul(input.Position, World);
    output.TextureCoordinate = VertexPosition - CameraPosition;
 
    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR
{
    float4 pixel =  texCUBE(SkyBoxSampler, normalize(input.TextureCoordinate));
    pixel.w = 1;
return pixel;
    //return float4(1, 0, 0, 1);
}

technique Skybox
{
    pass Pass1
    {
        VertexShader = compile VS_SHADERMODEL VertexShaderFunction();
        PixelShader = compile PS_SHADERMODEL PixelShaderFunction();
    }
}