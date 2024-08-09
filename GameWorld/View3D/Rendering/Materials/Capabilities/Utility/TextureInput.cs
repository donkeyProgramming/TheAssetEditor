using GameWorld.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel.Types;

namespace GameWorld.Core.Rendering.Materials.Capabilities.Utility
{
    public class TextureInput
    {
        public string TexturePath { get; set; }
        public bool UseTexture { get; set; }
        public TextureType Type { get; set; }

        public TextureInput(TextureType type)
        {
            Type = type;
            UseTexture = false;
            TexturePath = null;
        }

        public TextureInput Clone()
        {
            return new TextureInput(Type)
            {
                UseTexture = UseTexture,
                TexturePath = TexturePath
            };
        }

        public void Apply(Effect effect, ResourceLibrary resourceLibrary)
        {
            var useTextureParam = ShaderParameterHelper.UseTextureTypeToParamName[Type];
            var textureParam = ShaderParameterHelper.TextureTypeToParamName[Type];
            effect.Parameters[useTextureParam].SetValue(UseTexture);

            if (UseTexture)
            {
                var texture = resourceLibrary.LoadTexture(TexturePath);
                effect.Parameters[textureParam].SetValue(texture);
            }
        }
    }

}
