using GameWorld.Core.Rendering.Shading.Shaders;
using Shared.GameFormats.RigidModel;

namespace GameWorld.Core.Rendering.Shading.Factories
{
    public interface IMaterialFactory
    {
        CapabilityMaterial CreateShader(RmvModel model, string? wsModelFileName);
        string GetWsModelNameFromRmvFileName(string rmvFileName);
    }
}


