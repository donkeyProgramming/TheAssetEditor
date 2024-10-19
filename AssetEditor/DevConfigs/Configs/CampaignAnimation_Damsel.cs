using AnimationEditor.CampaignAnimationCreator;
using Editors.Shared.Core.Common.BaseControl;
using Editors.Shared.DevConfig.Base;
using Shared.Core.PackFiles;
using Shared.Core.Services;
using Shared.Core.ToolCreation;

namespace Editors.Shared.DevConfig.Configs
{
    internal class CampaignAnimation_Damsel : IDeveloperConfiguration
    {
        private readonly IEditorCreator _editorCreator;
        private readonly PackFileService _packFileService;

        public CampaignAnimation_Damsel(IEditorCreator editorCreator, IEditorDatabase toolFactory, PackFileService packFileService)
        {
            _editorCreator = editorCreator;
            _packFileService = packFileService;
        }

        public void OverrideSettings(ApplicationSettings currentSettings)
        {
            currentSettings.CurrentGame = GameTypeEnum.Warhammer3;
            currentSettings.LoadWemFiles = true;
        }

        public void OpenFileOnLoad()
        {
            var debugInput = new AnimationToolInput()
            {
                Mesh = _packFileService.FindFile(@"variantmeshes\variantmeshdefinitions\brt_damsel_campaign_01.variantmeshdefinition"),
                Animation = _packFileService.FindFile(@"animations\battle\humanoid01b\staff_and_sword\arch_mage\locomotion\hu1b_stsw_mage_combat_walk_01.anim")
            };

            _editorCreator.Create(EditorEnums.CampaginAnimation_Editor, (x => (x as EditorHost<CampaignAnimationCreatorViewModel>).Editor.SetDebugInputParameters(debugInput)));

        }

    }
}
