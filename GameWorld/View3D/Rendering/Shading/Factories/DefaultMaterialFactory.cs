using GameWorld.Core.Rendering.Shading.Shaders;
using Shared.GameFormats.RigidModel;

namespace GameWorld.Core.Rendering.Shading.Factories
{
    public class DefaultMaterialFactory : IMaterialFactory
    {
        public DefaultMaterialFactory()
        {
        }

        public CapabilityMaterial CreateShader(RmvModel model, string wsModelFileName)
        {
            throw new System.NotImplementedException();
        }

        public string GetWsModelNameFromRmvFileName(string rmvFileName)
        {
            throw new System.NotImplementedException();
        }
    }
}


