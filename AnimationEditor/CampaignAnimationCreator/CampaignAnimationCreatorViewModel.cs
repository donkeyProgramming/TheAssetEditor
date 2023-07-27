using AnimationEditor.Common.AnimationPlayer;
using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.PropCreator.ViewModels;
using CommonControls.Common;
using Microsoft.Xna.Framework;
using Monogame.WpfInterop.Common;
using MonoGame.Framework.WpfInterop;
using View3D.Components;
using View3D.Services;

namespace AnimationEditor.CampaignAnimationCreator
{
    public class CampaignAnimationCreatorViewModel : BaseAnimationViewModel<Editor>
    {
        AnimationToolInput _debugDataToLoad;

        private readonly SceneObjectViewModelBuilder _referenceModelSelectionViewModelBuilder;
        public override NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("Campaign Animation Creator");
        public CampaignAnimationCreatorViewModel(
            Editor editor,
            IComponentInserter componentInserter,
            SceneObjectViewModelBuilder referenceModelSelectionViewModelBuilder,
            AnimationPlayerViewModel animationPlayerViewModel,
            EventHub eventHub,
            GameWorld scene,
            FocusSelectableObjectService focusSelectableObjectService)
            : base(componentInserter, animationPlayerViewModel, scene, focusSelectableObjectService)
        {
            Editor = editor;
            _referenceModelSelectionViewModelBuilder = referenceModelSelectionViewModelBuilder;

            eventHub.Register<SceneInitializedEvent>(Initialize);
        }

        public void SetDebugInputParameters(AnimationToolInput debugDataToLoad)
        {
            _debugDataToLoad = debugDataToLoad;
        }

        void Initialize(SceneInitializedEvent sceneInitializedEvent)
        {
            var item = _referenceModelSelectionViewModelBuilder.CreateAsset(true, "model", Color.Black, _debugDataToLoad);
            Editor.Create(item.Data);
            SceneObjects.Add(item);
        }
    }

   /* public static class CampaignAnimationCreator_Debug
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

    }*/
}
