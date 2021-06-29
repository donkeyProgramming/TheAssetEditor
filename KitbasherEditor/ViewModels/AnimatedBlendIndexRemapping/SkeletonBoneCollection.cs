using Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace KitbasherEditor.ViewModels.AnimatedBlendIndexRemapping
{
    public delegate void BoneSelectedDelegate(AnimatedBone bone);
    public class SkeletonBoneCollection : NotifyPropertyChangedImpl
    {
        ObservableCollection<AnimatedBone> _bones;
        public event BoneSelectedDelegate BoneSelected;
        public ObservableCollection<AnimatedBone> Bones
        {
            get { return _bones; }
            set { SetAndNotify(ref _bones, value); VisibleBones = Bones; UpdateFilter(); SelectedBone = _bones.FirstOrDefault(); }
        }

        ObservableCollection<AnimatedBone> _visibleBones;
        public ObservableCollection<AnimatedBone> VisibleBones
        {
            get { return _visibleBones; }
            set { SetAndNotify(ref _visibleBones, value); }
        }

        AnimatedBone _selectedBone;
        public AnimatedBone SelectedBone
        {
            get { return _selectedBone; }
            set
            {
                SetAndNotify(ref _selectedBone, value);
                BoneSelected?.Invoke(_selectedBone);
            }
        }

        string _filterText = string.Empty;
        public string FilterText
        {
            get { return _filterText; }
            set
            {
                SetAndNotify(ref _filterText, value);
                UpdateFilter();
            }
        }

        string _skeletonName = string.Empty;
        public string SkeletonName
        {
            get { return _skeletonName; }
            set { SetAndNotify(ref _skeletonName, value); }
        }

        bool _onlyShowUsedBones = true;
        public bool OnlyShowUsedBones
        {
            get { return _onlyShowUsedBones; }
            set { SetAndNotify(ref _onlyShowUsedBones, value); UpdateFilter(); }
        }

        void UpdateFilter()
        {
            VisibleBones = FilterHelper.FilterBoneList(FilterText, OnlyShowUsedBones, Bones);
        }
    }
}
