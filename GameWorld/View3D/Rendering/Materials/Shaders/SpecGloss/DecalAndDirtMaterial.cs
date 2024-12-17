using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Services;

namespace GameWorld.Core.Rendering.Materials.Shaders.SpecGloss
{
    public class DecalAndDirtMaterial : CapabilityMaterial
    {
        public DecalAndDirtMaterial(IScopedResourceLibrary resourceLibrary)
            : base(CapabilityMaterialsEnum.SpecGlossPbr_DirtAndDecal, ShaderTypes.Pbr_SpecGloss, resourceLibrary)
        {
            Capabilities =
            [
                new CommonShaderParametersCapability(),
                new SpecGlossCapability(),
                new AnimationCapability(),
                new DirtAndDecalCapability(),
            ];

            _renderingTechniqueMap[RenderingTechnique.Normal] = "BasicColorDrawing";
        }

        protected override CapabilityMaterial CreateCloneInstance() => new DecalAndDirtMaterial(_resourceLibrary);
    }
}
