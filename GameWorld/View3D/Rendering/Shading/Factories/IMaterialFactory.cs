using System.Collections.Generic;
using GameWorld.Core.Rendering.Shading.Shaders;
using Shared.GameFormats.RigidModel;

namespace GameWorld.Core.Rendering.Shading.Factories
{
    public interface IMaterialFactory
    {
        CapabilityMaterial Create(RmvModel model, string? wsModelFileName);
        List<CapabilityMaterialsEnum> GetPossibleMaterials();
        CapabilityMaterial ChangeMaterial(CapabilityMaterial source, CapabilityMaterialsEnum newMaterial);
        CapabilityMaterial CreateMaterial(CapabilityMaterialsEnum type);
    }
}


