using AssetEditor.DevConfigs.Base;
using AssetEditor.UiCommands;
using Audio.Presentation.AudioExplorer;
using Shared.Core.Events;
using Shared.Core.Services;

namespace AssetEditor.DevConfig
{
    internal class Audio_Attila : IDeveloperConfiguration
    {
        private readonly IUiCommandFactory _uiCommandFactory;

        public Audio_Attila(IUiCommandFactory uiCommandFactory)
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
            _uiCommandFactory.Create<OpenEditorCommand>().Execute<AudioEditorViewModel>();
        }
    }
}
