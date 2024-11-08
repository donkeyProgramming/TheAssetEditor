using Shared.Core.DevConfig;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using Shared.EmbeddedResources;
using Shared.Ui.Events.UiCommands;

namespace Editors.KitbasherEditor.DevConfig
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
            var file = _packFileService.FindFile(@"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\emp_karl_franz.wsmodel");
            _uiCommandFactory.Create<OpenEditorCommand>().Execute(file, EditorEnums.Kitbash_Editor);
        }

        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.CurrentGame = GameTypeEnum.Warhammer3;
            currentSettings.LoadCaPacksByDefault = false;
            var packFile = ResourceLoader.GetDevelopmentDataFolder() + "\\Karl_and_celestialgeneral.pack";
            _packFileService.Load(packFile, false, true);
        }
    }
}
