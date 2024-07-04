#ifndef TEXTURESAMPLER_HLSLI
#define TEXTURESAMPLER_HLSLI
                           
float ModelRenderScale = 1;
SamplerState SampleType
{
	//Texture = <tex_cube_specular>;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
    Mipfilter = LINEAR;
    Filter = Anisotropic;
    MaxAnisotropy = 16;
    AddressU = Wrap;
    AddressV = Wrap;
};

SamplerState spBRDF_Sampler
{
	//Texture = <tex_cube_specular>;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
    Mipfilter = LINEAR;
    Filter = LINEAR;
    MaxAnisotropy = 16;
    AddressU = Clamp;
    AddressV = Clamp;
};

SamplerState s_normal
{
	//Texture = <tex_cube_specular>;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
    Mipfilter = LINEAR;
    Filter = LINEAR;
    MaxAnisotropy = 16;
    AddressU = Wrap;
    AddressV = Wrap;
};

#endif // TEXTURESAMPLER_HLSLI