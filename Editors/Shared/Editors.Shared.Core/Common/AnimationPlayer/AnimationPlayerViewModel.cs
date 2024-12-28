using System.Collections.Generic;
using System.Collections.ObjectModel;
using Shared.Core.Misc;

namespace Editors.Shared.Core.Common.AnimationPlayer
{
    public class AnimationPlayerViewModel : NotifyPropertyChangedImpl
    {
        readonly List<SceneObject> _assetList = new();

        public NotifyAttr<float> SelectedAnimationCurrentTime { get; private set; } = new();
        public NotifyAttr<float> SelectedAnimationMaxTime { get; private set; } = new();
        public NotifyAttr<int> SelectedAnimationFps { get; private set; } = new();
        public NotifyAttr<int> SelectedAnimationCurrentFrame { get; private set; } = new();
        public NotifyAttr<int> SelectedAnimationFrameCount { get; private set; } = new();
        public NotifyAttr<bool> IsEnabled { get; set; } = new();
        public NotifyAttr<bool> LoopAnimation { get; set; } = new(true);

        public ObservableCollection<AssetPlayerItem> PlayerItems { get; private set; } = new ObservableCollection<AssetPlayerItem>();

        AssetPlayerItem _selectedMainAnimation;
        public AssetPlayerItem SelectedMainAnimation { get { return _selectedMainAnimation; } set { OnMainAnimationChanged(_selectedMainAnimation, value); SetAndNotifyWhenChanged(ref _selectedMainAnimation, value); } }

        public AnimationPlayerViewModel()
        {
            IsEnabled.Value = false;
            IsEnabled.PropertyChanged += (x, y) => OnAnimationPlayerEnabled(IsEnabled.Value);
        }

        private void OnMainAnimationChanged(AssetPlayerItem oldAnimation, AssetPlayerItem mainAnimation)
        {
            if (oldAnimation != null)
                oldAnimation.Asset.Player.OnFrameChanged -= OnAnimationFrameChanged;

            SelectedAnimationFrameCount.Value = mainAnimation.MaxFrames.Value;
            mainAnimation.Asset.Player.OnFrameChanged += OnAnimationFrameChanged;
        }

        public void RegisterAsset(SceneObject asset)
        {
            _assetList.Add(asset);
            var playerItem = new AssetPlayerItem() { Asset = asset };
            PlayerItems.Add(playerItem);

            asset.Player.LoopAnimation = false;
            if (SelectedMainAnimation == null)
                SelectedMainAnimation = playerItem;

            OnAnimationPlayerEnabled(IsEnabled.Value);
            asset.AnimationChanged += (x) => RefreshAssetViewModel(asset, playerItem);
        }

        void RefreshAssetViewModel(SceneObject asset, AssetPlayerItem playerItem)
        {
            playerItem.AnimationName.Value = asset.AnimationName.Value;
            playerItem.SlotName.Value = asset.Description;
            playerItem.MaxFrames.Value = asset.Player.FrameCount();
        }

        public void ToggleAnimationPausePlay()
        {
            foreach (var item in _assetList)
            {
                if (item.Player.IsPlaying)
                    Pause(item);
                else
                    Play(item);
            }
        }

        public void SetAnimationNextFrame() => _assetList.ForEach(x => NextFrame(x));
        public void SetAnimationPrivFrame() => _assetList.ForEach(x => PrivFrame(x));
        public void SetAnimationFirstFrame() => _assetList.ForEach(x => SetFrame(x, 0));

        public void SetAnimationLastFrame()
        {
            LoopAnimation.Value = false;
            foreach (var item in _assetList)
                SetFrame(item, SelectedMainAnimation.Asset.Player.FrameCount());
        }

        private void OnAnimationFrameChanged(int currentFrame)
        {
            SelectedAnimationFrameCount.Value = SelectedMainAnimation.Asset.Player.FrameCount();
            SelectedAnimationCurrentFrame.Value = currentFrame;
            SelectedAnimationCurrentTime.Value = (float)SelectedMainAnimation.Asset.Player.GetTimeUs() / 1_000_000;
            SelectedAnimationMaxTime.Value = (float)SelectedMainAnimation.Asset.Player.GetAnimationLengthUs() / 1_000_000;
            SelectedAnimationFps.Value = SelectedMainAnimation.Asset.Player.GetFps();

            if (SelectedAnimationCurrentFrame.Value + 1 == SelectedMainAnimation.Asset.Player.FrameCount())
            {
                if (LoopAnimation.Value)
                {
                    SetAnimationFirstFrame();
                    ToggleAnimationPausePlay();
                }
            }
        }

        private void OnAnimationPlayerEnabled(bool isEnabled)
        {
            if (isEnabled)
            {
                SelectedAnimationFrameCount.Value = 0;
                if (SelectedMainAnimation != null)
                    SelectedAnimationFrameCount.Value = SelectedMainAnimation.MaxFrames.Value;

                _assetList.ForEach(x => Play(x));
            }
            else
            {
                _assetList.ForEach(x => Stop(x));
            }
        }

        void Play(SceneObject asset)
        {
            asset.Player.CurrentFrame = 0;
            asset.Player.Play();

            foreach (var metaItem in asset.MetaDataItems)
            {
                if (metaItem.Player != null)
                {
                    metaItem.Player.CurrentFrame = 0;
                    metaItem.Player.Play();
                }
            }
        }

        void Pause(SceneObject asset)
        {
            asset.Player.Pause();
            foreach (var metaItem in asset.MetaDataItems)
                metaItem.Player?.Pause();
        }

        void Stop(SceneObject asset)
        {
            asset.Player.Stop();
            foreach (var metaItem in asset.MetaDataItems)
                metaItem.Player?.Stop();
        }

        void SetFrame(SceneObject asset, int newFrame)
        {
            asset.Player.Pause();
            asset.Player.CurrentFrame = newFrame;

            foreach (var metaItem in asset.MetaDataItems)
            {
                if (metaItem.Player != null)
                {
                    metaItem.Player.Pause();
                    metaItem.Player.CurrentFrame = newFrame;
                }
            }
        }

        void NextFrame(SceneObject asset)
        {
            asset.Player.Pause();
            asset.Player.CurrentFrame++;

            foreach (var metaItem in asset.MetaDataItems)
            {
                if (metaItem.Player != null)
                {
                    metaItem.Player.Pause();
                    metaItem.Player.CurrentFrame = asset.Player.CurrentFrame;
                }
            }
        }

        void PrivFrame(SceneObject asset)
        {
            asset.Player.Pause();
            asset.Player.CurrentFrame--;

            foreach (var metaItem in asset.MetaDataItems)
            {
                if (metaItem.Player != null)
                {
                    metaItem.Player.Pause();
                    metaItem.Player.CurrentFrame = asset.Player.CurrentFrame;
                }
            }
        }

        public class AssetPlayerItem : NotifyPropertyChangedImpl
        {
            public SceneObject Asset { get; init; }
            public NotifyAttr<string> SlotName { get; internal set; } = new();
            public NotifyAttr<int> MaxFrames { get; internal set; } = new();
            public NotifyAttr<string> AnimationName { get; internal set; } = new();
        }
    }
}
