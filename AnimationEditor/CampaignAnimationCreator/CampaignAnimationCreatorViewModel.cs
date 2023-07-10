using AnimationEditor.Common.AnimationPlayer;
using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.PropCreator.ViewModels;
using Common;
using CommonControls.Common;
using CommonControls.Services;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using View3D.Components;
using View3D.Scene;
using View3D.Services;

namespace AnimationEditor.CampaignAnimationCreator
{
    public class CampaignAnimationCreatorViewModel : BaseAnimationViewModel<Editor>
    {
        private readonly ReferenceModelSelectionViewModelBuilder _referenceModelSelectionViewModelBuilder;

        public CampaignAnimationCreatorViewModel(
            Editor editor,
            IComponentInserter componentInserter,
            ReferenceModelSelectionViewModelBuilder referenceModelSelectionViewModelBuilder,
            AnimationPlayerViewModel animationPlayerViewModel,
            EventHub eventHub,
            MainScene scene,
            FocusSelectableObjectService focusSelectableObjectService)
            : base(componentInserter, animationPlayerViewModel, scene, focusSelectableObjectService)
        {
            DisplayName.Value = "Campaign Animation Creator";
            Editor = editor;
            _referenceModelSelectionViewModelBuilder = referenceModelSelectionViewModelBuilder;

            eventHub.Register<SceneInitializedEvent>(Initialize);
        }

        void Initialize(SceneInitializedEvent sceneInitializedEvent)
        {
            MainModelView.Value = _referenceModelSelectionViewModelBuilder.CreateAsset(true, "model", Color.Black, MainInput);
            ReferenceModelView.Value = _referenceModelSelectionViewModelBuilder.CreateAsset(false, "not_in_use2", Color.Black, RefInput);

            ReferenceModelView.Value.Data.IsSelectable = true;
            ReferenceModelView.Value.IsControlVisible.Value = false;

            Editor.Create(MainModelView.Value.Data);
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
