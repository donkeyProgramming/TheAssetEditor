using GameWorld.Core.Rendering.Materials.Capabilities.Utility;
using GameWorld.Core.Rendering.Materials.Serialization;
using GameWorld.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Rendering.Materials.Capabilities
{
    public class MetalRoughCapability : ICapability
    {
        public float ScaleMult { get; set; } = 1;
        public bool UseAlpha { get; set; }
        public TextureInput BaseColour { get; set; } = new TextureInput(TextureType.BaseColour);
        public TextureInput MaterialMap { get; set; } = new TextureInput(TextureType.MaterialMap);
        public TextureInput NormalMap { get; set; } = new TextureInput(TextureType.Normal);
        public TextureInput Mask { get; set; } = new TextureInput(TextureType.Mask);
        public TextureInput Distortion { get; set; } = new TextureInput(TextureType.Distortion);
        public TextureInput DistortionNoise { get; set; } = new TextureInput(TextureType.DistortionNoise);

        public void Apply(Effect effect, ResourceLibrary resourceLibrary)
        {
            effect.Parameters["UseAlpha"].SetValue(UseAlpha);

            BaseColour.Apply(effect, resourceLibrary);
            MaterialMap.Apply(effect, resourceLibrary);
            NormalMap.Apply(effect, resourceLibrary);
            Mask.Apply(effect, resourceLibrary);
            //Distortion.Apply(effect, resourceLibrary);
            //DistortionNoise.Apply(effect, resourceLibrary);
        }

        public ICapability Clone()
        {
            return new MetalRoughCapability()
            {
                ScaleMult = ScaleMult,
                UseAlpha = UseAlpha,
                BaseColour = BaseColour.Clone(),
                MaterialMap = MaterialMap.Clone(),
                NormalMap = NormalMap.Clone(),
                Mask = Mask.Clone(),
                Distortion = Distortion.Clone(),
                DistortionNoise = DistortionNoise.Clone(),
            };
        }

        public void Initialize(WsModelMaterialFile? wsModelMaterial, RmvModel model)
        {
            UseAlpha = model.Material.AlphaMode == AlphaMode.Transparent;

            CapabilityHelper.SetTextureFromModel(model, wsModelMaterial, BaseColour);
            CapabilityHelper.SetTextureFromModel(model, wsModelMaterial, MaterialMap);
            CapabilityHelper.SetTextureFromModel(model, wsModelMaterial, NormalMap);
            CapabilityHelper.SetTextureFromModel(model, wsModelMaterial, Mask);
            CapabilityHelper.SetTextureFromModel(model, wsModelMaterial, Distortion);
            CapabilityHelper.SetTextureFromModel(model, wsModelMaterial, DistortionNoise);
        }

        public void SerializeToRmvMaterial(IRmvMaterial rmvMaterial)
        {
  
        }

        public void SerializeToWsModel(WsMaterialTemplateEditor templateHandler)
        {
            templateHandler.AddAttribute("TEMPLATE_ATTR_BASE_COLOUR_PATH", BaseColour);
            templateHandler.AddAttribute("TEMPLATE_ATTR_MASK_PATH", Mask);
            templateHandler.AddAttribute("TEMPLATE_ATTR_MATERIAL_MAP", MaterialMap);
            templateHandler.AddAttribute("TEMPLATE_ATTR_NORMAL_PATH", NormalMap);
            templateHandler.AddAttribute("TEMPLATE_ATTR_DISTORTION_PATH", Distortion);
            templateHandler.AddAttribute("TEMPLATE_ATTR_DISTORTIONNOISE_PATH", DistortionNoise);
        }
    }
}
