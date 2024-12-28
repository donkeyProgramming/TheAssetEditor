using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.AnimatioReTarget.Editor.BoneHandling;
using Editors.AnimatioReTarget.Editor.Settings;
using Editors.Shared.Core.Common;
using Editors.Shared.Core.Common.AnimationPlayer;
using Editors.Shared.Core.Common.BaseControl;
using Editors.Shared.Core.Services;
using GameWorld.Core.Animation;
using Microsoft.Xna.Framework;
using Shared.Core.Events;
using Shared.Core.PackFiles;

namespace Editors.AnimatioReTarget.Editor
{
    // When applying proportion scling, should also rotation be scaled? 
    // Show scale factor in view for each bone 


    public partial class AnimationRetargetViewModel : EditorHostBase, IDisposable
    {
        // private readonly ILogger _logger = Logging.Create<AnimationTransferToolViewModel>();

        private readonly IPackFileService _pfs;
        private readonly AnimationPlayerViewModel _player;
        private readonly IEventHub _eventHub;

        private SceneObject _target;
        private SceneObject _source;
        private SceneObject _generated;

        [ObservableProperty] BoneManager _boneManager;
        [ObservableProperty] AnimationReTargetRenderingComponent _rendering;
        [ObservableProperty] AnimationGenerationSettings _settings;


        public override Type EditorViewModelType => typeof(EditorView);

        public AnimationRetargetViewModel(
            IEditorHostParameters editorHostParameters,
            AnimationPlayerViewModel player,
            SceneObjectViewModelBuilder referenceModelSelectionViewModelBuilder,
            IEventHub eventHub,
            BoneManager boneManager,
            AnimationReTargetRenderingComponent renderingComponent,
            IPackFileService pfs) : base(editorHostParameters)
        {
            DisplayName = "Animation transfer tool";

            _settings = new AnimationGenerationSettings();
            _boneManager = boneManager;
            _rendering = renderingComponent;
          
            _pfs = pfs;
            _player = player;
            _eventHub = eventHub;

            GameWorld.AddComponent(renderingComponent);

            _eventHub.Register<SceneObjectUpdateEvent>(this, OnSceneObjectUpdated);

            Initialize();
        }

        void Initialize()
        {
            var target = _sceneObjectViewModelBuilder.CreateAsset(true, "Target", Color.Black, null);
            var source = _sceneObjectViewModelBuilder.CreateAsset(true, "Source", Color.Black, null);
            var generated = _sceneObjectViewModelBuilder.CreateAsset(true, "Generated", Color.Black, null);
            _target = target.Data;
            _source = source.Data;
            _generated = generated.Data;

            generated.IsExpand = false;
            generated.IsEnabled = false;

            SceneObjects.Add(target);
            SceneObjects.Add(source);
            SceneObjects.Add(generated);

            BoneManager.SetSceneNodes(source.Data, target.Data, generated.Data);
            Rendering.SetSceneNodes(source.Data, target.Data, generated.Data);
        }

        private void OnSceneObjectUpdated(SceneObjectUpdateEvent e)
        {
           if (e.Owner == _target && e.MeshChanged)
               _sceneObjectEditor.CopyMeshFromOther(_generated, _target);

            if (e.Owner == _source && e.SkeletonChanged)
                BoneManager.UpdateSourceSkeleton(_source.SkeletonName.Value);
           
            if (e.Owner == _target && e.SkeletonChanged)
                BoneManager.UpdateTargetSkeleton(_target.SkeletonName.Value);

            Rendering.ComputeOffsets();
        }

        public void LoadData(AnimationToolInput targetInput, AnimationToolInput sourceInput)
        {
            _sceneObjectEditor.SetMesh(_target, targetInput.Mesh);

            _sceneObjectEditor.SetMesh(_source, sourceInput.Mesh);
            _sceneObjectEditor.SetAnimation(_source, _pfs.GetFullPath(sourceInput.Animation));
        }

        [RelayCommand]public void UpdateAnimation()
        {
            if (CanUpdateAnimation(true))
            {
                var newAnimationClip = UpdateAnimation(_source.AnimationClip);
                _sceneObjectEditor.SetAnimationClip(_generated, newAnimationClip, "Generated");
                _player.SelectedMainAnimation = _player.PlayerItems.First(x => x.Asset == _generated);
            }
        }

        AnimationClip UpdateAnimation(AnimationClip animationToCopy)
        { 
            var service = new AnimationRemapperService_new(Settings, BoneManager.Bones);
            var newClip = service.ReMapAnimation(_source.Skeleton, _target.Skeleton, animationToCopy);
            return newClip;
        }

        bool CanUpdateAnimation(bool requireAnimation)
        {
            if (_target.Skeleton == null || _source.Skeleton == null)
            {
                MessageBox.Show("Missing a skeleton?", "Error", MessageBoxButton.OK);
                return false;
            }

            if (_source.AnimationClip == null && requireAnimation)
            {
                MessageBox.Show("No animation to copy selected", "Error", MessageBoxButton.OK);
                return false;
            }

            return true;
        }

        public void Dispose()
        {
            _eventHub?.UnRegister(this);
        }
    }
}
