using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Services;

namespace GameWorld.Core.Rendering.Materials.Shaders.SpecGloss
{
    public class DefaultMaterial : CapabilityMaterial
    {
        public DefaultMaterial(IScopedResourceLibrary resourceLibrary)
            : base(CapabilityMaterialsEnum.SpecGlossPbr_Default, ShaderTypes.Pbr_SpecGloss, resourceLibrary)
        {
            Capabilities =
            [
                new CommonShaderParametersCapability(),
                new SpecGlossCapability(),
                new AnimationCapability(),
                //new TintCapability(),
            ];

            _renderingTechniqueMap[RenderingTechnique.Normal] = "BasicColorDrawing";
        }

        protected override CapabilityMaterial CreateCloneInstance() => new DefaultMaterial(_resourceLibrary);
    }
}
