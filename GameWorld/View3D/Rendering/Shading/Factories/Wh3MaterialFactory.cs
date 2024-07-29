using GameWorld.Core.Rendering.Shading.Shaders;
using GameWorld.WpfWindow.ResourceHandling;
using Shared.Core.PackFiles;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Rendering.Shading.Factories
{
    public class Wh3MaterialFactory : IMaterialFactory
    {
        private readonly PackFileService _packFileService;
        private readonly ResourceLibrary _resourceLibrary;

        public Wh3MaterialFactory(PackFileService packFileService, ResourceLibrary resourceLibrary)
        {
            _packFileService = packFileService;
            _resourceLibrary = resourceLibrary;
        }

        public CapabilityMaterial CreateShader(RmvModel model, string? wsModelMaterialPath)
        {
            WsModelMaterialFile? wsModelMaterial = null;
            if (wsModelMaterialPath != null)
            {
                var materialPackFile = _packFileService.FindFile(wsModelMaterialPath);
                wsModelMaterial = new WsModelMaterialFile(materialPackFile);
            }

            var shader = new DefaultMaterialWh3(_resourceLibrary);
            foreach (var capability in shader.Capabilities)
                capability.Initialize(wsModelMaterial, model);

            return shader;
        }

        public string GetWsModelNameFromRmvFileName(string rmvFileName)
        {
            throw new System.NotImplementedException();
        }
    }
}


