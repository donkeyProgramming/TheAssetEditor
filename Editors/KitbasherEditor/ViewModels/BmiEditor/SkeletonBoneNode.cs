using Shared.Core.Misc;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace KitbasherEditor.ViewModels.BmiEditor
{
    public class SkeletonBoneNode : NotifyPropertyChangedImpl
    {
        public bool IsUsedByCurrentModel { get; set; }

        bool _isChecked = true;
        public bool IsChecked
        {
            get { return _isChecked; }
            set { SetAndNotify(ref _isChecked, value); }
        }

        bool _isVisible = true;
        public bool IsVisible
        {
            get { return _isVisible; }
            set { SetAndNotify(ref _isVisible, value); }
        }

        string _boneName;
        public string BoneName
        {
            get { return _boneName; }
            set { SetAndNotify(ref _boneName, value); }
        }

        int _boneIndex;
        public int BoneIndex
        {
            get { return _boneIndex; }
            set { SetAndNotify(ref _boneIndex, value); }
        }


        int _parentBoneIndex;
        public int ParentBoneIndex
        {
            get { return _parentBoneIndex; }
            set { SetAndNotify(ref _parentBoneIndex, value); }
        }

        public override string ToString()
        {
            return BoneName + "[" + BoneIndex + "]";
        }

        public void SetCheckStatusForSelfAndChildren(bool value)
        {
            IsChecked = value;
            foreach (var child in Children)
                child.SetCheckStatusForSelfAndChildren(value);
        }

        public ObservableCollection<SkeletonBoneNode> Children { get; set; } = new ObservableCollection<SkeletonBoneNode>();

        public List<int> GetAllCheckedChildBoneIndexes()
        {
            var output = new List<int>();
            RecusrivlyBuildMappingList(this, output);
            return output;
        }

        static void RecusrivlyBuildMappingList(SkeletonBoneNode bone, List<int> output)
        {
            if (bone.IsChecked)
                output.Add(bone.BoneIndex);
            foreach (var child in bone.Children)
                RecusrivlyBuildMappingList(child, output);
        }
    }
}
