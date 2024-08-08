using GameWorld.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Rendering.Shading.Capabilities
{
    public class DefaultCapabilitySpecGloss : ICapability
    {
        public float ScaleMult { get; set; } = 1;
        public bool UseAlpha { get; set; }
        public TextureInput SpecularMap { get; set; } = new TextureInput(TextureType.Specular);
        public TextureInput GlossMap { get; set; } = new TextureInput(TextureType.Gloss);
        public TextureInput DiffuseMap { get; set; } = new TextureInput(TextureType.Diffuse);
        public TextureInput NormalMap { get; set; } = new TextureInput(TextureType.Normal);
        public TextureInput Mask { get; set; } = new TextureInput(TextureType.Mask);

        public void Apply(Effect effect, ResourceLibrary resourceLibrary)
        {
            effect.Parameters["UseAlpha"].SetValue(UseAlpha);

            SpecularMap.Apply(effect, resourceLibrary);
            GlossMap.Apply(effect, resourceLibrary);
            DiffuseMap.Apply(effect, resourceLibrary);
            NormalMap.Apply(effect, resourceLibrary);
            Mask.Apply(effect, resourceLibrary);
        }

        public ICapability Clone()
        {
            return new DefaultCapabilitySpecGloss()
            {
                ScaleMult = ScaleMult,
                UseAlpha = UseAlpha,
                SpecularMap = SpecularMap.Clone(),
                GlossMap = GlossMap.Clone(),
                DiffuseMap = DiffuseMap.Clone(),
                NormalMap = NormalMap.Clone(),
                Mask = Mask.Clone(),
            };
        }

        public void Initialize(WsModelMaterialFile? wsModelMaterial, RmvModel model)
        {
            UseAlpha = model.Material.AlphaMode == AlphaMode.Transparent;

            CapabilityHelper.SetTextureFromModel(model, wsModelMaterial, SpecularMap);
            CapabilityHelper.SetTextureFromModel(model, wsModelMaterial, GlossMap);
            CapabilityHelper.SetTextureFromModel(model, wsModelMaterial, DiffuseMap);
            CapabilityHelper.SetTextureFromModel(model, wsModelMaterial, NormalMap);
            CapabilityHelper.SetTextureFromModel(model, wsModelMaterial, Mask);
        }
    }
}
