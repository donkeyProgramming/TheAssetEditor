using AnimationEditor.AnimationBuilder.Nodes;
using AnimationEditor.Common.AnimationPlayer;
using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.PropCreator.ViewModels;
using Common;
using CommonControls.Common;
using CommonControls.FileTypes.DB;
using CommonControls.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using View3D.Animation;
using View3D.Animation.MetaData;
using View3D.Scene;

namespace AnimationEditor.AnimationBuilder
{

    public class AnimationBuilderInput
    {
        public List<AnimationNode> AnimationNodes { get; set; }
        public string ModelFile { get; set; }
    };

    public class AnimationBuilderViewModel : BaseAnimationViewModel
    {
        CopyPasteManager _copyPasteManager;

        public AnimationBuilderViewModel(AnimationPlayerViewModel animationPlayerViewModel, EventHub eventHub, MetaDataFactory metaDataFactory, AssetViewModelEditor assetViewModelBuilder, MainScene scene,IToolFactory toolFactory, PackFileService pfs, SkeletonAnimationLookUpHelper skeletonHelper, CopyPasteManager copyPasteManager, ApplicationSettingsService applicationSettingsService)
            : base(animationPlayerViewModel, eventHub, metaDataFactory, assetViewModelBuilder, scene, toolFactory, pfs, skeletonHelper, applicationSettingsService)
        {
            Set("Main Node", "not_in_use2", false);
            _copyPasteManager = copyPasteManager;
            DisplayName.Value = "Animation Builder";
            Pfs = pfs;
        }

        public PackFileService Pfs { get; }
        public AnimationBuilderInput Input { get; set; }

        public override void Initialize()
        {
            MainModelView.Value.IsControlVisible.Value = true;
            ReferenceModelView.Value.IsControlVisible.Value = false;
            ReferenceModelView.Value.Data.IsSelectable = false;

            var typedEditor = new Editor(_pfs, MainModelView.Value.Data, Scene, _copyPasteManager);
            Editor = typedEditor;

            if (MainInput == null)
                MainInput = new AnimationToolInput();

            typedEditor.CreateEditor(Input);
        }

        public static class AnimationBuilder_Debug
        {
            public static void CreateExampleAnimation(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService, SkeletonAnimationLookUpHelper animationLookUpHelper)
            {
                var factory = new AnimationNodeFactory(animationLookUpHelper, "humanoid01d");

                var editorView = toolFactory.Create<AnimationBuilderViewModel>();
                editorView.Input = new AnimationBuilderInput()
                {
                    ModelFile = @"variantmeshes\variantmeshdefinitions\def_malekith.variantmeshdefinition",
                    AnimationNodes = new List<AnimationNode>()
                    {
                        new AnimationNode()
                        {
                            EditItems = new ObservableCollection<AnimationEditorItem>()
                            {
                                factory.CreateAddAnimationNode(),
                            }
                        }
                    }
                };


                //editorView.MainInput = new AnimationToolInput()
                //{
                //    Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\hef_alarielle.variantmeshdefinition"),
                //    FragmentName = @"animations/animation_tables/hu1b_alarielle_staff_and_sword.frg",
                //    AnimationSlot = AnimationSlotTypeHelper.GetfromValue("STAND")
                //};
                //editorView.MainInput = new AnimationToolInput()
                //{
                //    Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\skv_throt.variantmeshdefinition"),
                //    FragmentName = @"animations/animation_tables/hu17_dlc16_throt.frg",
                //    AnimationSlot = AnimationSlotTypeHelper.GetfromValue("ATTACK_5")
                //};
                //
                ////editorView.MainInput = new AnimationToolInput()
                ////{
                ////    Mesh = packfileService.FindFile(@"warmachines\engines\emp_steam_tank\emp_steam_tank01.rigid_model_v2"),
                ////    FragmentName = @"animations/animation_tables/wm_steam_tank01.frg",
                ////    AnimationSlot = AnimationSlotTypeHelper.GetfromValue("STAND")
                ////};
                ////editorView.MainInput = new AnimationToolInput()
                ////{
                ////    Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\emp_state_troops_crossbowmen_ror.variantmeshdefinition"),
                ////    FragmentName = @"animations/animation_tables/hu1_empire_sword_crossbow.frg",
                ////    AnimationSlot = AnimationSlotTypeHelper.GetfromValue("FIRE_HIGH")
                ////};
                //
                creator.CreateEmptyEditor(editorView);
            }
        }
    }
}
