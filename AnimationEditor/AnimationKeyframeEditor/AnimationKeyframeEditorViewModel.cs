using AnimationEditor.Common.AnimationPlayer;
using AnimationEditor.Common.ReferenceModel;
using AnimationEditor.PropCreator.ViewModels;
using Common;
using CommonControls.Common;
using CommonControls.Services;
using KitbasherEditor.ViewModels.MenuBarViews;
using Microsoft.Xna.Framework;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Components;
using View3D.Components.Component;
using View3D.Components.Gizmo;
using View3D.Scene;

namespace AnimationEditor.AnimationKeyframeEditor
{
    public class AnimationKeyframeEditorViewModel : BaseAnimationViewModel<Editor>
    {
        private readonly ReferenceModelSelectionViewModelBuilder _referenceModelSelectionViewModelBuilder;
        private readonly AssetViewModelBuilder _assetViewModelBuilder;
        public AnimationKeyframeEditorViewModel(Editor editor,
            IComponentInserter componentInserter,
            ReferenceModelSelectionViewModelBuilder referenceModelSelectionViewModelBuilder,
            AnimationPlayerViewModel animationPlayerViewModel,
            EventHub eventHub,
            AssetViewModelBuilder assetViewModelBuilder,
            MainScene scene)
            : base(componentInserter, animationPlayerViewModel, scene)
        {
            DisplayName.Value = "Animation transfer tool";
            Editor = editor;
            _referenceModelSelectionViewModelBuilder = referenceModelSelectionViewModelBuilder;
            _assetViewModelBuilder = assetViewModelBuilder;

            eventHub.Register<SceneInitializedEvent>(Initialize);
        }


         void Initialize(SceneInitializedEvent sceneEvent)
        {
            MainModelView.Value = _referenceModelSelectionViewModelBuilder.CreateAsset(true, "Rider", Color.Black, MainInput);
            ReferenceModelView.Value = _referenceModelSelectionViewModelBuilder.CreateAsset(true, "Mount", Color.Black, RefInput);

            ReferenceModelView.Value.Data.IsSelectable = true;

            Editor.Create(MainModelView.Value.Data, ReferenceModelView.Value.Data, Scene);
        }
    }
}
