using AssetEditor.DevConfigs.Base;
using AssetEditor.UiCommands;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.EmbeddedResources;

namespace AssetEditor.DevConfigs
{
    internal class Kitbash_Karl : IDeveloperConfiguration
    {
        private readonly PackFileService _packFileService;
        private readonly IUiCommandFactory _uiCommandFactory;

        public Kitbash_Karl(PackFileService packFileService, IUiCommandFactory uiCommandFactory)
        {
            _packFileService = packFileService;
            _uiCommandFactory = uiCommandFactory;
        }

        public void OpenFileOnLoad()
        {
            var file = _packFileService.FindFile(@"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\emp_karl_franz.rigid_model_v2");
            _uiCommandFactory.Create<OpenFileInEditorCommand>().Execute(file);
        }

        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.LoadCaPacksByDefault = false;
            var packFile = ResourceLoader.GetDevelopmentDataFolder() + "\\Karl_and_celestialgeneral.pack";
            _packFileService.Load(packFile, false, true);
        }
    }
}
