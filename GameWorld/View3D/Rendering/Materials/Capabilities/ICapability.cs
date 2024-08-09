using GameWorld.Core.Rendering.Materials.Serialization;
using GameWorld.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Rendering.Materials.Capabilities
{
    public interface ICapability
    {
        void Initialize(WsModelMaterialFile? wsModelMaterial, RmvModel model);
        public void Apply(Effect effect, ResourceLibrary resourceLibrary);
        public ICapability Clone();
        void SerializeToWsModel(WsMaterialTemplateEditor templateHandler);
        void SerializeToRmvMaterial(IRmvMaterial rmvMaterial);
    }

}
