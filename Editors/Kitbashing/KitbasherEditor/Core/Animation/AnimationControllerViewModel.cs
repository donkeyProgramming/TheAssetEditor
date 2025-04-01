using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Editors.KitbasherEditor.Core;
using Editors.KitbasherEditor.Events;
using GameWorld.Core.Animation;
using GameWorld.Core.Services;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.Animation;
using static CommonControls.FilterDialog.FilterUserControl;

namespace Editors.KitbasherEditor.ViewModels
{
    public class AnimationControllerViewModel : NotifyPropertyChangedImpl
    {
        private readonly IPackFileService _packFileService;

        PackFile _skeletonPackFile;
        PackFile Animation;

        private readonly ISkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;
        private readonly KitbasherRootScene _kitbasherRootScene;
        private readonly AnimationPlayer _player;

        string _headerText = "No animation selected";
        public string HeaderText { get { return _headerText; } set { SetAndNotify(ref _headerText, value); } }

        ObservableCollection<AnimationReference> _animationList = new ObservableCollection<AnimationReference>();
        public ObservableCollection<AnimationReference> AnimationsForCurrentSkeleton { get { return _animationList; } set { SetAndNotify(ref _animationList, value); } }

        ObservableCollection<string> _skeletonList = new ObservableCollection<string>();
        public ObservableCollection<string> SkeletonList { get { return _skeletonList; } set { SetAndNotify(ref _skeletonList, value); } }

        string _selectedSkeleton;
        public string SelectedSkeleton { get { return _selectedSkeleton; } set { SetAndNotify(ref _selectedSkeleton, value); SkeletonChanged(_selectedSkeleton); } }


        AnimationReference _selectedAnimation;
        public AnimationReference SelectedAnimation { get { return _selectedAnimation; } set { SetAndNotify(ref _selectedAnimation, value); AnimationChanged(_selectedAnimation); } }

        public OnSeachDelegate FilterByFullPath { get { return (item, expression) => { return expression.Match(item.ToString()).Success; }; } }


        int _currentFrame = 0;
        public int CurrentFrame { get { return _currentFrame; } set { SetAndNotify(ref _currentFrame, value); } }

        int _maxFrames = 0;
        public int MaxFrames { get { return _maxFrames; } set { SetAndNotify(ref _maxFrames, value); } }


        public ICommand PausePlayCommand { get; set; }
        public ICommand NextFrameCommand { get; set; }
        public ICommand PrivFrameCommand { get; set; }

        public ICommand FirstFrameCommand { get; set; }
        public ICommand LastFrameCommand { get; set; }

        bool _isEnabled;
        public bool IsEnabled { get { return _isEnabled; } set { SetAndNotify(ref _isEnabled, value); OnEnableChanged(IsEnabled); } }

        public AnimationControllerViewModel(IPackFileService pf,
            ISkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper,
            IEventHub eventHub,
            KitbasherRootScene kitbasherRootScene)
        {
            _packFileService = pf;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _kitbasherRootScene = kitbasherRootScene;
            SkeletonList = _skeletonAnimationLookUpHelper.GetAllSkeletonFileNames();

            _player = _kitbasherRootScene.Player;
            _player.OnFrameChanged += (currentFrame) => CurrentFrame = currentFrame + 1;

            PausePlayCommand = new RelayCommand(OnPlayPause);
            NextFrameCommand = new RelayCommand(OnNextFrame);
            PrivFrameCommand = new RelayCommand(OnPrivFrame);

            FirstFrameCommand = new RelayCommand(OnFirstFrame);
            LastFrameCommand = new RelayCommand(OnLastFrame);

            IsEnabled = false;
            eventHub.Register<KitbasherSkeletonChangedEvent>(this, OnSkeletonChanged);
        }

        void OnPlayPause()
        {
            var player = _player;
            if (player.IsPlaying)
                player.Pause();
            else
                player.Play();
        }

        void OnNextFrame()
        {
            _player.Pause();
            _player.CurrentFrame++;
        }

        void OnPrivFrame()
        {
            _player.Pause();
            _player.CurrentFrame--;
        }

        void OnFirstFrame()
        {
            _player.Pause();
            _player.CurrentFrame = 0;
        }

        void OnLastFrame()
        {
            _player.Pause();
            _player.CurrentFrame = _player.FrameCount();
        }

        private void OnSkeletonChanged(KitbasherSkeletonChangedEvent e)
        {
            string animationFolder = "animations\\skeletons\\";
            var skeletonFilePath = animationFolder + e.SkeletonName + ".anim";
            if (SelectedSkeleton != skeletonFilePath)
            {
                SelectedSkeleton = skeletonFilePath;
                SelectedAnimation = null;

                // Try to set a default animation
                var defaultIdleAnim = AnimationsForCurrentSkeleton.FirstOrDefault(x => x.AnimationFile.Contains("stand_idle"));
                if (defaultIdleAnim != null)
                    AnimationChanged(defaultIdleAnim);
            }
        }

        private void SkeletonChanged(string selectedSkeletonPath)
        {
            HeaderText = "";
            _skeletonPackFile = null;
            AnimationsForCurrentSkeleton = new ObservableCollection<AnimationReference>();
            if (!string.IsNullOrWhiteSpace(selectedSkeletonPath))
            {
                _skeletonPackFile = _packFileService.FindFile(selectedSkeletonPath);
                if (_skeletonPackFile == null)
                    HeaderText = "No skeleton";
                else
                {
                    HeaderText = _skeletonPackFile.Name + " - No Animation";
                    AnimationsForCurrentSkeleton = _skeletonAnimationLookUpHelper.GetAnimationsForSkeleton(Path.GetFileNameWithoutExtension(_skeletonPackFile.Name));
                }
            }

            SelectedAnimation = null;
        }

        private void AnimationChanged(AnimationReference selectedAnimation)
        {
            Animation = null;
            if (selectedAnimation != null)
                Animation = _packFileService.FindFile(selectedAnimation.AnimationFile, selectedAnimation.Container);

            if (Animation != null)
                HeaderText = _skeletonPackFile.Name + " - " + Animation.Name;
            else if (_skeletonPackFile != null)
                HeaderText = _skeletonPackFile.Name + " - No Animation";
            else
                HeaderText = "No Skeleton - No Animation";

            var skeleton = _kitbasherRootScene.Skeleton;
            var isAnimationDataPresent = Animation != null && skeleton != null;
            if (isAnimationDataPresent)
            {
                var animFile = AnimationFile.Create(Animation);
                var animClip = new AnimationClip(animFile, skeleton);

                _player.SetAnimation(animClip, skeleton, true);
                if (_player.IsPlaying && _player.IsEnabled)
                    _player.Play();

                MaxFrames = _player.FrameCount();
                CurrentFrame = 0;
            }
        }

        private void OnEnableChanged(bool isEnabled)
        {
            var skeleton = _kitbasherRootScene.Skeleton;
            var isAnimationDataPresent = Animation != null && skeleton != null;
            if (isEnabled && isAnimationDataPresent)
            {
                var animFile = AnimationFile.Create(Animation);
                var animClip = new AnimationClip(animFile, skeleton);

                MaxFrames = animClip.DynamicFrames.Count;
                CurrentFrame = 0;

                _player.SetAnimation(animClip, skeleton, true);
            }
            else
            {
                _player.SetAnimation(null, skeleton, true);
            }

            _player.IsEnabled = isEnabled;
        }
    }
}
