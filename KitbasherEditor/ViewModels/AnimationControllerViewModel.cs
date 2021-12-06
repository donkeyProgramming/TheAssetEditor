using CommonControls.Common;
using CommonControls.FileTypes.Animation;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.Services;
using GalaSoft.MvvmLight.CommandWpf;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using View3D.Animation;
using View3D.Components.Component;
using View3D.SceneNodes;
using static CommonControls.FilterDialog.FilterUserControl;
using static CommonControls.Services.SkeletonAnimationLookUpHelper;

namespace KitbasherEditor.ViewModels
{

    public class AnimationControllerViewModel : NotifyPropertyChangedImpl, ISkeletonProvider
    {
        ILogger _logger = Logging.Create<AnimationControllerViewModel>();
        IComponentManager _componentManager;
        PackFileService _packFileService;


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


        PackFile _skeletonPackFile;
        PackFile Animation;

        public AnimationPlayer Player { get; set; }

        SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;


        int _currentFrame = 0;
        public int CurrentFrame { get { return _currentFrame; } set { SetAndNotify(ref _currentFrame, value); } }

        int _maxFrames = 0;
        public int MaxFrames { get { return _maxFrames; } set { SetAndNotify(ref _maxFrames, value);  } }

        public ICommand PausePlayCommand { get; set; }
        public ICommand NextFrameCommand { get; set; }
        public ICommand PrivFrameCommand { get; set; }

        public ICommand FirstFrameCommand { get; set; }
        public ICommand LastFrameCommand { get; set; }

        bool _isEnabled;
        public bool IsEnabled { get { return _isEnabled; } set { SetAndNotify(ref _isEnabled, value); OnEnableChanged(IsEnabled); } }



        // interface - ISkeletonProvider
        public bool IsActive => IsEnabled;

        public GameSkeleton Skeleton { get; set; }

        public AnimationControllerViewModel(IComponentManager componentManager, PackFileService pf)
        {
            _componentManager = componentManager;
            _packFileService = pf;
            _skeletonAnimationLookUpHelper = _componentManager.GetComponent<SkeletonAnimationLookUpHelper>();
            SkeletonList = _skeletonAnimationLookUpHelper.SkeletonFileNames;

            var animCollection = _componentManager.GetComponent<AnimationsContainerComponent>();
            Player = animCollection.RegisterAnimationPlayer(new AnimationPlayer(), "MainPlayer");
            Player.OnFrameChanged += OnAnimationFrameChanged;

            PausePlayCommand = new RelayCommand(OnPlayPause);
            NextFrameCommand = new RelayCommand(OnNextFrame);
            PrivFrameCommand = new RelayCommand(OnPrivFrame);

            FirstFrameCommand = new RelayCommand(OnFirstFrame);
            LastFrameCommand = new RelayCommand(OnLastFrame);

            IsEnabled = false;
        }

        private void OnAnimationFrameChanged(int currentFrame)
        {
            CurrentFrame = currentFrame;
        }

        void OnPlayPause()
        {
            var player = Player;
            if (player.IsPlaying)
                player.Pause();
            else
                player.Play();
        }

        void OnNextFrame()
        {
            Player.Pause();
            Player.CurrentFrame++;
        }

        void OnPrivFrame()
        {
            Player.Pause();
            Player.CurrentFrame--;
        }

        void OnFirstFrame()
        {
            Player.Pause();
            Player.CurrentFrame = 0;
        }

        void OnLastFrame()
        {
            Player.Pause();
            Player.CurrentFrame = Player.FrameCount();
        }

        public void SetActiveSkeleton(string skeletonName)
        {
            string animationFolder = "animations\\skeletons\\";
            var skeletonFilePath = animationFolder + skeletonName + ".anim";
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
            Skeleton = null;
            AnimationsForCurrentSkeleton = new ObservableCollection<AnimationReference>();
            if (!string.IsNullOrWhiteSpace(selectedSkeletonPath))
            {
                _skeletonPackFile = _packFileService.FindFile(selectedSkeletonPath) ;
                if (_skeletonPackFile == null)
                    HeaderText = "No skeleton";
                else
                {
                    HeaderText = _skeletonPackFile.Name + " - No Animation";
                    AnimationsForCurrentSkeleton = _skeletonAnimationLookUpHelper.GetAnimationsForSkeleton(Path.GetFileNameWithoutExtension(_skeletonPackFile.Name));

                    var skeletonAnimationFile = AnimationFile.Create(_skeletonPackFile);
                    Skeleton = new GameSkeleton(skeletonAnimationFile, Player);
                }

            }

            SelectedAnimation = null;
        }

        private void AnimationChanged(AnimationReference selectedAnimation)
        {
            Animation = null;
            if (selectedAnimation != null)
                Animation = _packFileService.FindFile(selectedAnimation.AnimationFile, selectedAnimation.Container) ;

            if (Animation != null)
                HeaderText = _skeletonPackFile.Name + " - " + Animation.Name;
            else if(_skeletonPackFile != null)
                HeaderText = _skeletonPackFile.Name + " - No Animation";
            else
                HeaderText = "No Skeleton - No Animation";

            if (Animation != null)
            {
                var animFile = AnimationFile.Create(Animation);
                var animClip = new AnimationClip(animFile);


                Player.SetAnimation(animClip, Skeleton);
                if(Player.IsPlaying && Player.IsEnabled)
                    Player.Play();

                MaxFrames = Player.FrameCount();
                CurrentFrame = 0;
            }
        }

        private void OnEnableChanged(bool isEnabled)
        {
            if (isEnabled && Animation != null)
            {
                var animFile = AnimationFile.Create(Animation);
                var animClip = new AnimationClip(animFile);

                MaxFrames = animClip.DynamicFrames.Count;
                CurrentFrame = 0;

                Player.SetAnimation(animClip, Skeleton);
            }
            else
            {
                Player.SetAnimation(null, Skeleton);
            }

            Player.IsEnabled = isEnabled;
        }
    }
}
