using System.Collections.Generic;
using Shared.GameFormats.RigidModel.Types;

namespace GameWorld.Core.Rendering.Materials.Capabilities.Utility
{
    public static class ShaderParameterHelper
    {
        static public readonly Dictionary<TextureType, string> TextureTypeToParamName = new()
        {
            { TextureType.Diffuse, "DiffuseTexture"},
            { TextureType.Specular, "SpecularTexture"},
            { TextureType.Gloss, "GlossTexture"},
            { TextureType.Mask, "MaskTexture"},
            { TextureType.MaterialMap, "GlossTexture"},
            { TextureType.BaseColour, "DiffuseTexture"},
            { TextureType.Normal, "NormalTexture"},
            { TextureType.Emissive, "Emissive_Texture"}
        };

        static public readonly Dictionary<TextureType, string> UseTextureTypeToParamName = new()
        {
            { TextureType.Diffuse, "UseDiffuse"},
            { TextureType.Specular, "UseSpecular"},
            { TextureType.Gloss, "UseGloss"},
            { TextureType.Mask, "UseMask"},
            { TextureType.MaterialMap, "UseGloss"},
            { TextureType.BaseColour, "UseDiffuse"},
            { TextureType.Normal, "UseNormal"},
            { TextureType.Emissive, "Emissive_UseTexture"}
        };
    }

}
