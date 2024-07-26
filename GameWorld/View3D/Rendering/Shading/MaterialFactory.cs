using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.GameFormats.RigidModel;

namespace GameWorld.Core.Rendering.Shading
{
    public interface IMaterialFactory
    {
        IShader CreateShader(RmvModel model, string wsModelFileName);

        string GetWsModelNameFromRmvFileName(string rmvFileName);
    }


    public class AbstractMaterialFactory
    {
        private readonly ApplicationSettingsService _applicationSettingsService;
        private readonly PackFileService _packFileService;

        public AbstractMaterialFactory(ApplicationSettingsService applicationSettingsService, PackFileService packFileService)
        {
            _applicationSettingsService = applicationSettingsService;
            _packFileService = packFileService;
        }

        public IMaterialFactory CreateFactory()
        {
            if (_applicationSettingsService.CurrentSettings.CurrentGame == GameTypeEnum.Warhammer3)
            {
                return new Wh3MaterialFactory(_packFileService);
            }

            return new DefaultMaterialFactory();
        }
    }

    public class Wh3MaterialFactory : IMaterialFactory
    {

        public Wh3MaterialFactory(PackFileService packFileService) 
        {
        }

        public IShader CreateShader(RmvModel model, string wsModelFileName)
        {
            throw new System.NotImplementedException();
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

        public IShader CreateShader(RmvModel model, string wsModelFileName)
        {
            throw new System.NotImplementedException();
        }

        public string GetWsModelNameFromRmvFileName(string rmvFileName)
        {
            throw new System.NotImplementedException();
        }
    }
}


