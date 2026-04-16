using System;
using GameWorld.Core.Rendering.Materials.Capabilities.Utility;
using GameWorld.Core.Rendering.Materials.Serialization;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel.MaterialHeaders;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Rendering.Materials.Capabilities
{
    public abstract class MaterialBaseCapability : ICapability
    {
        public bool UseAlpha { get; set; }
        public float ScaleMult { get; set; } = 1;

        public virtual void Apply(Effect effect, IScopedResourceLibrary resourceLibrary)
        {
            effect.Parameters["UseAlpha"].SetValue(UseAlpha);
        }

        public abstract ICapability Clone();
     
        public virtual (bool Result, string Message) AreEqual(ICapability otherCap)
        {
            if (otherCap is not MaterialBaseCapability typedCap)
                throw new Exception($"Comparing {GetType} against {otherCap?.GetType()}");

            if (!CompareHelper.Compare(UseAlpha, typedCap.UseAlpha, nameof(UseAlpha), out var res))
                return res;

            return (true, "");
        }

        public virtual void Initialize(WsModelMaterialFile? wsModelMaterial, IRmvMaterial rmvMaterial) { }

        public virtual void SerializeToWsModel(WsMaterialTemplateEditor templateHandler) { }

        public virtual void SerializeToRmvMaterial(IRmvMaterial rmvMaterial) { }
    }
}
