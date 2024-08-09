using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.WpfWindow.ResourceHandling;

namespace GameWorld.Core.Rendering.Materials.Shaders.SpecGloss
{
    public class DefaultMaterial : CapabilityMaterial
    {
        public DefaultMaterial(ResourceLibrary resourceLibrary)
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

        public override CapabilityMaterial CreateCloneInstance() => new DefaultMaterial(_resourceLibrary);
    }
}
