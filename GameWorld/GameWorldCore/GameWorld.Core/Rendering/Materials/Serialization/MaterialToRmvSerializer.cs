using GameWorld.Core.Rendering.Materials.Shaders;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;

namespace GameWorld.Core.Rendering.Materials.Serialization
{
    public class MaterialToRmvSerializer
    {
        public IRmvMaterial CreateMaterialFromCapabilityMaterial(CapabilityMaterial material)
        {
            var newMaterial = MaterialFactory.Create().CreateMaterial(ModelMaterialEnum.weighted);

            foreach (var cap in material.Capabilities)
                cap.SerializeToRmvMaterial(newMaterial);

            return newMaterial;
        }
    }
}
