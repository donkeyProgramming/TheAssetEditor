using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.WpfWindow.ResourceHandling;

namespace GameWorld.Core.Rendering.Materials.Shaders.MetalRough
{
    public class DefaultMaterial : CapabilityMaterial
    {
        public DefaultMaterial(ResourceLibrary resourceLibrary)
            : base(CapabilityMaterialsEnum.MetalRoughPbr_Default, ShaderTypes.Pbs_MetalRough, resourceLibrary)
        {
            Capabilities =
                [
                    new CommonShaderParametersCapability(),
                    new MetalRoughCapability(),
                    new AnimationCapability(),
                    new BloodCapability(),
                ];

            _renderingTechniqueMap[RenderingTechnique.Normal] = "BasicColorDrawing";
        }

        public override CapabilityMaterial CreateCloneInstance() => new DefaultMaterial(_resourceLibrary);
    }
}
