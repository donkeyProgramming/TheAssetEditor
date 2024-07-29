using GameWorld.WpfWindow.ResourceHandling;
using Shared.Core.PackFiles;
using Shared.Core.Services;

namespace GameWorld.Core.Rendering.Shading.Factories
{
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
}


