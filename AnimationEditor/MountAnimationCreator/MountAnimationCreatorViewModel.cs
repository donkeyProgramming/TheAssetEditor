using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.PropCreator.ViewModels;
using Common;
using CommonControls.Services;
using FileTypes.PackFiles.Models;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

using System.Text;
using View3D.Components.Component.Selection;
using View3D.Utility;

namespace AnimationEditor.MountAnimationCreator
{

    public class MountAnimationCreatorViewModel : BaseAnimationViewModel
    {
        public MountAnimationCreatorViewModel(PackFileService pfs, SkeletonAnimationLookUpHelper skeletonHelper) : base(pfs, skeletonHelper, "Rider", "Mount")
        {
            DisplayName = "MountAnimCreator";
        }

        public override void Initialize()
        {
            ReferenceModelView.Data.IsSelectable = true;
            var propAsset = Scene.AddCompnent(new AssetViewModel(_pfs, "NewAnim", Color.Red, Scene));
            Player.RegisterAsset(propAsset);
            Editor = new Editor(_pfs, MainModelView.Data, ReferenceModelView.Data, propAsset, Scene);
        }
    }

    public static class MountAnimationCreatorViewModel_Debug
    {
        public static void CreateDamselAndGrymgoreEditor(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var editorView = toolFactory.CreateEditorViewModel<MountAnimationCreatorViewModel>();

            editorView.MainInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\brt_damsel_campaign_01.variantmeshdefinition") as PackFile,
                Animation = packfileService.FindFile(@"animations\battle\humanoid01b\rider\horse01\spear_and_shield\locomotion\hu1b_hr1_sps_rider1_walk_01.anim") as PackFile,
            };

            editorView.RefInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\lzd_carnosaur_grymloq.variantmeshdefinition") as PackFile,
                Animation = packfileService.FindFile(@"animations\battle\humanoid07b\rider\raptor03b\club\locomotion\hu7b_rp3b_cl_rider1_walk_03.anim") as PackFile,
            };

            creator.CreateEmptyEditor(editorView);
        }


        public static void CreateKarlAndSquigEditor(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var editorView = toolFactory.CreateEditorViewModel<MountAnimationCreatorViewModel>();

            editorView.MainInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\emp_ch_karl.variantmeshdefinition") as PackFile,
                Animation = packfileService.FindFile(@"animations\battle\humanoid01\rider\horse01\staff_and_sword\attacks\hu1_hr1_sfsw_rider1_attack_01.anim") as PackFile,
            };

            editorView.RefInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\grn_great_cave_squig.variantmeshdefinition") as PackFile,
                Animation = packfileService.FindFile(@"animations\battle\raptor02\attacks\rp2_attack_05.anim") as PackFile,
            };

            creator.CreateEmptyEditor(editorView);
        }

        public static void CreateBroodHorrorEditor(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var editorView = toolFactory.CreateEditorViewModel<MountAnimationCreatorViewModel>();

            editorView.MainInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\skv_plague_priest.variantmeshdefinition") as PackFile,
               // Animation = packfileService.FindFile(@"animations\battle\humanoid01\rider\horse01\staff_and_sword\attacks\hu1_hr1_sfsw_rider1_attack_01.anim") as PackFile,
            };

            editorView.RefInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\skv_brood_horror.variantmeshdefinition") as PackFile,
                Animation = packfileService.FindFile(@"animations\battle\rat01\locomotion\rt1_walk_01.anim") as PackFile,
            };

            creator.CreateEmptyEditor(editorView);
        }

        public static void CreateLionAndHu01b(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var editorView = toolFactory.CreateEditorViewModel<MountAnimationCreatorViewModel>();

            editorView.MainInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\hef_princess_campaign_01.variantmeshdefinition") as PackFile,
                Animation = packfileService.FindFile(@"animations\battle\humanoid01b\rider\great_stag01\spear_bow\attack\hu1b_st1_spbo_great_stag_rider1_jumping_attack_01.anim") as PackFile,
            };

            editorView.RefInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\hef_war_lion.variantmeshdefinition") as PackFile,
                Animation = packfileService.FindFile(@"animations\battle\bigcat04\attacks\bc4_lion_jumping_attack_01.anim") as PackFile,
            };

            creator.CreateEmptyEditor(editorView);
        }
    }
}



