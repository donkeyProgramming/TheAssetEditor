using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering.Shading.Capabilities;
using GameWorld.WpfWindow.ResourceHandling;

namespace GameWorld.Core.Rendering.Shading.Shaders
{
    public class EmissiveMaterial : CapabilityMaterial
    {
        public EmissiveMaterial(ResourceLibrary resourceLibrary)
            : base(CapabilityMaterialsEnum.MetalRoughPbr_Emissive, ShaderTypes.Pbs_MetalRough, resourceLibrary)
        {
            Capabilities =
                [
                    new CommonShaderParametersCapability(),
                    new DefaultCapability(),
                    new AnimationCapability(),
                    new BloodCapability(),
                    new EmissiveCapability(),
                    new TintCapability(),
                ];

            _renderingTechniqueMap[RenderingTechnique.Normal] = "BasicColorDrawing";
            _renderingTechniqueMap[RenderingTechnique.Emissive] = "GlowDrawing";
        }

        public override CapabilityMaterial Clone()
        {
            var copy = new EmissiveMaterial(_resourceLibrary)
            {
                Capabilities = CloneCapabilities()
            };

            return copy;
        }
    }
}
