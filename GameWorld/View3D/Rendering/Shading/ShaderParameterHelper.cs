using System.Collections.Generic;
using GameWorld.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel.Types;

namespace GameWorld.Core.Rendering.Shading
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
            { TextureType.Normal, "NormalTexture"}
        };

        static public readonly Dictionary<TextureType, string> UseTextureTypeToParamName = new()
        {
            { TextureType.Diffuse, "UseDiffuse"},
            { TextureType.Specular, "UseSpecular"},
            { TextureType.Gloss, "UseGloss"},
            { TextureType.Mask, "UseMask"},
            { TextureType.MaterialMap, "UseGloss"},
            { TextureType.BaseColour, "UseDiffuse"},
            { TextureType.Normal, "UseNormal"}
        };
    }
}


