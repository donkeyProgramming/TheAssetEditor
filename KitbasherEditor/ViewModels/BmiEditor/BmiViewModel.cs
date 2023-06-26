using CommonControls.Common;
using CommonControls.MathViews;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using View3D.Animation;
using View3D.Commands;
using View3D.Commands.Object;
using View3D.SceneNodes;

namespace KitbasherEditor.ViewModels.BmiEditor
{
    public class BmiViewModel : NotifyPropertyChangedImpl
    {
        Rmv2MeshNode _meshNode;
        private readonly CommandFactory _commandFactory;
        GameSkeleton _skeleton;

        public ObservableCollection<SkeletonBoneNode> Bones { get; set; } = new ObservableCollection<SkeletonBoneNode>();
        public SkeletonBoneNode _selectedBone;
        public SkeletonBoneNode SelectedBone
        {
            get { return _selectedBone; }
            set { SetAndNotify(ref _selectedBone, value); CheckButtonsEnabled = value != null; }
        }


        public bool _checkButtonsEnabled = false;
        public bool CheckButtonsEnabled
        {
            get { return _checkButtonsEnabled; }
            set { SetAndNotify(ref _checkButtonsEnabled, value); }
        }

        DoubleViewModel _scaleFactor = new DoubleViewModel(0.002);
        public DoubleViewModel ScaleFactor
        {
            get { return _scaleFactor; }
            set { SetAndNotify(ref _scaleFactor, value); }
        }

        public BmiViewModel(GameSkeleton skeleton, Rmv2MeshNode meshNode,  CommandFactory commandFactory)
        {
            _commandFactory = commandFactory;
            _meshNode = meshNode;
            _skeleton = skeleton;
            CreateBoneOverview(_skeleton);
        }

        public void CheckAllChildren()
        {
            SelectedBone.SetCheckStatusForSelfAndChildren(true);
        }

        public void UnCheckAllChildren()
        {
            SelectedBone.SetCheckStatusForSelfAndChildren(false);
        }

        public void Apply()
        {
            _commandFactory.Create<GrowMeshCommand>()
                .Configure(x => x.Configure(_skeleton, _meshNode, (float)_scaleFactor.Value, Bones.First().GetAllCheckedChildBoneIndexes()))
                .BuildAndExecute();
        }

        void CreateBoneOverview(GameSkeleton skeleton)
        {
            var boneIndexsUsed = _meshNode.Geometry.GetUniqeBlendIndices();

            Bones.Clear();

            if (skeleton == null)
                return;

            for (int i = 0; i < skeleton.BoneCount; i++)
            {
                var parentBoneId = skeleton.GetParentBoneIndex(i);
                if (parentBoneId == -1)
                {
                    Bones.Add(CreateNode(i, parentBoneId, skeleton.BoneNames[i], boneIndexsUsed));
                }
                else
                {
                    var treeParent = GetParent(Bones, parentBoneId);

                    if (treeParent != null)
                        treeParent.Children.Add(CreateNode(i, parentBoneId, skeleton.BoneNames[i], boneIndexsUsed));
                }
            }

            Bones = FilterHelper.FilterBoneList("", true, Bones);
        }

        SkeletonBoneNode CreateNode(int boneId, int parentBoneId, string boneName, List<byte> usedBonesList)
        {
            SkeletonBoneNode item = new SkeletonBoneNode
            {
                BoneIndex = boneId,
                BoneName = boneName,
                ParentBoneIndex = parentBoneId,
                IsUsedByCurrentModel = usedBonesList.IndexOf((byte)boneId) != -1
            };
            return item;
        }

        SkeletonBoneNode GetParent(ObservableCollection<SkeletonBoneNode> root, int parentBoneId)
        {
            foreach (SkeletonBoneNode item in root)
            {
                if (item.BoneIndex == parentBoneId)
                    return item;

                var result = GetParent(item.Children, parentBoneId);
                if (result != null)
                    return result;
            }
            return null;
        }

        class FilterHelper
        {
            public static ObservableCollection<SkeletonBoneNode> FilterBoneList(string filterText, bool onlySHowUsedBones, ObservableCollection<SkeletonBoneNode> completeList)
            {
                var output = new ObservableCollection<SkeletonBoneNode>();
                FilterBoneListRecursive(filterText, onlySHowUsedBones, completeList, output);
                return completeList;
            }

            static void FilterBoneListRecursive(string filterText, bool onlySHowUsedBones, ObservableCollection<SkeletonBoneNode> completeList, ObservableCollection<SkeletonBoneNode> output)
            {
                foreach (var item in completeList)
                {
                    bool isVisible = IsBoneVisibleInFilter(item, onlySHowUsedBones, filterText, true);
                    item.IsVisible = isVisible;
                    if (isVisible)
                    {
                        FilterBoneListRecursive(filterText, onlySHowUsedBones, item.Children, item.Children);
                    }
                }
            }

            static bool IsBoneVisibleInFilter(SkeletonBoneNode bone, bool onlySHowUsedBones, string filterText, bool checkChildren)
            {
                var contains = bone.BoneName.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) != -1;
                if (onlySHowUsedBones)
                {
                    if (contains && bone.IsUsedByCurrentModel)
                        return contains;
                }
                else
                {
                    if (contains)
                        return contains;
                }
                if (checkChildren)
                {
                    foreach (var child in bone.Children)
                    {
                        var res = IsBoneVisibleInFilter(child, onlySHowUsedBones, filterText, checkChildren);
                        if (res == true)
                            return true;
                    }
                }

                return false;
            }
        }
    }

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
            if(bone.IsChecked)
                output.Add(bone.BoneIndex);
            foreach (var child in bone.Children)
                RecusrivlyBuildMappingList(child, output);
        }
    }
}
