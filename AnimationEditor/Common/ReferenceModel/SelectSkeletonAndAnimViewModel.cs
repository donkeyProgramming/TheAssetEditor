using Common;
using CommonControls.Services;
using Filetypes.RigidModel;
using FileTypes.PackFiles.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using View3D.Animation;
using View3D.Utility;
using static CommonControls.FilterDialog.FilterUserControl;

namespace AnimationEditor.Common.ReferenceModel
{
    public class SelectSkeletonAndAnimViewModel : NotifyPropertyChangedImpl
    {
        PackFileService _pfs;
        AssetViewModel _data;
        SkeletonAnimationLookUpHelper _skeletonAnimationLookUpHelper;

        ObservableCollection<string> _animationList = new ObservableCollection<string>();
        public ObservableCollection<string> AnimationsForCurrentSkeleton { get { return _animationList; } set { SetAndNotify(ref _animationList, value); } }

        ObservableCollection<string> _skeletonList = new ObservableCollection<string>();
        public ObservableCollection<string> SkeletonList { get { return _skeletonAnimationLookUpHelper.SkeletonFileNames; } set { SetAndNotify(ref _skeletonList, value); } }


        string _skeletonName;
        public string SkeletonName { get => _data.SkeletonName; set { SetAndNotify(ref _skeletonName, value); SkeletonChanged(value); } }


        string _selectedAnimation;
        public string SelectedAnimation { get => _data.AnimationName; set { SetAndNotify(ref _selectedAnimation, value); AnimationChanged(value); } }

        public OnSeachDelegate FiterByFullPath { get { return (item, expression) => { return expression.Match(item.ToString()).Success; }; } }


        public SelectSkeletonAndAnimViewModel(AssetViewModel data, PackFileService pfs, SkeletonAnimationLookUpHelper skeletonAnimationLookUpHelper)
        {
            _data = data;
            _pfs = pfs;
            _skeletonAnimationLookUpHelper = skeletonAnimationLookUpHelper;
            //SkeletonList = ;

            _data.PropertyChanged += _data_PropertyChanged;
        }

        private void _data_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged(nameof(SkeletonName));
            NotifyPropertyChanged(nameof(SelectedAnimation));

            if(e.PropertyName == nameof(_data.SkeletonName))
                SkeletonChanged(SkeletonName);
        }

        private void SkeletonChanged(string selectedSkeletonPath)
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

            _data.Skeleton = null;
            AnimationsForCurrentSkeleton = new ObservableCollection<string>();
        }

        private void AnimationChanged(string selectedAnimationPath)
        {
            if (string.IsNullOrWhiteSpace(selectedAnimationPath) == false)
            {
                var animFile = _pfs.FindFile(selectedAnimationPath) as PackFile;
                _data.SetAnimation(animFile);
            }
            else
            {
                _data.SetAnimation(null);
            }
        }
    }
}
