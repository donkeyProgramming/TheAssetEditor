using AnimationEditor.Common.AnimationPlayer;
using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.PropCreator.ViewModels;
using CommonControls.Common;
using CommonControls.Services;
using CommonControls.Services.ToolCreation;
using Microsoft.Xna.Framework;
using Monogame.WpfInterop.Common;
using MonoGame.Framework.WpfInterop;
using View3D.Components;
using View3D.Services;

namespace AnimationEditor.SkeletonEditor
{
    public class SkeletonEditorViewModel : BaseAnimationViewModel<Editor>
    {
        private readonly SceneObjectViewModelBuilder _sceneObjectViewModelBuilder;
        public override NotifyAttr<string> DisplayName { get; set; } = new NotifyAttr<string>("Skeleton Editor");

        public SkeletonEditorViewModel(
            Editor editor,
            SceneObjectViewModelBuilder sceneObjectViewModelBuilder,
            AnimationPlayerViewModel animationPlayerViewModel,
            EventHub eventHub,
            IComponentInserter componentInserter,
            GameWorld scene,
            FocusSelectableObjectService focusSelectableObjectService)
            : base(componentInserter, animationPlayerViewModel, scene, focusSelectableObjectService)
        {
            Editor = editor;
            _sceneObjectViewModelBuilder = sceneObjectViewModelBuilder;

            eventHub.Register<SceneInitializedEvent>(Initialize);
            componentInserter.Execute();
        }

        void Initialize(SceneInitializedEvent sceneInitializedEvent)
        {
            var item  = _sceneObjectViewModelBuilder.CreateAsset(false, "not_in_use1", Color.Black, null);
            item.IsControlVisible.Value = false;

            Editor.CreateEditor(item.Data, @"variantmeshes\wh_variantmodels\hq3\nor\nor_war_mammoth\tech\nor_war_mammoth_howdah_01.anim");
            SceneObjects.Add(item);
        }
    }
}
