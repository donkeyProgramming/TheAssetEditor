using Shared.Core.DevConfig;
using Shared.Core.Events;
using Shared.Core.Settings;
using Shared.Core.ToolCreation;
using Shared.Ui.Events.UiCommands;

namespace Editors.Audio.DevConfig
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
            currentSettings.LoadCaPacksByDefault = true;
            currentSettings.ShowCAWemFiles = true;
        }

        public void OpenFileOnLoad()
        {
            _uiCommandFactory.Create<OpenEditorCommand>().Execute(EditorEnums.AudioExplorer_Editor);
        }
    }
}
