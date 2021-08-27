using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.PropCreator.ViewModels;
using Common;
using CommonControls.Services;
using FileTypes.DB;
using FileTypes.PackFiles.Models;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Utility;

namespace AnimationEditor.PropCreator
{
    public class PropCreatorViewModel : BaseAnimationViewModel
    {
        public PropCreatorViewModel(ToolFactory toolFactory, PackFileService pfs, SkeletonAnimationLookUpHelper skeletonHelper, SchemaManager schemaManager) : base(toolFactory, pfs, skeletonHelper, schemaManager, "Main", "Reference")
        {
            DisplayName  = "Anim.Prop Creator";
        }

        public override void Initialize()
        {
            var propAsset = Scene.AddCompnent(new AssetViewModel(_pfs, "Prop", Color.Red, Scene));
            Player.RegisterAsset(propAsset);
            var editor = new PropCreatorEditorViewModel(propAsset, MainModelView.Data, ReferenceModelView.Data);
            Editor = editor;
        }
    }

    public static class PropCreatorViewModel_Debug
    {
        public static void CreateDamselAndSkavenEditor(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var editorView = toolFactory.CreateEditorViewModel<PropCreatorViewModel>();
            editorView.MainInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\skv_assassin.variantmeshdefinition") as PackFile,
                Animation = packfileService.FindFile(@"animations\battle\humanoid17\halberd\stand\hu17_hb_stand_01.anim") as PackFile,
            };

            editorView.RefInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\brt_damsel_campaign_01.variantmeshdefinition") as PackFile,
                Animation = packfileService.FindFile(@"animations\battle\humanoid01b\staff_and_sword\celebrate\hu1b_sfsw_celebrate_01.anim") as PackFile,
            };
            creator.CreateEmptyEditor(editorView);
        }
    }
}
