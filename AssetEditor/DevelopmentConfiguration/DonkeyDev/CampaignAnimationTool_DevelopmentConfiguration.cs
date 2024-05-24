using AnimationEditor.CampaignAnimationCreator;
using AnimationEditor.PropCreator.ViewModels;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Core.ToolCreation;

namespace AssetEditor.DevelopmentConfiguration.DonkeyDev
{
    internal class CampaignAnimationTool_DevelopmentConfiguration : IDeveloperConfiguration
    {
        private readonly IEditorCreator _editorCreator;
        private readonly IToolFactory _toolFactory;
        private readonly PackFileService _packFileService;

        public CampaignAnimationTool_DevelopmentConfiguration(IEditorCreator editorCreator, IToolFactory toolFactory, PackFileService packFileService)
        {
            _editorCreator = editorCreator;
            _toolFactory = toolFactory;
            _packFileService = packFileService;
        }

        public string[] MachineNames => DonkeyMachineNameProvider.MachineNames;
        public bool IsEnabled => false;
        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.CurrentGame = GameTypeEnum.Warhammer3;
            currentSettings.SkipLoadingWemFiles = true;
        }

        public void OpenFileOnLoad()
        {
            var editorView = _toolFactory.Create<EditorHost<CampaignAnimationCreatorViewModel>>();
            var debugInput = new AnimationToolInput()
            {
                Mesh = _packFileService.FindFile(@"variantmeshes\variantmeshdefinitions\brt_damsel_campaign_01.variantmeshdefinition"),
                Animation = _packFileService.FindFile(@"animations\battle\humanoid01b\staff_and_sword\arch_mage\locomotion\hu1b_stsw_mage_combat_walk_01.anim")
            };

            editorView.Editor.SetDebugInputParameters(debugInput);
            _editorCreator.CreateEmptyEditor(editorView);
        }

    }
}
