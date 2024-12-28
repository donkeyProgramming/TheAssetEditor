using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Editors.AnimatioReTarget.Editor.BoneHandling;
using Editors.AnimatioReTarget.Editor.Saving;
using Editors.AnimatioReTarget.Editor.Settings;
using Editors.Shared.Core.Common;
using Editors.Shared.Core.Common.AnimationPlayer;
using Editors.Shared.Core.Common.BaseControl;
using GameWorld.Core.Animation;
using Microsoft.Xna.Framework;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Core.Services;

namespace Editors.AnimatioReTarget.Editor
{
    // When applying proportion scling, should also rotation be scaled? 
    // Show scale factor in view for each bone 


    // Note!
    // When the test config AE-AnimTraser the followin values are applied:
    // Target (The one that gets the animation)  = Dwarf
    // Source (The one we take animation from)   = Human with animation hu1_swp_missile_attack_aim_to_shootready_01
    // Setting target(dwarf) updates generated 

    public static class AnimationRetargetIds
    {
        public static string Target => "Target";
        public static string Source => "Source";
        public static string Generated => "Generated";
    }

    public partial class AnimationRetargetEditor : EditorHostBase, IDisposable
    {
        // private readonly ILogger _logger = Logging.Create<AnimationTransferToolViewModel>();

        private readonly IPackFileService _pfs;
        private readonly AnimationPlayerViewModel _player;
        private readonly IStandardDialogs _standardDialogs;
        private readonly IEventHub _eventHub;

        private SceneObject _target;
        private SceneObject _source;
        private SceneObject _generated;

        [ObservableProperty] BoneManager _boneManager;
        [ObservableProperty] AnimationReTargetRenderingComponent _rendering;
        [ObservableProperty] AnimationGenerationSettings _settings;
        [ObservableProperty] SaveManager _saveManager;


        public override Type EditorViewModelType => typeof(EditorView);

        public AnimationRetargetEditor(
            IEditorHostParameters editorHostParameters,
            AnimationPlayerViewModel player,
            SceneObjectViewModelBuilder referenceModelSelectionViewModelBuilder,
            IStandardDialogs standardDialogs,

            IEventHub eventHub,
            SaveManager saveManager,
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
            _standardDialogs = standardDialogs;
            _eventHub = eventHub;
            _saveManager = saveManager;

            GameWorld.AddComponent(renderingComponent);

            _eventHub.Register<SceneObjectUpdateEvent>(this, OnSceneObjectUpdated);

            Initialize();
        }

        void Initialize()
        {
            var target = _sceneObjectViewModelBuilder.CreateAsset(AnimationRetargetIds.Target, true, "Target", Color.Black, null);
            var source = _sceneObjectViewModelBuilder.CreateAsset(AnimationRetargetIds.Source, true, "Source", Color.Black, null);
            var generated = _sceneObjectViewModelBuilder.CreateAsset(AnimationRetargetIds.Generated, true, "Generated", Color.Black, null);
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
            SaveManager.SetSceneNodes(source.Data, target.Data, generated.Data);
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
            var canUpdate = CanUpdateAnimation(out var errorText);
            if (canUpdate == false)
            {
               _standardDialogs.ShowDialogBox(errorText);
                return;
            }

            var newAnimationClip = UpdateAnimation(_source.AnimationClip);
            _sceneObjectEditor.SetAnimationClip(_generated, newAnimationClip, "Generated");
            _player.SelectedMainAnimation = _player.PlayerItems.First(x => x.Asset == _generated);
            
        }

        AnimationClip UpdateAnimation(AnimationClip animationToCopy)
        { 
            var service = new AnimationRemapperService(Settings, BoneManager.Bones);
            var newClip = service.ReMapAnimation(_source.Skeleton, _target.Skeleton, animationToCopy);
            return newClip;
        }

        public bool CanUpdateAnimation(out string errorText)
        {
            if (_target.Skeleton == null || _source.Skeleton == null)
            {
                errorText = "Missing a skeleton?";
                return false;
            }

            if (_source.AnimationClip == null)
            {
                errorText = "No animation to copy selected";
                return false;
            }

            errorText = string.Empty;
            return true;
        }

        public void Dispose()
        {
            _eventHub?.UnRegister(this);
        }
    }
}
