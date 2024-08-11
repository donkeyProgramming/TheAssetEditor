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
    public class SpecGlossCapability : ICapability
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
            return new SpecGlossCapability()
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

        public void Initialize(WsModelMaterialFile? wsModelMaterial, IRmvMaterial rmvMaterial)
        {
            UseAlpha = rmvMaterial.AlphaMode == AlphaMode.Transparent;

            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, SpecularMap);
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, GlossMap);
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, DiffuseMap);
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, NormalMap);
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, Mask);
        }
    }
}
