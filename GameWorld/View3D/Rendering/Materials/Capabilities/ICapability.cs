using GameWorld.Core.Rendering.Materials.Serialization;
using GameWorld.Core.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Rendering.Materials.Capabilities
{
    public interface ICapability
    {
        public void Apply(Effect effect, ResourceLibrary resourceLibrary);
        public ICapability Clone();

        void Initialize(WsModelMaterialFile? wsModelMaterial, IRmvMaterial rmvMaterial) { }
        void SerializeToWsModel(WsMaterialTemplateEditor templateHandler) { }
        void SerializeToRmvMaterial(IRmvMaterial rmvMaterial) { }
    }

}
