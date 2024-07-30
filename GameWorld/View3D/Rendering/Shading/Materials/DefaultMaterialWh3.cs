using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering.Shading.Capabilities;
using GameWorld.WpfWindow.ResourceHandling;

namespace GameWorld.Core.Rendering.Shading.Shaders
{
    public class DefaultMaterialWh3 : CapabilityMaterial
    {
        public DefaultMaterialWh3(ResourceLibrary resourceLibrary) 
            : base(CapabilityMaterialsEnum.Normal, ShaderTypes.Pbs_MetalRough, resourceLibrary)
        {
            Capabilities =
                [
                    new CommonShaderParametersCapability(),
                    new DefaultCapability(),
                    new AnimationCapability(),
                    new BloodCapability(),
                ];

            _renderingTechniqueMap[RenderingTechnique.Normal] = "BasicColorDrawing";
        }

        public override CapabilityMaterial Clone()
        {
            var copy = new DefaultMaterialWh3(_resourceLibrary)
            {
                Capabilities = CloneCapabilities()
            };

            return copy;
        }
    }
}
