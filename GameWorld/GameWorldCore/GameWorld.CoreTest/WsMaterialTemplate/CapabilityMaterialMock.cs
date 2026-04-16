using GameWorld.Core.Rendering.Materials.Shaders;
using GameWorld.Core.Services;

namespace Test.GameWorld.Core.WsMaterialTemplate
{
    public class CapabilityMaterialMock : CapabilityMaterial
    {
        public CapabilityMaterialMock(CapabilityMaterialsEnum materialType) : base(materialType, ShaderTypes.Pbr_SpecGloss, null)
        {

        }
        protected override CapabilityMaterial CreateCloneInstance()
        {
            throw new NotImplementedException();
        }
    }
}
