using Audio.Presentation.AudioExplorer;
using Editors.Shared.DevConfig.Base;
using Shared.Core.Events;
using Shared.Core.Services;
using Shared.Ui.Events.UiCommands;

namespace Editors.Shared.DevConfig.Configs
{
    internal class AudioExplorer_Attila : IDeveloperConfiguration
    {
        private readonly IUiCommandFactory _uiCommandFactory;

        public AudioExplorer_Attila(IUiCommandFactory uiCommandFactory)
        {
            _uiCommandFactory = uiCommandFactory;
        }

        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.CurrentGame = GameTypeEnum.Attila;
            currentSettings.SkipLoadingWemFiles = false;
        }

        public void OpenFileOnLoad()
        {
            _uiCommandFactory.Create<OpenEditorCommand>().Execute<AudioExplorerViewModel>();
        }
    }
}
