using AnimationEditor.Common.AnimationPlayer;
using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.PropCreator.ViewModels;
using Common;
using CommonControls.Common;
using CommonControls.FileTypes.AnimationPack;
using CommonControls.Services;
using View3D.Animation.MetaData;
using View3D.Components;
using View3D.Scene;

namespace AnimationEditor.SuperView
{
    public class SuperViewViewModel : BaseAnimationViewModel
    {
        private readonly Editor _superView;

        public SuperViewViewModel(Editor superView, AnimationPlayerViewModel animationPlayerViewModel, EventHub eventHub, MetaDataFactory metaDataFactory, AssetViewModelEditor assetViewModelBuilder, IComponentInserter componentInserter, MainScene scene, IToolFactory toolFactory, PackFileService pfs, 
            SkeletonAnimationLookUpHelper skeletonHelper, ApplicationSettingsService applicationSettingsService) 
            : base(animationPlayerViewModel, eventHub, metaDataFactory, assetViewModelBuilder, scene, toolFactory, pfs, skeletonHelper, applicationSettingsService)
        {
            Set("not_in_use1", "not_in_use2", false);
            _superView = superView;


            DisplayName.Value = "Super view";

            componentInserter.Execute();
        }


        public override void Initialize()
        {
            MainModelView.Value.IsControlVisible.Value = false;
            ReferenceModelView.Value.IsControlVisible.Value = false;
            ReferenceModelView.Value.Data.IsSelectable = false;

            //var typedEditor = new Editor(_metaDataFactory, _assetViewModelBuilder, _toolFactory ,Scene, _pfs, _skeletonHelper, Player, _copyPasteManager, _applicationSettingsService);
            Editor = _superView;

            if (MainInput == null)
                MainInput = new AnimationToolInput();

            _superView.Create(MainInput);
        }
    }

    public static class SuperViewViewModel_Debug
    {
        public static void CreateDamselEditor(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var editorView = toolFactory.Create<SuperViewViewModel>();
            editorView.MainInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\hef_alarielle.variantmeshdefinition"),
                FragmentName = @"animations/animation_tables/hu1b_alarielle_staff_and_sword.frg",
                AnimationSlot = DefaultAnimationSlotTypeHelper.GetfromValue("STAND")
            };
        //editorView.MainInput = new AnimationToolInput()
        //{
        //    Mesh = packfileService.FindFile(@"warmachines\engines\emp_steam_tank\emp_steam_tank01.rigid_model_v2"),
        //    FragmentName = @"animations/animation_tables/wm_steam_tank01.frg",
        //    AnimationSlot = AnimationSlotTypeHelper.GetfromValue("STAND")
        //};
           //editorView.MainInput = new AnimationToolInput()
           //{
           //    Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\emp_state_troops_crossbowmen_ror.variantmeshdefinition"),
           //    FragmentName = @"animations/animation_tables/hu1_empire_sword_crossbow.frg",
           //    AnimationSlot = AnimationSlotTypeHelper.GetfromValue("FIRE_HIGH")
           //};

            creator.CreateEmptyEditor(editorView);
        }

        public static void CreateThrot(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var editorView = toolFactory.Create<SuperViewViewModel>();
            editorView.MainInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\skv_throt.variantmeshdefinition"),
                FragmentName = @"animations/database/battle/bin/hu17_dlc16_throt.bin",
                AnimationSlot = DefaultAnimationSlotTypeHelper.GetfromValue("ATTACK_5")
            };

            creator.CreateEmptyEditor(editorView);
        }


        public static void CreatePlaguebearer(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
        {
            var editorView = toolFactory.Create<SuperViewViewModel>();
            editorView.MainInput = new AnimationToolInput()
            {
                Mesh = packfileService.FindFile(@"variantmeshes\variantmeshdefinitions\dae_plaguebearer_plagueridden.variantmeshdefinition"),
                FragmentName = @"animations/database/battle/bin/hu4d_wh3_nurgle_sword_on_palanquin.bin",
                AnimationSlot = DefaultAnimationSlotTypeHelper.GetfromValue("STAND_IDLE_2")
            };

            creator.CreateEmptyEditor(editorView);
        }
    }
}
