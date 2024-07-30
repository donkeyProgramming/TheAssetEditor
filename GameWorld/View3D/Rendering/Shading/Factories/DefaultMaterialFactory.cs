using System.Collections.Generic;
using GameWorld.Core.Rendering.Shading.Shaders;
using Shared.GameFormats.RigidModel;

namespace GameWorld.Core.Rendering.Shading.Factories
{
    public class DefaultMaterialFactory : IMaterialFactory
    {
        public DefaultMaterialFactory()
        {
        }

        public CapabilityMaterial ChangeMaterial(CapabilityMaterial source, CapabilityMaterialsEnum newMaterial)
        {
            throw new System.NotImplementedException();
        }

        public CapabilityMaterial Create(RmvModel model, string wsModelFileName)
        {
            throw new System.NotImplementedException();
        }

        public List<CapabilityMaterialsEnum> GetPossibleMaterials()
        {
            throw new System.NotImplementedException();
        }

        public string GetWsModelNameFromRmvFileName(string rmvFileName)
        {
            throw new System.NotImplementedException();
        }
    }
}


