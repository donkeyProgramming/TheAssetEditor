using GameWorld.Core.Rendering.Materials.Capabilities.Utility;
using GameWorld.Core.Rendering.Materials.Serialization;
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
            //effect.Parameters["UseBlood"].SetValue(UseBlood);
            //
            //BloodMask.Apply(effect, resourceLibrary);
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
            //CapabilityHelper.SetTextureFromModel(rmvMaterial, wsModelMaterial, BloodMask);
        }

        public void SerializeToWsModel(WsMaterialTemplateEditor templateHandler)
        {
           // templateHandler.AddAttribute("TEMPLATE_ATTR_BLOOOD_PATH", BloodMask);
           // templateHandler.AddAttribute("TEMPLATE_ATTR_BLOOD_UV_SCALE_VALUE", UvScale);
           // templateHandler.AddAttribute("TEMPLATE_ATTR_USE_BLOOD_VALUE", UseBlood ? 1 : 0);
        }
    }
}
