using AnimationEditor.Common.AnimationPlayer;
using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.PropCreator.ViewModels;
using Common;
using CommonControls.Common;
using CommonControls.Services;
using View3D.Animation.MetaData;
using View3D.Scene;

namespace AnimationEditor.CampaignAnimationCreator
{
    public class CampaignAnimationCreatorViewModel : BaseAnimationViewModel
    {
        private readonly AssetViewModelEditor _assetViewModelBuilder;

        public CampaignAnimationCreatorViewModel(AnimationPlayerViewModel animationPlayerViewModel, EventHub eventHub,MetaDataFactory metaDataFactory,AssetViewModelEditor assetViewModelBuilder,MainScene scene, IToolFactory toolFactory, PackFileService pfs, SkeletonAnimationLookUpHelper skeletonHelper, ApplicationSettingsService applicationSettingsService) 
            : base(animationPlayerViewModel, eventHub, metaDataFactory, assetViewModelBuilder, scene, toolFactory, pfs, skeletonHelper, applicationSettingsService)
        {
            Set("model", "not_in_use", true);
            DisplayName.Value = "Campaign Animation Creator";
            _assetViewModelBuilder = assetViewModelBuilder;
        }

        public override void Initialize()
        {
            ReferenceModelView.Value.Data.IsSelectable = true;
            ReferenceModelView.Value.IsControlVisible.Value = false;
            Editor = new Editor(_assetViewModelBuilder, _pfs, MainModelView.Value.Data);
        }
    }

    public static class CampaignAnimationCreator_Debug
    {
        public static void CreateDamselEditor(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var editorView = toolFactory.Create<CampaignAnimationCreatorViewModel>();
            editorView.MainInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\brt_damsel_campaign_01.variantmeshdefinition"),
                Animation = packfileService.FindFile(@"animations\battle\humanoid01b\staff_and_sword\arch_mage\locomotion\hu1b_stsw_mage_combat_walk_01.anim")
            };
       
            creator.CreateEmptyEditor(editorView);
        }
       
    }
}
