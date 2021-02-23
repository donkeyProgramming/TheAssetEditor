using Common;
using Filetypes.RigidModel;
using FileTypes.PackFiles.Models;
using FileTypes.PackFiles.Services;
using GalaSoft.MvvmLight.CommandWpf;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using View3D.Animation;
using View3D.Components.Component;
using View3D.SceneNodes;
using View3D.Utility;
using static CommonControls.FilterDialog.FilterUserControl;

namespace KitbasherEditor.ViewModels
{

    public class AnimationControllerViewModel : NotifyPropertyChangedImpl
    {
        ILogger _logger = Logging.Create<AnimationControllerViewModel>();
        IComponentManager _componentManager;
        PackFileService _packFileService;




        string _headerText = "No animation selected";
        public string HeaderText { get { return _headerText; } set { SetAndNotify(ref _headerText, value); } }

        ObservableCollection<string> _animationList = new ObservableCollection<string>();
        public ObservableCollection<string> AnimationsForCurrentSkeleton { get { return _animationList; } set { SetAndNotify(ref _animationList, value); } }

        List<string> _skeletonList = new List<string>();
        public List<string> SkeletonList { get { return _skeletonList; } set { SetAndNotify(ref _skeletonList, value); } }

        string _selectedSkeleton;
        public string SelectedSkeleton { get { return _selectedSkeleton; } set { SetAndNotify(ref _selectedSkeleton, value); SkeletonChanged(_selectedSkeleton); } }


        string _selectedAnimation;
        public string SelectedAnimation { get { return _selectedAnimation; } set { SetAndNotify(ref _selectedAnimation, value); AnimationChanged(_selectedAnimation); } }

        public OnSeachDelegate FilterByFullPath { get { return (item, expression) => { return expression.Match(item.ToString()).Success; }; } }


        PackFile Skeleton;
        PackFile Animation;


        //
        public AnimationPlayer Player { get; set; }
        public SkeletonNode SkeletonTreeNode { get; set; }

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
        public bool IsEnabled { get { return _isEnabled; } set { SetAndNotify(ref _isEnabled, value); Player.IsEnabled = value; } }

        public AnimationControllerViewModel(IComponentManager componentManager, PackFileService pf)
        {
            _componentManager = componentManager;
            _packFileService = pf;
            _skeletonAnimationLookUpHelper = new SkeletonAnimationLookUpHelper();
            _skeletonAnimationLookUpHelper.FindAllAnimations(_packFileService);

            var allFilesInFolder = _packFileService.FindAllFilesInDirectory("animations\\skeletons");
            SkeletonList = allFilesInFolder.Where(x => Path.GetExtension(x.Name) == ".anim").Select(x=>pf.GetFullPath(x)).ToList();

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

            SelectedSkeleton = skeletonFilePath;;
            SelectedAnimation = null;
        }

        private void SkeletonChanged(string selectedSkeletonPath)
        {
            HeaderText = "";
            Skeleton = null;
            AnimationsForCurrentSkeleton.Clear();
            if (!string.IsNullOrWhiteSpace(selectedSkeletonPath))
            {
                Skeleton = _packFileService.FindFile(selectedSkeletonPath) as PackFile;
                HeaderText = Skeleton.Name + " - No Animation";
                var animations = _skeletonAnimationLookUpHelper.GetAnimationsForSkeleton(Path.GetFileNameWithoutExtension(Skeleton.Name));
                foreach (var anim in animations)
                    AnimationsForCurrentSkeleton.Add(_packFileService.GetFullPath(anim));
            }

            SelectedAnimation = null;
        }

        private void AnimationChanged(string selectedAnimationPath)
        {
            Animation = null;
            if (string.IsNullOrWhiteSpace(selectedAnimationPath) == false)
                Animation = _packFileService.FindFile(selectedAnimationPath) as PackFile;

            if (Animation != null)
                HeaderText = Skeleton.Name + " - " + Animation.Name;
            else if(Skeleton != null)
                HeaderText = Skeleton.Name + " - No Animation";
            else
                HeaderText = "No Skeleton - No Animation";

            if (Animation != null && Skeleton != null)
            {
                var skelFile = AnimationFile.Create(Skeleton);
                var gameSkel = new GameSkeleton(skelFile, Player);

                var animFile = AnimationFile.Create(Animation);
                var animClip = new AnimationClip(animFile);

                MaxFrames = animClip.DynamicFrames.Count;
                CurrentFrame = 0;

                Player.SetAnimation(animClip, gameSkel);
                SkeletonTreeNode.Skeleton = gameSkel;
                Player.Play();
            }
        }
    }
}
