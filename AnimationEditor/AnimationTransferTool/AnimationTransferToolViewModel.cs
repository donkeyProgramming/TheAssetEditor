using AnimationEditor.Common.AnimationPlayer;
using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.PropCreator.ViewModels;
using CommonControls.Common;
using Microsoft.Xna.Framework;
using Monogame.WpfInterop.Common;
using MonoGame.Framework.WpfInterop;
using View3D.Components;
using View3D.Services;

namespace AnimationEditor.AnimationTransferTool
{
    public class AnimationTransferToolViewModel : EditorHost<Editor>
    {
        AnimationToolInput _inputTargetData;
        AnimationToolInput _inputSourceData;

        private readonly SceneObjectViewModelBuilder _referenceModelSelectionViewModelBuilder;
        private readonly SceneObjectBuilder _assetViewModelBuilder;

        public new NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("Animation transfer tool");

        public AnimationTransferToolViewModel(
            Editor editor,
            SceneObjectViewModelBuilder referenceModelSelectionViewModelBuilder,
            AnimationPlayerViewModel animationPlayerViewModel,
            EventHub eventHub,
            IComponentInserter componentInserter,
            SceneObjectBuilder assetViewModelBuilder,
            GameWorld scene,
            FocusSelectableObjectService focusSelectableObjectService)
            : base(componentInserter, animationPlayerViewModel, scene, focusSelectableObjectService, editor, eventHub)
        {
            Editor = editor;
            _referenceModelSelectionViewModelBuilder = referenceModelSelectionViewModelBuilder;
            _assetViewModelBuilder = assetViewModelBuilder;

            eventHub.Register<SceneInitializedEvent>(Initialize);
        }

        public void SetDebugInputParameters(AnimationToolInput target, AnimationToolInput source)
        {
            _inputTargetData = target;
            _inputSourceData = source;
        }

        void Initialize(SceneInitializedEvent sceneInitializedEvent)
        {
            var target = _referenceModelSelectionViewModelBuilder.CreateAsset(true, "Target", Color.Black, _inputTargetData);
            var source = _referenceModelSelectionViewModelBuilder.CreateAsset(true, "Source", Color.Black, _inputSourceData);
            var generated = _assetViewModelBuilder.CreateAsset("Generated", Color.Black);

            source.Data.IsSelectable = false;
            
            Player.RegisterAsset(generated);
            Editor.Create(target.Data, source.Data, generated);

            SceneObjects.Add(target);
            SceneObjects.Add(source);
        }
    }
}
