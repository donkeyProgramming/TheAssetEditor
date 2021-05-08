using AnimationEditor.Common.ReferenceModel;
using Common;
using CommonControls.Services;
using GalaSoft.MvvmLight.CommandWpf;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using View3D.Utility;

namespace AnimationEditor.Common.AnimationPlayer
{
    public class AnimationPlayerViewModel : NotifyPropertyChangedImpl
    {
     
        ILogger _logger = Logging.Create<AnimationPlayerViewModel>();



        int _selectedAnimationCurrentFrame = 0;
        public int SelectedAnimationCurrentFrame { get { return _selectedAnimationCurrentFrame; } set { SetAndNotify(ref _selectedAnimationCurrentFrame, value); } }

        int _selectedAnimationFrameCount = 0;
        public int SelectedAnimationFrameCount { get { return _selectedAnimationFrameCount; } set { SetAndNotify(ref _selectedAnimationFrameCount, value); } }


        bool _isEnabled;
        public bool IsEnabled { get { return _isEnabled; } set { SetAndNotify(ref _isEnabled, value); OnEnableChanged(IsEnabled); } }

        bool _loopAnimation = true;
        public bool LoopAnimation { get { return _loopAnimation; } set { SetAndNotify(ref _loopAnimation, value); } }


        List<AssetViewModel> _assetList = new List<AssetViewModel>();

        public ObservableCollection<AssetPlayerItem> PlayerItems { get; set; } = new ObservableCollection<AssetPlayerItem>();

        AssetPlayerItem _selectedMainAnimation;
        public AssetPlayerItem SelectedMainAnimation { get { return _selectedMainAnimation; } set { MainAnimationChanged(_selectedMainAnimation, value);  SetAndNotify(ref _selectedMainAnimation, value); } }

        

        public AnimationPlayerViewModel()
        {
            //var animCollection = _componentManager.GetComponent<AnimationsContainerComponent>();
            //Player = animCollection.RegisterAnimationPlayer(new AnimationPlayer(), "MainPlayer");
            //Player.OnFrameChanged += OnAnimationFrameChanged;

            IsEnabled = false;
        }

        public void RegisterAsset(AssetViewModel asset)
        {
            _assetList.Add(asset);
            PlayerItems.Add(new AssetPlayerItem(asset));

            asset.Player.LoopAnimation = false;
            if (SelectedMainAnimation == null)
                SelectedMainAnimation = PlayerItems.First();
        }

        public void TogleAnimationPausePlay()
        {
            foreach (var item in _assetList)
            {
                if(item.Player.IsPlaying)
                    item.Player.Pause();
                else
                    item.Player.Play();
            }
        }

        public void SetAnimationNextFrame()
        {
            foreach (var item in _assetList)
            {
                item.Player.Pause();
                item.Player.CurrentFrame++;
            }
        }

        public void SetAnimationPrivFrame()
        {
            foreach (var item in _assetList)
            {
                item.Player.Pause();
                item.Player.CurrentFrame--;
            }
        }

        public void SetAnimationFirstFrame()
        {
            foreach (var item in _assetList)
            {
                item.Player.Pause();
                item.Player.CurrentFrame = 0;
            }
        }

        public void SetAnimationLastFrame()
        {
            foreach (var item in _assetList)
            {
                item.Player.Pause();
                item.Player.CurrentFrame = SelectedMainAnimation.Asset.Player.FrameCount();
            }
        }

        private void MainAnimationChanged(AssetPlayerItem oldAnimation, AssetPlayerItem mainAnimation)
        {
            if (oldAnimation != null)
                oldAnimation.Asset.Player.OnFrameChanged -= Player_OnFrameChanged;

            SelectedAnimationFrameCount = mainAnimation.MaxFrames;
            mainAnimation.Asset.Player.OnFrameChanged += Player_OnFrameChanged;
        }

        private void Player_OnFrameChanged(int currentFrame)
        {
            SelectedAnimationCurrentFrame = currentFrame;
            if (currentFrame == SelectedMainAnimation.Asset.Player.FrameCount())
            {
                SetAnimationFirstFrame();
                if (LoopAnimation)
                    TogleAnimationPausePlay();
            }
        }

        private void OnEnableChanged(bool isEnabled)
        {
            if (isEnabled)
            {
                if (SelectedMainAnimation != null)
                    SelectedAnimationFrameCount = SelectedMainAnimation.MaxFrames;
                else
                    SelectedAnimationFrameCount = 0;

                foreach (var item in _assetList)
                {
                    item.Player.CurrentFrame = 0;
                    item.Player.Play();
                }
            }
            else
            {
                foreach (var item in _assetList)
                    item.Player.Stop();
            }
        }

        public class AssetPlayerItem : NotifyPropertyChangedImpl
        {
            public AssetViewModel Asset { get; set; }

            bool _isActive = true;
            public bool IsActive { get { return _isActive; } set { SetAndNotify(ref _isActive, value); } }

            string _slotName;
            public string SlotName { get { return _slotName; } set { SetAndNotify(ref _slotName, value); } }

            int _maxFrames = 0;
            public int MaxFrames { get { return _maxFrames; } set { SetAndNotify(ref _maxFrames, value); } }


            string _animationName;
            public string AnimationName { get { return _animationName; } set { SetAndNotify(ref _animationName, value); } }

            public AssetPlayerItem(AssetViewModel asset)
            {
                Asset = asset;
                Asset.AnimationChanged += _asset_AnimationChanged;
            }

            private void _asset_AnimationChanged(View3D.Animation.AnimationClip newValue)
            {
                UpdateInfo();
            }

            void UpdateInfo()
            {
                IsActive = true;
                AnimationName = Asset.AnimationName;
                SlotName = Asset.Description;
                MaxFrames = Asset.Player.FrameCount();
            }

        }
    }

    
}
