using AnimationEditor.PropCreator.ViewModels;
using Common;
using CommonControls.Services;
using FileTypes.AnimationPack;
using FileTypes.DB;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnimationEditor.SuperView
{


    public class SuperViewViewModel : BaseAnimationViewModel
    {
        public SuperViewViewModel(PackFileService pfs, SkeletonAnimationLookUpHelper skeletonHelper, SchemaManager schemaManager) : base(pfs, skeletonHelper, schemaManager, "not_in_use1", "not_in_use2", false)
        {
            DisplayName = "Super view";
            Pfs = pfs;
        }

        public PackFileService Pfs { get; }

        public override void Initialize()
        {
            MainModelView.IsControlVisible.Value = false;
            ReferenceModelView.IsControlVisible.Value = false;
            ReferenceModelView.Data.IsSelectable = false;

            var typedEditor = new Editor(Scene, _pfs, _skeletonHelper, Player, _schemaManager);
            Editor = typedEditor;

            typedEditor.Create(MainInput);

            //var propAsset = Scene.AddCompnent(new AssetViewModel(_pfs, "Generated", Color.Black, Scene));
            //Player.RegisterAsset(propAsset);
            //Editor = new Editor(_pfs, _skeletonHelper, MainModelView.Data, ReferenceModelView.Data, propAsset, Scene);

            /*
             
             if (MainInput != null)
                {
                    MainModelView.Data.SetMesh(MainInput.Mesh);
                    if (MainInput.Animation != null)
                        MainModelView.Data.SetAnimation(_skeletonHelper.FindAnimationRefFromPackFile(MainInput.Animation, _pfs));
                }

                if (RefInput != null)
                {
                    ReferenceModelView.Data.SetMesh(RefInput.Mesh);
                    if (RefInput.Animation != null)
                        ReferenceModelView.Data.SetAnimation(_skeletonHelper.FindAnimationRefFromPackFile(RefInput.Animation, _pfs));
                }
             */

        }
    }

    public static class SuperViewViewModel_Debug
    {
        public static void CreateDamselEditor(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var editorView = toolFactory.CreateEditorViewModel<SuperViewViewModel>();
            editorView.MainInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\hef_alarielle.variantmeshdefinition"),
                FragmentName = @"animations/animation_tables/hu1b_alarielle_staff_and_sword.frg",
                AnimationSlot = AnimationSlotTypeHelper.GetfromValue("STAND")
            };
            editorView.MainInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\skv_throt.variantmeshdefinition"),
                FragmentName = @"animations/animation_tables/hu17_dlc16_throt.frg",
                AnimationSlot = AnimationSlotTypeHelper.GetfromValue("STAND_IDLE_1")
            };

            creator.CreateEmptyEditor(editorView);
        }
    }
}
