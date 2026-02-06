using Shared.Core.DevConfig;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;
using Shared.Core.ToolCreation;
using Shared.EmbeddedResources;
using Shared.Ui.Events.UiCommands;

namespace Editors.AnimationFragmentEditor.DevConfig
{
    internal class AnimPack_WH3 : IDeveloperConfiguration
    {
        private readonly IPackFileService _packFileService;
        private readonly IPackFileContainerLoader _packFileContainerLoader;
        private readonly IUiCommandFactory _uiCommandFactory;

        public AnimPack_WH3(IPackFileService packFileService, IPackFileContainerLoader packFileContainerLoader, IUiCommandFactory uiCommandFactory)
        {
            _packFileService = packFileService;
            _packFileContainerLoader = packFileContainerLoader;
            _uiCommandFactory = uiCommandFactory;
        }

        public void OpenFileOnLoad()
        {
            var file = _packFileService.FindFile(@"animations\database\battle\bin\animation_tables.animpack");
            _uiCommandFactory.Create<OpenEditorCommand>().Execute(file, EditorEnums.AnimationPack_Editor);
        }

        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.CurrentGame = GameTypeEnum.Warhammer3;
            currentSettings.LoadCaPacksByDefault = true;
            var packFile = ResourceLoader.GetDevelopmentDataFolder() + "\\Karl_and_celestialgeneral.pack";

            //var container = _packFileContainerLoader.Load(packFile);
            //container.IsCaPackFile = true;
            //_packFileService.AddContainer(container);
        }
    }
}
