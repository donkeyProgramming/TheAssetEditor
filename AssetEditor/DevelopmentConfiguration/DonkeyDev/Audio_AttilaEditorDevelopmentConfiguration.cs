using AssetEditor.UiCommands;
using Audio.Presentation.AudioExplorer;
using CommonControls.Events.UiCommands;
using CommonControls.Services;

namespace AssetEditor.DevelopmentConfiguration.DonkeyDev
{
    internal class Audio_AttilaEditorDevelopmentConfiguration : IDeveloperConfiguration
    {
        private readonly IUiCommandFactory _uiCommandFactory;

        public Audio_AttilaEditorDevelopmentConfiguration(IUiCommandFactory uiCommandFactory)
        {
            _uiCommandFactory = uiCommandFactory;
        }

        public string[] MachineNames => DonkeyMachineNameProvider.MachineNames;
        public bool IsEnabled => true;
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
