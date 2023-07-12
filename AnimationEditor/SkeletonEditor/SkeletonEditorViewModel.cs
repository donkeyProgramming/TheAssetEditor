using AnimationEditor.Common.AnimationPlayer;
using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.PropCreator.ViewModels;
using Common;
using CommonControls.Common;
using CommonControls.Services;
using CommonControls.Services.ToolCreation;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using View3D.Components;
using View3D.Services;

namespace AnimationEditor.SkeletonEditor
{
    public class SkeletonEditorViewModel : BaseAnimationViewModel<Editor>
    {
        private readonly ReferenceModelSelectionViewModelBuilder _referenceModelSelectionViewModelBuilder;

        public SkeletonEditorViewModel(
            Editor editor,
            ReferenceModelSelectionViewModelBuilder referenceModelSelectionViewModelBuilder,
            AnimationPlayerViewModel animationPlayerViewModel,
            EventHub eventHub,
            IComponentInserter componentInserter,
            GameWorld scene,
            FocusSelectableObjectService focusSelectableObjectService)
            : base(componentInserter, animationPlayerViewModel, scene, focusSelectableObjectService)
        {
            Editor = editor;
            _referenceModelSelectionViewModelBuilder = referenceModelSelectionViewModelBuilder;

            DisplayName.Value = "Skeleton Editor";

            eventHub.Register<SceneInitializedEvent>(Initialize);
            componentInserter.Execute();
        }

        void Initialize(SceneInitializedEvent sceneInitializedEvent)
        {
            MainModelView.Value = _referenceModelSelectionViewModelBuilder.CreateAsset(false, "not_in_use1", Color.Black, null);
            ReferenceModelView.Value = _referenceModelSelectionViewModelBuilder.CreateAsset(false, "not_in_use2", Color.Black, null);

            MainModelView.Value.IsControlVisible.Value = false;
            ReferenceModelView.Value.IsControlVisible.Value = false;
            ReferenceModelView.Value.Data.IsSelectable = false;


            Editor.CreateEditor(MainModelView.Value.Data, @"variantmeshes\wh_variantmodels\hq3\nor\nor_war_mammoth\tech\nor_war_mammoth_howdah_01.anim");
        }

        public static class TechSkeleton_Debug
        {
            public static void CreateDamselEditor(IEditorCreator creator, IToolFactory toolFactory, PackFileService packfileService)
            {
                //var editorView = toolFactory.Create<TechSkeletonEditorViewModel>();
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

                //creator.CreateEmptyEditor(editorView);
            }
        }
    }
}
