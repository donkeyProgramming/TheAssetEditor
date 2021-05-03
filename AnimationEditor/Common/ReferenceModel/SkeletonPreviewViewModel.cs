using AnimationEditor.Common.ReferenceModel;
using Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using View3D.Animation;

namespace AnimationEditor.Common.ReferenceModel
{
    public class SkeletonPreviewViewModel : NotifyPropertyChangedImpl
    {
        AssetViewModel _data;

        int _boneCount = 0;
        public int BoneCount
        {
            get { return _boneCount; }
            set { SetAndNotify(ref _boneCount, value); }
        }

        public ObservableCollection<SkeletonBoneNode> Bones { get; set; } = new ObservableCollection<SkeletonBoneNode>();

        public SkeletonBoneNode _selectedBone;
        public SkeletonBoneNode SelectedBone
        {
            get { return _selectedBone; }
            set { SetAndNotify(ref _selectedBone, value); _data.SelectedBoneIndex(SelectedBone?.BoneIndex); }
        }

        public SkeletonPreviewViewModel(AssetViewModel assetViewModel)
        {
            _data = assetViewModel;
            _data.SkeletonChanged += CreateBoneOverview;
        }

        void CreateBoneOverview(GameSkeleton skeleton)
        {
            SelectedBone = null;
            Bones.Clear();
            BoneCount = 0;

            if (skeleton == null)
                return;

            BoneCount = skeleton.BoneCount;
            for (int i = 0; i < skeleton.BoneCount; i++)
            {
                var parentBoneId = skeleton.GetParentBone(i);
                if (parentBoneId == -1)
                {
                    Bones.Add(CreateNode(i, parentBoneId, skeleton.BoneNames[i]));
                }
                else
                {
                    var treeParent = GetParent(Bones, parentBoneId);

                    if (treeParent != null)
                        treeParent.Children.Add(CreateNode(i, parentBoneId, skeleton.BoneNames[i]));
                }
            }
        }

        SkeletonBoneNode CreateNode(int boneId, int parentBoneId, string boneName)
        {
            SkeletonBoneNode item = new SkeletonBoneNode
            {
                BoneIndex = boneId,
                BoneName = boneName,
                ParentBoneIndex = parentBoneId
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
    }

    public class SkeletonBoneNode : NotifyPropertyChangedImpl
    {
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
            return BoneName + "[" + BoneIndex +"]";
        }

        public ObservableCollection<SkeletonBoneNode> Children { get; set; } = new ObservableCollection<SkeletonBoneNode>();
    }
}
