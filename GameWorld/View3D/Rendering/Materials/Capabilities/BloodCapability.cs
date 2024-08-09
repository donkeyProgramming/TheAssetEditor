using GameWorld.Core.Rendering.Materials.Capabilities.Utility;
using GameWorld.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.WsModel;


namespace GameWorld.Core.Rendering.Materials.Capabilities
{
    public class BloodCapability : ICapability
    {
        public bool UseBlood { get; set; } = true;
        public Vector2 UvScale { get; set; } = new Vector2(1);
        public TextureInput BloodMask { get; set; } = new TextureInput(TextureType.Blood);
        public float PreviewBlood { get; set; } = 0;

        public void Apply(Effect effect, ResourceLibrary resourceLibrary)
        {
            //effect.Parameters["UseBlood"].SetValue(UseBlood);
            //
            //BloodMask.Apply(effect, resourceLibrary);
        }

        public ICapability Clone()
        {
            return new BloodCapability()
            {
                UseBlood = UseBlood,
                BloodMask = BloodMask.Clone(),
                PreviewBlood = PreviewBlood,
                UvScale = UvScale
            };
        }

        public void Initialize(WsModelMaterialFile? wsModelMaterial, RmvModel model)
        {
            CapabilityHelper.SetTextureFromModel(model, wsModelMaterial, BloodMask);
        }

        public void SerializeToRmvMaterial(IRmvMaterial rmvMaterial)
        {
            throw new System.NotImplementedException();
        }

        public void SerializeToWsModel(MaterialToWsModelSerializer templateHandler)
        {
            throw new System.NotImplementedException();
        }
    }
}
