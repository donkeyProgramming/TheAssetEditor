using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.PropCreator.ViewModels;
using Common;
using CommonControls.Services;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnimationEditor.CampaignAnimationCreator
{
    public class CampaignAnimationCreatorViewModel : BaseAnimationViewModel
    {
        public CampaignAnimationCreatorViewModel(PackFileService pfs, SkeletonAnimationLookUpHelper skeletonHelper) : base(pfs, skeletonHelper, "Model", "Not_in_use")
        {
            DisplayName = "Campaign Animation Creator";
        }

        public override void Initialize()
        {
            ReferenceModelView.Data.IsSelectable = true;
            //var propAsset = Scene.AddCompnent(new AssetViewModel(_pfs, "NewAnim", Color.Red, Scene));
            //Player.RegisterAsset(propAsset);
            ReferenceModelView.IsControlVisible.Value = false;
            Editor = new Editor(_pfs, _skeletonHelper, MainModelView.Data, Scene);
        }
    }

    public static class CampaignAnimationCreator_Debug
    {
        public static void CreateDamselEditor(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var editorView = toolFactory.CreateEditorViewModel<CampaignAnimationCreatorViewModel>();
            editorView.MainInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\brt_damsel_campaign_01.variantmeshdefinition"),
                Animation = packfileService.FindFile(@"animations\battle\humanoid01b\staff_and_sword\arch_mage\locomotion\hu1b_stsw_mage_combat_walk_01.anim")
            };
       
            creator.CreateEmptyEditor(editorView);
        }
       
    }
}
