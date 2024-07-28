using GameWorld.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Rendering.Shading.Capabilities
{
    public interface ICapability
    {
        void Initialize(WsModelFile wsModelFile, RmvModel model);

        public void Apply(Effect effect, ResourceLibrary resourceLibrary);
    }

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

            //if (Texture == null || ApplyTexture == false)
            //{
            //    effect.Parameters[useTextureParam].SetValue(false);
            //}
            //else
            //{
            //    var textureParam = ShaderParameterHelper.TextureTypeToParamName[Type];
            //    effect.Parameters[useTextureParam].SetValue(ApplyTexture);
            //    effect.Parameters[textureParam].SetValue(Texture);
            //}

        }
    }

    public static class CapabilityHelper
    {
        public static void SetTextureFromModel(RmvModel model, TextureInput textureInput)
        {
            var textureType = textureInput.Type;
            var modelTexture = model.Material.GetTexture(textureType);
            if (modelTexture != null)
            {
                textureInput.TexturePath = modelTexture.Value.Path;
                textureInput.UseTexture = true;
            }
        }
    }

}
