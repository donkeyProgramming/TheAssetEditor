using GameWorld.Core.Rendering.Materials.Capabilities.Utility;
using GameWorld.Core.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Rendering.Materials.Capabilities
{
    public class SpecGlossCapability : MaterialBaseCapability
    {
        public TextureInput SpecularMap { get; set; } = new TextureInput(TextureType.Specular);
        public TextureInput GlossMap { get; set; } = new TextureInput(TextureType.Gloss);
        public TextureInput DiffuseMap { get; set; } = new TextureInput(TextureType.Diffuse);
        public TextureInput NormalMap { get; set; } = new TextureInput(TextureType.Normal);
        public TextureInput Mask { get; set; } = new TextureInput(TextureType.Mask);

        public override void Apply(Effect effect, ResourceLibrary resourceLibrary)
        {
            SpecularMap.Apply(effect, resourceLibrary);
            GlossMap.Apply(effect, resourceLibrary);
            DiffuseMap.Apply(effect, resourceLibrary);
            NormalMap.Apply(effect, resourceLibrary);
            //Mask.Apply(effect, resourceLibrary);

            base.Apply(effect, resourceLibrary);    
        }

        public override ICapability Clone()
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


        public override void Initialize(WsModelMaterialFile? wsModelMaterial, IRmvMaterial rmvMaterial)
        {
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, SpecularMap);
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, GlossMap);
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, DiffuseMap);
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, NormalMap);
            CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, Mask);

            base.Initialize(wsModelMaterial, rmvMaterial);
        }

        public override void SerializeToRmvMaterial(IRmvMaterial rmvMaterial)
        {
            rmvMaterial.SetTexture(SpecularMap.Type, SpecularMap.TexturePath);
            rmvMaterial.SetTexture(GlossMap.Type, GlossMap.TexturePath);
            rmvMaterial.SetTexture(DiffuseMap.Type, DiffuseMap.TexturePath);
            rmvMaterial.SetTexture(NormalMap.Type, NormalMap.TexturePath);
            rmvMaterial.SetTexture(Mask.Type, Mask.TexturePath);

            base.SerializeToRmvMaterial (rmvMaterial);
        }

        public static bool AreEqual(SpecGlossCapability a, SpecGlossCapability b)
        {
            if (a.UseAlpha != b.UseAlpha)
                return false;

            string[] aTextures = [
                a.SpecularMap.TexturePath,
                a.GlossMap.TexturePath,
                a.DiffuseMap.TexturePath,
                a.NormalMap.TexturePath,
                a.Mask.TexturePath,
            ];

            string[] bTextures = [
                b.SpecularMap.TexturePath,
                b.GlossMap.TexturePath,
                b.DiffuseMap.TexturePath,
                b.NormalMap.TexturePath,
                b.Mask.TexturePath,
            ];

            for (var i = 0; i < aTextures.Length; i++)
            {
                if (aTextures[i] != bTextures[i])
                    return false;
            }

            return true;
        }
    }
}
