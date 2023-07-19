using CommonControls.Services;

namespace AssetEditor.DevelopmentConfiguration.DonkeyDev
{
    internal class AtillaAudioEditorDevelopmentConfiguration : IDeveloperConfiguration
    {
        public AtillaAudioEditorDevelopmentConfiguration()
        {
        }

        public string[] MachineNames => DonkeyMachineNameProvider.MachineNames;
        public bool IsEnabled => true;
        public void OverrideSettings(ApplicationSettings currentSettings) 
        {
            currentSettings.CurrentGame = GameTypeEnum.Attila;
            currentSettings.SkipLoadingWemFiles = false;
        }

        public void OpenFileOnLoad() { }
    }
}