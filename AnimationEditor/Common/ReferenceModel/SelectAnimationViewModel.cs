using Common;
using CommonControls.Services;
using FileTypes.PackFiles.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using static CommonControls.FilterDialog.FilterUserControl;
using static CommonControls.Services.SkeletonAnimationLookUpHelper;

namespace AnimationEditor.Common.ReferenceModel
{
    public class SelectAnimationViewModel : NotifyPropertyChangedImpl
    {
        PackFileService _pfs;
        AssetViewModel _data;
        SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;

        ObservableCollection<AnimationReference> _animationList = new ObservableCollection<AnimationReference>();
        public ObservableCollection<AnimationReference> AnimationsForCurrentSkeleton { get { return _animationList; } set { SetAndNotify(ref _animationList, value); } }


        AnimationReference _selectedAnimation;
        public AnimationReference SelectedAnimation { get => _data.AnimationName.Value; set { SetAndNotify(ref _selectedAnimation, value); AnimationChanged(value); } }

        public OnSeachDelegate FiterByFullPath { get { return (item, expression) => { return expression.Match(item.ToString()).Success; }; } }

        public SelectAnimationViewModel(AssetViewModel data, PackFileService pfs, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper)
        {
            _data = data;
            _pfs = pfs;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            _data.SkeletonChanged += OnSkeletonChanged;
            _data.AnimationChanged += OnAnimationChanged;
        }

        void OnAnimationChanged(View3D.Animation.AnimationClip newValue)
        {
            NotifyPropertyChanged(nameof(SelectedAnimation));
        }

        void OnSkeletonChanged(View3D.Animation.GameSkeleton newValue)
        {
            SkeletonChanged(_data.SkeletonName.Value);
        }

        void SkeletonChanged(string selectedSkeletonPath)
        {
            if (!string.IsNullOrWhiteSpace(selectedSkeletonPath))
            {
                var skeletonPackFile = _pfs.FindFile(selectedSkeletonPath) as PackFile;
                if (skeletonPackFile != null)
                {
                    AnimationsForCurrentSkeleton = _skeletonAnimationLookUpHelper.GetAnimationsForSkeleton(Path.GetFileNameWithoutExtension(skeletonPackFile.Name));
                    _data.SetSkeleton(skeletonPackFile);
                    return;
                }
            }

            _data.SetSkeleton(null);
            AnimationsForCurrentSkeleton = new ObservableCollection<AnimationReference>();
        }

        void AnimationChanged(AnimationReference animationReference)
        {
            if (animationReference != null)
                _data.SetAnimation(animationReference);
            else
                _data.SetAnimation(null);
        }
    }
}
