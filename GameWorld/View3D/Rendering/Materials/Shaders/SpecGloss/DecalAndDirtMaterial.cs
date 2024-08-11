using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.WpfWindow.ResourceHandling;

namespace GameWorld.Core.Rendering.Materials.Shaders.SpecGloss
{
    public class DecalAndDirtMaterial : CapabilityMaterial
    {
        public DecalAndDirtMaterial(ResourceLibrary resourceLibrary)
            : base(CapabilityMaterialsEnum.SpecGlossPbr_DirtAndDecal, ShaderTypes.Pbr_SpecGloss, resourceLibrary)
        {
            Capabilities =
            [
                new CommonShaderParametersCapability(),
                new SpecGlossCapability(),
                new AnimationCapability(),
                new DecalAndDirtCapability(),
            ];

            _renderingTechniqueMap[RenderingTechnique.Normal] = "BasicColorDrawing";
        }

        protected override CapabilityMaterial CreateCloneInstance() => new DecalAndDirtMaterial(_resourceLibrary);
    }
}
