using Shared.Core.DevConfig;
using Shared.Core.Events;
using Shared.Core.Services;
using Shared.Core.ToolCreation;
using Shared.Ui.Events.UiCommands;

namespace Editors.Audio.DevConfig
{
    internal class AudioExplorer_Wh3 : IDeveloperConfiguration
    {
        private readonly IUiCommandFactory _uiCommandFactory;

        public AudioExplorer_Wh3(IUiCommandFactory uiCommandFactory)
        {
            _uiCommandFactory = uiCommandFactory;
        }

        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.CurrentGame = GameTypeEnum.Warhammer3;
            currentSettings.LoadWemFiles = false;
        }

        public void OpenFileOnLoad()
        {
            _uiCommandFactory.Create<OpenEditorCommand>().Execute(EditorEnums.AudioExplorer_Editor);
        }
    }
}
