using GameWorld.Core.Rendering.Materials.Serialization;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Rendering.Materials.Capabilities
{
    public interface ICapability
    {
        public void Apply(Effect effect, IScopedResourceLibrary resourceLibrary);
        public ICapability Clone();

        void Initialize(WsModelMaterialFile? wsModelMaterial, IRmvMaterial rmvMaterial) { }
        void SerializeToWsModel(WsMaterialTemplateEditor templateHandler) { }
        void SerializeToRmvMaterial(IRmvMaterial rmvMaterial) { }

        (bool Result, string Message) AreEqual(ICapability otherCap);
    }
}
