using Shared.Core.DevConfig;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.Settings;
using Shared.Core.ToolCreation;
using Shared.EmbeddedResources;
using Shared.Ui.Events.UiCommands;

namespace Editors.KitbasherEditor.DevConfig
{
    internal class Kitbash_Karl : IDeveloperConfiguration
    {
        private readonly IPackFileService _packFileService;
        private readonly IPackFileContainerLoader _packFileContainerLoader;
        private readonly IUiCommandFactory _uiCommandFactory;

        public Kitbash_Karl(IPackFileService packFileService, IPackFileContainerLoader packFileContainerLoader, IUiCommandFactory uiCommandFactory)
        {
            _packFileService = packFileService;
            _packFileContainerLoader = packFileContainerLoader;
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

            var container = _packFileContainerLoader.Load(packFile);
            container.IsCaPackFile = true;
            _packFileService.AddContainer(container);
        }
    }
}
