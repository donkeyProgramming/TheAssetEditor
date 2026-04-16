using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework.Graphics;

namespace GameWorld.Core.Rendering.Materials.Shaders.MetalRough
{
    public class DefaultMaterial : CapabilityMaterial
    {
        public DefaultMaterial(IScopedResourceLibrary resourceLibrary)
            : base(CapabilityMaterialsEnum.MetalRoughPbr_Default, ShaderTypes.Pbs_MetalRough, resourceLibrary)
        {
            Capabilities =
                [
                    new CommonShaderParametersCapability(),
                    new MetalRoughCapability(),
                    new AnimationCapability(),
                    new BloodCapability(),
                    //new TintCapability(),
                ];

            _renderingTechniqueMap[RenderingTechnique.Normal] = "BasicColorDrawing";
        }

        protected override void OnApply(Effect effect)
        {
            // Disable all effects, so they can be enabled later.
            effect.Parameters["CapabilityFlag_ApplyEmissive"].SetValue(false);
            effect.Parameters["CapabilityFlag_ApplyAnimation"].SetValue(false);
            base.OnApply(effect);
        }

        protected override CapabilityMaterial CreateCloneInstance() => new DefaultMaterial(_resourceLibrary);
    }
}
