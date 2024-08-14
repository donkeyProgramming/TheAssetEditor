using GameWorld.Core.Rendering.Materials.Serialization;
using GameWorld.Core.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Rendering.Materials.Capabilities
{
    public abstract class MaterialBaseCapability : ICapability
    {
        public bool UseAlpha { get; set; }
        public float ScaleMult { get; set; } = 1;

        public virtual void Apply(Effect effect, ResourceLibrary resourceLibrary)
        {
            effect.Parameters["UseAlpha"].SetValue(UseAlpha);
        }

        public abstract ICapability Clone();
        public virtual void Initialize(WsModelMaterialFile? wsModelMaterial, IRmvMaterial rmvMaterial)
        {
            UseAlpha = rmvMaterial.AlphaMode == AlphaMode.Transparent;
        }

        public virtual void SerializeToWsModel(WsMaterialTemplateEditor templateHandler) 
        {
        }

        public virtual void SerializeToRmvMaterial(IRmvMaterial rmvMaterial)
        {
            if (UseAlpha == false)
            { 
            
            }

            rmvMaterial.AlphaMode = UseAlpha ? AlphaMode.Transparent : AlphaMode.Opaque;
        }
    }
}
