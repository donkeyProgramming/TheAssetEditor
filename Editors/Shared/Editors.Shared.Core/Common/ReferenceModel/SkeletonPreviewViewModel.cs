using System.Collections.ObjectModel;
using GameWorld.Core.Animation;
using Shared.Core.Misc;

namespace Editors.Shared.Core.Common.ReferenceModel
{
    public class SkeletonPreviewViewModel : NotifyPropertyChangedImpl
    {
        private readonly SceneObject _sceneObject;

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
            set { SetAndNotify(ref _selectedBone, value); _sceneObject.SelectedBoneIndex(SelectedBone?.BoneIndex); }
        }

        public SkeletonPreviewViewModel(SceneObject sceneObject)
        {
            _sceneObject = sceneObject;
            _sceneObject.SkeletonChanged += CreateBoneOverview;
        }

        void CreateBoneOverview(GameSkeleton skeleton)
        {
            SelectedBone = null;
            Bones.Clear();
            BoneCount = 0;

            if (skeleton == null)
                return;

            BoneCount = skeleton.BoneCount;
            var newBones = SkeletonBoneNodeHelper.CreateBoneOverview(skeleton);
            foreach (var bone in newBones)
                Bones.Add(bone);
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
            return BoneName + "[" + BoneIndex + "]";
        }

        public ObservableCollection<SkeletonBoneNode> Children { get; set; } = new ObservableCollection<SkeletonBoneNode>();
    }

    public class SkeletonBoneNodeHelper
    {
        public static List<SkeletonBoneNode> CreateBoneOverview(GameSkeleton skeleton)
        {
            var output = new List<SkeletonBoneNode>();

            for (var i = 0; i < skeleton.BoneCount; i++)
            {
                var parentBoneId = skeleton.GetParentBoneIndex(i);
                if (parentBoneId == -1)
                {
                    output.Add(CreateNode(i, parentBoneId, skeleton.BoneNames[i]));
                }
                else
                {
                    var treeParent = GetParent(output, parentBoneId);

                    if (treeParent != null)
                        treeParent.Children.Add(CreateNode(i, parentBoneId, skeleton.BoneNames[i]));
                }
            }

            return output;
        }

        public static ObservableCollection<SkeletonBoneNode> CreateFlatSkeletonList(GameSkeleton skeleton, int startBone = -1)
        {
            var output = new ObservableCollection<SkeletonBoneNode>();
            for (var i = 0; i < skeleton.BoneCount; i++)
            {
                if (startBone == -1 || IsIndirectChildOf(i, startBone, skeleton))
                {
                    var bone = new SkeletonBoneNode()
                    {
                        BoneIndex = i,
                        BoneName = skeleton.BoneNames[i]
                    };
                    output.Add(bone);
                }
            }
            return output;
        }

        public static bool IsIndirectChildOf(int boneIndex, int childOff, GameSkeleton skeleton)
        {
            var parentIndex = skeleton.GetParentBoneIndex(boneIndex);
            if (parentIndex == -1)
                return false;

            if (parentIndex == childOff)
                return true;

            return IsIndirectChildOf(parentIndex, childOff, skeleton);
        }

        static SkeletonBoneNode CreateNode(int boneId, int parentBoneId, string boneName)
        {
            var item = new SkeletonBoneNode
            {
                BoneIndex = boneId,
                BoneName = boneName,
                ParentBoneIndex = parentBoneId
            };
            return item;
        }

        static SkeletonBoneNode GetParent(IEnumerable<SkeletonBoneNode> root, int parentBoneId)
        {
            foreach (var item in root)
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
}
