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
using Shared.Core.PackFiles;

namespace Editors.AnimatioReTarget.Editor
{
    // When applying proportion scling, should also rotation be scaled? 
    // Show scale factor in view for each bone 


    public partial class AnimationRetargetViewModel : EditorHostBase
    {
        // private readonly ILogger _logger = Logging.Create<AnimationTransferToolViewModel>();

        AnimationToolInput _inputTargetData;
        AnimationToolInput _inputSourceData;

        private readonly IPackFileService _pfs;
        private readonly SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        private readonly AnimationPlayerViewModel _player;

        private SceneObject _copyTo;
        private SceneObject _copyFrom;
        private SceneObject _generated;

        [ObservableProperty] BoneManager _boneManager;
        [ObservableProperty] AnimationReTargetRenderingComponent _rendering;
        [ObservableProperty] AnimationGenerationSettings _settings;


        public override Type EditorViewModelType => typeof(EditorView);

        public AnimationRetargetViewModel(
            IEditorHostParameters editorHostParameters,
            AnimationPlayerViewModel player,
            SceneObjectViewModelBuilder referenceModelSelectionViewModelBuilder,

            BoneManager boneManager,
            AnimationReTargetRenderingComponent renderingComponent,
            IPackFileService pfs, 
            SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper) : base(editorHostParameters)
        {
            DisplayName = "Animation transfer tool";

            _settings = new AnimationGenerationSettings();
            _boneManager = boneManager;
            _rendering = renderingComponent;
          
            _pfs = pfs;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _player = player;

            GameWorld.AddComponent(renderingComponent);

            Initialize();
        }

        public void SetDebugInputParameters(AnimationToolInput target, AnimationToolInput source)
        {
            _inputTargetData = target;
            _inputSourceData = source;
        }

        void Initialize()
        {
            _inputTargetData = new AnimationToolInput()
            {
                Mesh = _pfs.FindFile(@"variantmeshes\variantmeshdefinitions\dwf_giant_slayers.variantmeshdefinition")
            };

            _inputSourceData = new AnimationToolInput()
            {
                Mesh = _pfs.FindFile(@"variantmeshes\variantmeshdefinitions\emp_archer_ror.variantmeshdefinition"),
                Animation = _pfs.FindFile(@"animations\battle\humanoid01\sword_and_pistol\missile_attacks\hu1_swp_missile_attack_aim_to_shootready_01.anim")
            };

            var target = _sceneObjectViewModelBuilder.CreateAsset(true, "Target", Color.Black, _inputTargetData);
            var source = _sceneObjectViewModelBuilder.CreateAsset(true, "Source", Color.Black, _inputSourceData);
            var sourceView = _sceneObjectViewModelBuilder.CreateAsset(true, "Generated", Color.Black, null);
            sourceView.Data.IsSelectable = false;
            sourceView.IsExpand = false;
            sourceView.IsEnabled = false;
  
            var generated = sourceView.Data;

            source.Data.IsSelectable = false;

            _player.RegisterAsset(generated);
            Create(target.Data, source.Data, generated);

            SceneObjects.Add(target);
            SceneObjects.Add(source);
            SceneObjects.Add(sourceView);

            BoneManager.SetSceneNodes(source.Data, target.Data, generated);
            _rendering.SetSceneNodes(source.Data, target.Data, generated);
        }

        void Create(SceneObject copyToAsset, SceneObject copyFromAsset, SceneObject generated)
        {
            _copyTo = copyToAsset;
            _copyFrom = copyFromAsset;
            _generated = generated;

            _copyFrom.SkeletonChanged += CopyFromSkeletonChanged;
            _copyTo.MeshChanged += CopyToMeshChanged;

            if (_copyTo.Skeleton != null)
                CopyToMeshChanged(_copyTo);

            if (_copyFrom.Skeleton != null)
                CopyFromSkeletonChanged(_copyFrom.Skeleton);

            BoneManager.UpdateSourceSkeleton(_copyFrom.Skeleton.SkeletonName);
            BoneManager.UpdateTargetSkeleton(_copyTo.Skeleton.SkeletonName);
        }
        
        [RelayCommand]public void UpdateAnimation()
        {
            if (CanUpdateAnimation(true))
            {
                var newAnimationClip = UpdateAnimation(_copyFrom.AnimationClip, _copyTo.AnimationClip);
                _sceneObjectEditor.SetAnimationClip(_generated, newAnimationClip, null);
                _player.SelectedMainAnimation = _player.PlayerItems.First(x => x.Asset == _generated);
            }
        }

        AnimationClip UpdateAnimation(AnimationClip animationToCopy, AnimationClip originalAnimation)
        {
            
            var service = new AnimationRemapperService_new(Settings, BoneManager.Bones);
            var newClip = service.ReMapAnimation(_copyFrom.Skeleton, _copyTo.Skeleton, animationToCopy);
            return newClip;
        }

        bool CanUpdateAnimation(bool requireAnimation)
        {
            if (_copyTo.Skeleton == null || _copyFrom.Skeleton == null)
            {
                MessageBox.Show("Missing a skeleton?", "Error", MessageBoxButton.OK);
                return false;
            }

            if (_copyFrom.AnimationClip == null && requireAnimation)
            {
                MessageBox.Show("No animation to copy selected", "Error", MessageBoxButton.OK);
                return false;
            }

            return true;
        }

        private void CopyToMeshChanged(SceneObject newValue)
        {
            _sceneObjectEditor.CopyMeshFromOther(_generated, newValue);
        }

        private void CopyFromSkeletonChanged(GameSkeleton newValue)
        {
            if (newValue == _copyFrom.Skeleton)
                return;

            var standAnim = _skeletonAnimationLookUpHelper.GetAnimationsForSkeleton(newValue.SkeletonName).FirstOrDefault(x => x.AnimationFile.Contains("stand"));
            if (standAnim != null)
                _sceneObjectEditor.SetAnimation(_copyFrom, standAnim);
        }
    }
}
