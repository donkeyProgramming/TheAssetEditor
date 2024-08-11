using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.WpfWindow.ResourceHandling;

namespace GameWorld.Core.Rendering.Materials.Shaders.MetalRough
{
    public class EmissiveMaterial : CapabilityMaterial
    {
        public EmissiveMaterial(ResourceLibrary resourceLibrary)
            : base(CapabilityMaterialsEnum.MetalRoughPbr_Emissive, ShaderTypes.Pbs_MetalRough, resourceLibrary)
        {
            Capabilities =
                [
                    new CommonShaderParametersCapability(),
                    new MetalRoughCapability(),
                    new AnimationCapability(),
                    new BloodCapability(),
                    new EmissiveCapability(),
                    new TintCapability(),
                ];

            _renderingTechniqueMap[RenderingTechnique.Normal] = "BasicColorDrawing";
            _renderingTechniqueMap[RenderingTechnique.Emissive] = "GlowDrawing";
        }

        protected override CapabilityMaterial CreateCloneInstance() => new EmissiveMaterial(_resourceLibrary);
    }
}
