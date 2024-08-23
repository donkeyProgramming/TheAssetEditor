using GameWorld.Core.Rendering.Materials.Capabilities.Utility;
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
            if (wsModelMaterial != null)
                UseAlpha = wsModelMaterial.Alpha;
            else
                UseAlpha = rmvMaterial.AlphaMode == AlphaMode.Transparent;
        }

        public virtual void SerializeToWsModel(WsMaterialTemplateEditor templateHandler) 
        {
        }

        public virtual void SerializeToRmvMaterial(IRmvMaterial rmvMaterial)
        {
            rmvMaterial.AlphaMode = UseAlpha ? AlphaMode.Transparent : AlphaMode.Opaque;
        }

        public virtual (bool Result, string Message) AreEqual(ICapability otherCap)
        {
            var typedCap = otherCap as MaterialBaseCapability;
            if (typedCap == null)
                throw new System.Exception($"Comparing {GetType} against {otherCap?.GetType()}");

            if (CompareHelper.Compare(UseAlpha, typedCap.UseAlpha, nameof(UseAlpha), out var res))
                return res;

            return (true, "");
        }
    }
}
