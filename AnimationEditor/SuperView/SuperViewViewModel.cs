using AnimationEditor.Common.AnimationPlayer;
using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.PropCreator.ViewModels;
using CommonControls.Common;
using Microsoft.Xna.Framework;
using Monogame.WpfInterop.Common;
using MonoGame.Framework.WpfInterop;
using View3D.Components;
using View3D.Services;

namespace AnimationEditor.SuperView
{
    public class SuperViewViewModel : BaseAnimationViewModel<Editor>
    {
        private readonly SceneObjectViewModelBuilder _sceneObjectViewModelBuilder;
        AnimationToolInput _debugDataToLoad;
        public override NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("Super View");

        public SuperViewViewModel(
            SceneObjectViewModelBuilder sceneObjectViewModelBuilder,
            Editor editor,
            AnimationPlayerViewModel animationPlayerViewModel,
            EventHub eventHub,
            IComponentInserter componentInserter,
            GameWorld gameWorld,
            FocusSelectableObjectService focusSelectableObjectService)
            : base(componentInserter, animationPlayerViewModel, gameWorld, focusSelectableObjectService)
        {
            Editor = editor;

            eventHub.Register<SceneInitializedEvent>(Initialize);
            _sceneObjectViewModelBuilder = sceneObjectViewModelBuilder;
        }

        public void SetDebugInputParameters(AnimationToolInput debugDataToLoad)
        {
            _debugDataToLoad = debugDataToLoad;
        }

        void Initialize(SceneInitializedEvent sceneInitializedEvent)
        {
            var assetViewModel = _sceneObjectViewModelBuilder.CreateAsset(true, "Root", Color.Black, _debugDataToLoad, true);
         
            Editor.Create(assetViewModel.Data);
            SceneObjects.Add(assetViewModel);
        }
    }
}
