using CommunityToolkit.Diagnostics;
using GameWorld.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.WsModel;


namespace GameWorld.Core.Rendering.Shading.Capabilities
{
    public class DefaultCapability : BaseTextureCapability, ICapability
    {
        public float ScaleMult { get; set; } = 1;
        public bool UseAlpha { get; set; }
        public TextureInput BaseColour { get; set; } = new TextureInput(TextureType.BaseColour);
        public TextureInput MaterialMap{ get; set; } = new TextureInput(TextureType.MaterialMap);
        public TextureInput NormalMap { get; set; } = new TextureInput(TextureType.Normal);
        public TextureInput Mask { get; set; } = new TextureInput(TextureType.Mask);

        public DefaultCapability()
        {
            _textureMap[BaseColour.Type] = BaseColour;
            _textureMap[MaterialMap.Type] = MaterialMap;
            _textureMap[NormalMap.Type] = NormalMap;
            _textureMap[Mask.Type] = Mask;
        }

        public void Apply(Effect effect, ResourceLibrary resourceLibrary)
        {
            effect.Parameters["UseAlpha"].SetValue(UseAlpha);

            BaseColour.Apply(effect, resourceLibrary);
            MaterialMap.Apply(effect, resourceLibrary);
            NormalMap.Apply(effect, resourceLibrary);
            Mask.Apply(effect, resourceLibrary);
        }

        public void Initialize(WsModelFile wsModelFile, RmvModel model)
        {
            UseAlpha = model.Material.AlphaMode == AlphaMode.Transparent;

            CapabilityHelper.SetTextureFromModel(model, BaseColour);
            CapabilityHelper.SetTextureFromModel(model, MaterialMap);
            CapabilityHelper.SetTextureFromModel(model, NormalMap);
            CapabilityHelper.SetTextureFromModel(model, Mask);
        }

       
    }



}
