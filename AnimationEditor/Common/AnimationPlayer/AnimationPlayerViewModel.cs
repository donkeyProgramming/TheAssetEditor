using AnimationEditor.Common.ReferenceModel;
using CommonControls.Common;
using CommonControls.Services;
using CommunityToolkit.Mvvm.Input;
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

        float _selectedAnimationCurrentTime = 0;
        public float SelectedAnimationCurrentTime { get { return _selectedAnimationCurrentTime; } set { SetAndNotifyWhenChanged(ref _selectedAnimationCurrentTime, value); } }

        float _selectedAnimationMaxTime = 0;
        public float SelectedAnimationMaxTime { get { return _selectedAnimationMaxTime; } set { SetAndNotifyWhenChanged(ref _selectedAnimationMaxTime, value); } }
        int _selectedAnimationFps = 0;
        public int SelectedAnimationFps { get { return _selectedAnimationFps; } set { SetAndNotifyWhenChanged(ref _selectedAnimationFps, value); } }

        int _selectedAnimationCurrentFrame = 0;
        public int SelectedAnimationCurrentFrame { get { return _selectedAnimationCurrentFrame + 1; } set { SetAndNotifyWhenChanged(ref _selectedAnimationCurrentFrame, value); } }

        int _selectedAnimationFrameCount = 0;
        public int SelectedAnimationFrameCount { get { return _selectedAnimationFrameCount; } set { SetAndNotifyWhenChanged(ref _selectedAnimationFrameCount, value); } }

        bool _isEnabled;
        public bool IsEnabled { get { return _isEnabled; } set { SetAndNotify(ref _isEnabled, value); OnEnableChanged(IsEnabled); } }

        bool _loopAnimation = true;
        public bool LoopAnimation { get { return _loopAnimation; } set { SetAndNotifyWhenChanged(ref _loopAnimation, value); } }


        List<AssetViewModel> _assetList = new List<AssetViewModel>();

        public ObservableCollection<AssetPlayerItem> PlayerItems { get; set; } = new ObservableCollection<AssetPlayerItem>();

        AssetPlayerItem _selectedMainAnimation;
        public AssetPlayerItem SelectedMainAnimation { get { return _selectedMainAnimation; } set { MainAnimationChanged(_selectedMainAnimation, value); SetAndNotifyWhenChanged(ref _selectedMainAnimation, value); } }


        public AnimationPlayerViewModel()
        {
            IsEnabled = false;
        }

        public void RegisterAsset(AssetViewModel asset)
        {
            _assetList.Add(asset);
            PlayerItems.Add(new AssetPlayerItem(asset));

            asset.Player.LoopAnimation = false;
            if (SelectedMainAnimation == null)
                SelectedMainAnimation = PlayerItems.First();

            OnEnableChanged(IsEnabled);
        }

        public void TogleAnimationPausePlay()
        {
            foreach (var item in _assetList)
            {
                if (item.Player.IsPlaying)
                    Pause(item);
                else
                    Play(item);  
            }
        }

        public void SetAnimationNextFrame()
        {
            foreach (var item in _assetList)
                NextFrame(item);
        }

        public void SetAnimationPrivFrame()
        {
            foreach (var item in _assetList)
                PrivFrame(item);
        }

        public void SetAnimationFirstFrame()
        {
            foreach (var item in _assetList)
                SetFrame(item, 0);
        }

        public void SetAnimationLastFrame()
        {
            LoopAnimation = false;
            foreach (var item in _assetList)
                SetFrame(item, SelectedMainAnimation.Asset.Player.FrameCount());
        }

        private void MainAnimationChanged(AssetPlayerItem oldAnimation, AssetPlayerItem mainAnimation)
        {
            if (oldAnimation != null)
                oldAnimation.Asset.Player.OnFrameChanged -= Player_OnFrameChanged;

            SelectedAnimationFrameCount = mainAnimation.MaxFrames;
            //SelectedAnimationFps = mainAnimation.MaxFrames
            mainAnimation.Asset.Player.OnFrameChanged += Player_OnFrameChanged;
        }

        private void Player_OnFrameChanged(int currentFrame)
        {
            SelectedAnimationFrameCount = SelectedMainAnimation.Asset.Player.FrameCount();
            SelectedAnimationCurrentFrame = currentFrame;
            SelectedAnimationCurrentTime = (float) SelectedMainAnimation.Asset.Player.GetTimeUs() / 1_000_000;
            SelectedAnimationMaxTime = (float) SelectedMainAnimation.Asset.Player.GetAnimationLengthUs() / 1_000_000;
            SelectedAnimationFps = SelectedMainAnimation.Asset.Player.GetFPS();
            if (SelectedAnimationCurrentFrame == SelectedMainAnimation.Asset.Player.FrameCount())
            {
                if (LoopAnimation)
                {
                    SetAnimationFirstFrame();
                    TogleAnimationPausePlay();
                }
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
                    Play(item);
            }
            else
            {
                foreach (var item in _assetList)
                    Stop(item);
            }
        }


        void Play(AssetViewModel item)
        {
            item.Player.CurrentFrame = 0;
            item.Player.Play();

            foreach (var attachedItem in item.MetaDataItems)
            {
                if (attachedItem.Player != null)
                {
                    attachedItem.Player.CurrentFrame = 0;
                    attachedItem.Player.Play();
                }
            }
        }

        void Pause(AssetViewModel item)
        {
            item.Player.Pause();
            foreach (var attachedItem in item.MetaDataItems)
            {
                if (attachedItem.Player != null)
                    attachedItem.Player.Pause();
            }
        }

        void Stop(AssetViewModel item)
        {
            item.Player.Stop();

            foreach (var attachedItem in item.MetaDataItems)
            {
                if (attachedItem.Player != null)
                    attachedItem.Player.Stop();
            }
        }

        void SetFrame(AssetViewModel item, int frame)
        {
            item.Player.Pause();
            item.Player.CurrentFrame = frame;

            foreach (var attachedItem in item.MetaDataItems)
            {
                if (attachedItem.Player != null)
                {
                    attachedItem.Player.Pause();
                    attachedItem.Player.CurrentFrame = frame;
                }
            }
        }

        void NextFrame(AssetViewModel item)
        {
            item.Player.Pause();
            item.Player.CurrentFrame++;

            foreach (var attachedItem in item.MetaDataItems)
            {
                if (attachedItem.Player != null)
                {
                    attachedItem.Player.Pause();
                    attachedItem.Player.CurrentFrame = item.Player.CurrentFrame;
                }
            }
        }

        void PrivFrame(AssetViewModel item)
        {
            item.Player.Pause();
            item.Player.CurrentFrame--;

            foreach (var attachedItem in item.MetaDataItems)
            {
                if (attachedItem.Player != null)
                {
                    attachedItem.Player.Pause();
                    attachedItem.Player.CurrentFrame = item.Player.CurrentFrame;
                }
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
                AnimationName = Asset.AnimationName.Value?.AnimationFile;
                SlotName = Asset.Description;
                MaxFrames = Asset.Player.FrameCount();
            }
        }
    }
}
