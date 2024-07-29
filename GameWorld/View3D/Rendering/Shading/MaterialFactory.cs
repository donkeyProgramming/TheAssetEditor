using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Xml;
using GameWorld.WpfWindow.ResourceHandling;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Rendering.Shading
{
    public interface IMaterialFactory
    {
        ICapabilityMaterial CreateShader(RmvModel model, string? wsModelFileName);
        string GetWsModelNameFromRmvFileName(string rmvFileName);
    }


    public class AbstractMaterialFactory
    {
        private readonly ApplicationSettingsService _applicationSettingsService;
        private readonly PackFileService _packFileService;
        private readonly ResourceLibrary _resourceLibrary;

        public AbstractMaterialFactory(ApplicationSettingsService applicationSettingsService, PackFileService packFileService, ResourceLibrary resourceLibrary)
        {
            _applicationSettingsService = applicationSettingsService;
            _packFileService = packFileService;
            _resourceLibrary = resourceLibrary;
        }

        public IMaterialFactory CreateFactory()
        {
            if (_applicationSettingsService.CurrentSettings.CurrentGame == GameTypeEnum.Warhammer3)
            {
                return new Wh3MaterialFactory(_packFileService, _resourceLibrary);
            }

            return new DefaultMaterialFactory();
        }
    }

    public class Wh3MaterialFactory : IMaterialFactory
    {
        private readonly PackFileService _packFileService;
        private readonly ResourceLibrary _resourceLibrary;

        public Wh3MaterialFactory(PackFileService packFileService, ResourceLibrary resourceLibrary) 
        {
            _packFileService = packFileService;
            _resourceLibrary = resourceLibrary;
        }

        public ICapabilityMaterial CreateShader(RmvModel model, string? wsModelMaterialPath)
        {
            WsModelMaterialFile? wsModelMaterial = null;
            if (wsModelMaterialPath != null)
            {
                var materialPackFile = _packFileService.FindFile(wsModelMaterialPath);
                wsModelMaterial = new WsModelMaterialFile(materialPackFile);
            }

            var shader = new DefaultCapabilityMaterialWh3(_resourceLibrary);
            foreach (var capability in shader.Capabilities)
                capability.Initialize(wsModelMaterial, model);

            return shader;
        }

        public string GetWsModelNameFromRmvFileName(string rmvFileName)
        {
            throw new System.NotImplementedException();
        }
    }

    public class DefaultMaterialFactory : IMaterialFactory
    {
        public DefaultMaterialFactory()
        {
        }

        public ICapabilityMaterial CreateShader(RmvModel model, string wsModelFileName)
        {
            throw new System.NotImplementedException();
        }

        public string GetWsModelNameFromRmvFileName(string rmvFileName)
        {
            throw new System.NotImplementedException();
        }
    }
}


