using AssetEditor.UiCommands;
using CommonControls.Events.UiCommands;
using CommonControls.Services;

namespace AssetEditor.DevelopmentConfiguration.DonkeyDev
{
    internal class LoadKarlFranzInKitbashEditorDevelopmentConfiguration : IDeveloperConfiguration
    {
        private readonly PackFileService _packFileService;
        private readonly IUiCommandFactory _uiCommandFactory;

        public LoadKarlFranzInKitbashEditorDevelopmentConfiguration(PackFileService packFileService, IUiCommandFactory uiCommandFactory)
        {
            _packFileService = packFileService;
            _uiCommandFactory = uiCommandFactory;
        }

        public string[] MachineNames => DonkeyMachineNameProvider.MachineNames;

        public bool IsEnabled => false;
        public void OverrideSettings(ApplicationSettings currentSettings) { }

        public void OpenFileOnLoad()
        {
            var file = _packFileService.FindFile(@"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\emp_karl_franz.rigid_model_v2");
            _uiCommandFactory.Create<OpenFileInEditorCommand>().Execute(file);
        }
    }
}