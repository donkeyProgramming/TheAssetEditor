using GameWorld.Core.Rendering.Materials.Capabilities.Utility;
using GameWorld.Core.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Rendering.Materials.Capabilities
{
    public class DirtAndDecalCapability : ICapability
    {
        public bool UseDirt { get; set; } = false;
        public bool UseDecal { get; set; } = false;
        public TextureInput DecalDirtMap { get; set; } = new TextureInput(TextureType.Decal_dirtmap);
        public TextureInput DecalDirtMask { get; set; } = new TextureInput(TextureType.Decal_dirtmask);
        public TextureInput DecalMask { get; set; } = new TextureInput(TextureType.Decal_mask);

        public void Apply(Effect effect, ResourceLibrary resourceLibrary)
        {

        }

        public ICapability Clone()
        {
            return new DirtAndDecalCapability()
            {
                UseDirt = UseDirt,
                UseDecal = UseDecal,
                DecalDirtMap = DecalDirtMap.Clone(),
                DecalDirtMask = DecalDirtMask.Clone(),
                DecalMask = DecalMask.Clone(),
            };
        }

        public void Initialize(WsModelMaterialFile? wsModelMaterial, IRmvMaterial rmvMaterial)
        {
            if (rmvMaterial is WeightedMaterial weightedMateial)
            { 
                UseDecal = weightedMateial.UseDecal;
                UseDirt = weightedMateial.UseDirt;

                CapabilityHelper.SetTextureFromModel(rmvMaterial, null, DecalDirtMap);
                CapabilityHelper.SetTextureFromModel(rmvMaterial, null, DecalDirtMask);
                CapabilityHelper.SetTextureFromModel(rmvMaterial, null, DecalMask);
            }
        }

        public void SerializeToRmvMaterial(IRmvMaterial rmvMaterial) 
        {
            if(rmvMaterial is WeightedMaterial weightedMateial)
            {
                weightedMateial.UseDecal = UseDecal;
                weightedMateial.UseDirt = UseDirt;

                weightedMateial.SetTexture(DecalDirtMap.Type, DecalDirtMap.TexturePath);
                weightedMateial.SetTexture(DecalDirtMask.Type, DecalDirtMask.TexturePath);
                weightedMateial.SetTexture(DecalMask.Type, DecalMask.TexturePath);
            }
        }

        public (bool Result, string Message) AreEqual(ICapability otherCap)
        {
            var typedCap = otherCap as DirtAndDecalCapability;
            if (typedCap == null)
                throw new System.Exception($"Comparing {GetType} against {otherCap?.GetType()}");
            return (true, "");
        }
    }
}
