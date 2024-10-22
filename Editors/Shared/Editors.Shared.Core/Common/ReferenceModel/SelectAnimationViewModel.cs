using System.Collections.ObjectModel;
using System.IO;
using Editors.Shared.Core.Services;
using GameWorld.Core.Animation;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using static CommonControls.FilterDialog.FilterUserControl;
using static Editors.Shared.Core.Services.SkeletonAnimationLookUpHelper;

namespace Editors.Shared.Core.Common.ReferenceModel
{
    public class SelectAnimationViewModel : NotifyPropertyChangedImpl
    {
        private readonly PackFileService _pfs;
        private readonly SceneObjectEditor _assetViewModelEditor;
        private readonly SceneObject _data;
        private readonly SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;

        ObservableCollection<AnimationReference> _animationList = new();
        public ObservableCollection<AnimationReference> AnimationsForCurrentSkeleton { get { return _animationList; } set { SetAndNotify(ref _animationList, value); } }


        AnimationReference _selectedAnimation;
        public AnimationReference SelectedAnimation { get => _data.AnimationName.Value; set { SetAndNotify(ref _selectedAnimation, value); AnimationChanged(value); } }

        public OnSeachDelegate FiterByFullPath { get { return (item, expression) => { return expression.Match(item.ToString()).Success; }; } }

        public SelectAnimationViewModel(SceneObjectEditor assetViewModelEditor, SceneObject data, PackFileService pfs, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper)
        {
            _assetViewModelEditor = assetViewModelEditor;
            _data = data;
            _pfs = pfs;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _data.SkeletonChanged += OnSkeletonChanged;
            _data.AnimationChanged += OnAnimationChanged;
        }

        void OnAnimationChanged(AnimationClip newValue)
        {
            NotifyPropertyChanged(nameof(SelectedAnimation));
        }

        void OnSkeletonChanged(GameSkeleton newValue)
        {
            SkeletonChanged(_data.SkeletonName.Value);
        }

        void SkeletonChanged(string selectedSkeletonPath)
        {
            if (!string.IsNullOrWhiteSpace(selectedSkeletonPath))
            {
                var skeletonPackFile = _pfs.FindFile(selectedSkeletonPath);
                if (skeletonPackFile != null)
                {
                    AnimationsForCurrentSkeleton = _skeletonAnimationLookUpHelper.GetAnimationsForSkeleton(Path.GetFileNameWithoutExtension(skeletonPackFile.Name));
                    _assetViewModelEditor.SetSkeleton(_data, skeletonPackFile);

                    return;
                }
            }
            _assetViewModelEditor.SetSkeleton(_data, null);
            AnimationsForCurrentSkeleton = new ObservableCollection<AnimationReference>();
        }

        void AnimationChanged(AnimationReference animationReference)
        {
            if (animationReference != null)
                _assetViewModelEditor.SetAnimation(_data, animationReference);
            else
                _assetViewModelEditor.SetAnimation(_data, null);
        }
    }
}
