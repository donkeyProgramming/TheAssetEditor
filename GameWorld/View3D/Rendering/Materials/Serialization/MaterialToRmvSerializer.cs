using GameWorld.Core.Rendering.Materials.Shaders;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;

namespace GameWorld.Core.Rendering.Materials.Serialization
{
    public class MaterialToRmvSerializer
    {
        public IRmvMaterial CreateMaterialFromCapabilityMaterial(IRmvMaterial currentRmvMaterial, CapabilityMaterial material)
        {
            // Create a new empty matial
            var currentVertexFormat = currentRmvMaterial.BinaryVertexFormat;
            var newMaterial = MaterialFactory.Create().CreateMaterial(ModelMaterialEnum.weighted, currentVertexFormat);
            newMaterial.ModelName = currentRmvMaterial.ModelName;

            foreach (var cap in material.Capabilities)
                cap.SerializeToRmvMaterial(newMaterial);

            return newMaterial;
        }
    }
}
