using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.PropCreator.ViewModels;
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

    public static class MountAnimationCreator_Debug
    {
       // public static void CreateDamselAndGrymgoreEditor(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
       // {
       //     var editorView = toolFactory.CreateEditorViewModel<MountAnimationCreatorViewModel>();
       //
       //     editorView.MainInput = new AnimationToolInput()
       //     {
       //         Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\brt_damsel_campaign_01.variantmeshdefinition")
       //     };
       //
       //     editorView.RefInput = new AnimationToolInput()
       //     {
       //         Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\lzd_carnosaur_grymloq.variantmeshdefinition")
       //     };
       //
       //     creator.CreateEmptyEditor(editorView);
       // }
       
    }
}
