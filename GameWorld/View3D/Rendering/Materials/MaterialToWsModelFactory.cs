using GameWorld.Core.Rendering.Materials.Shaders;
using Shared.Core.Services;
using Shared.GameFormats.RigidModel;

namespace GameWorld.Core.Rendering.Materials
{

    public interface IMaterialToWsModelSerializer
    {
        (string FileName, string FileContent) Create(string uniqueMeshName, UiVertexFormat vertexFormat, CapabilityMaterial capabilityMaterial);
    }
    public class MaterialToWsModelFactory
    {
        private readonly GameTypeEnum _preferedGameHint;

        public MaterialToWsModelFactory(GameTypeEnum preferedGameHint) 
        {
            _preferedGameHint = preferedGameHint;
        }

        public IMaterialToWsModelSerializer CreateInstance() => new MaterialToWsModelSerializer(_preferedGameHint);
    }
}
