using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering.Shading.Capabilities;
using GameWorld.WpfWindow.ResourceHandling;

namespace GameWorld.Core.Rendering.Shading.Shaders
{
    public class DefaultMetalRoughPbrMaterial : CapabilityMaterial
    {
        public DefaultMetalRoughPbrMaterial(ResourceLibrary resourceLibrary) 
            : base(CapabilityMaterialsEnum.MetalRoughPbr_Default, ShaderTypes.Pbs_MetalRough, resourceLibrary)
        {
            Capabilities =
                [
                    new CommonShaderParametersCapability(),
                    new DefaultCapabilityMetalRough(),
                    new AnimationCapability(),
                    new BloodCapability(),
                ];

            _renderingTechniqueMap[RenderingTechnique.Normal] = "BasicColorDrawing";
        }

        public override CapabilityMaterial Clone()
        {
            var copy = new DefaultMetalRoughPbrMaterial(_resourceLibrary)
            {
                Capabilities = CloneCapabilities()
            };

            return copy;
        }
    }


    public class DefaultSpecGlossPbrMaterial : CapabilityMaterial
    {
        public DefaultSpecGlossPbrMaterial(ResourceLibrary resourceLibrary)
            : base(CapabilityMaterialsEnum.SpecGlossPbr_Default, ShaderTypes.Pbr_SpecGloss, resourceLibrary)
        {
            Capabilities =
                [
                    new CommonShaderParametersCapability(),
                    new DefaultCapabilitySpecGloss(),
                    new AnimationCapability(),
                    new BloodCapability(),
                ];

            _renderingTechniqueMap[RenderingTechnique.Normal] = "BasicColorDrawing";
        }

        public override CapabilityMaterial Clone()
        {
            var copy = new DefaultSpecGlossPbrMaterial(_resourceLibrary)
            {
                Capabilities = CloneCapabilities()
            };

            return copy;
        }
    }
}
