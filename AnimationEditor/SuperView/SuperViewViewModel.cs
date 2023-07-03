using AnimationEditor.Common.AnimationPlayer;
using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.PropCreator.ViewModels;
using Common;
using CommonControls.Common;
using CommonControls.FileTypes.AnimationPack;
using CommonControls.Services;
using MonoGame.Framework.WpfInterop;
using View3D.Components;
using View3D.Scene;

namespace AnimationEditor.SuperView
{
    public class SuperViewViewModel : BaseAnimationViewModel<Editor>
    {
        private readonly ReferenceModelSelectionViewModelBuilder _referenceModelSelectionViewModelBuilder;

        public SuperViewViewModel(ReferenceModelSelectionViewModelBuilder referenceModelSelectionViewModelBuilder, Editor editor, AnimationPlayerViewModel animationPlayerViewModel, EventHub eventHub, 
            IComponentInserter componentInserter,  MainScene scene) 
            : base(componentInserter, animationPlayerViewModel, scene)
        {
            DisplayName.Value = "Super view";
            _referenceModelSelectionViewModelBuilder = referenceModelSelectionViewModelBuilder;
            Editor = editor;
           
            eventHub.Register<SceneInitializedEvent>(Initialize);
        }

        void Initialize(SceneInitializedEvent sceneInitializedEvent)
        {
            MainModelView.Value = _referenceModelSelectionViewModelBuilder.CreateEmpty();
            ReferenceModelView.Value = _referenceModelSelectionViewModelBuilder.CreateEmpty();
            Editor.Create(MainInput); 
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
