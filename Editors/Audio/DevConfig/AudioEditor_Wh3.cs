using Shared.Core.DevConfig;
using Shared.Core.Events;
using Shared.Core.Settings;
using Shared.Core.ToolCreation;
using Shared.Ui.Events.UiCommands;

namespace Editors.Audio.DevConfig
{
    internal class AudioEditor_Wh3 : IDeveloperConfiguration
    {
        private readonly IUiCommandFactory _uiCommandFactory;

        public AudioEditor_Wh3(IUiCommandFactory uiCommandFactory)
        {
            _uiCommandFactory = uiCommandFactory;
        }

        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.CurrentGame = GameTypeEnum.Warhammer3;
            currentSettings.LoadCaPacksByDefault = true;
            currentSettings.ShowCAWemFiles = false;
        }

        public void OpenFileOnLoad()
        {
            _uiCommandFactory.Create<OpenEditorCommand>().Execute(EditorEnums.Audio_Editor);
        }
    }
}
