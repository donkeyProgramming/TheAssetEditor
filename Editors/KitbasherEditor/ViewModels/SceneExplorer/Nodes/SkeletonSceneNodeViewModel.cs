using System.Collections.ObjectModel;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using GameWorld.Core.Animation;
using GameWorld.Core.SceneNodes;

namespace Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes
{
    public partial class SkeletonSceneNodeViewModel : ObservableObject, ISceneNodeViewModel
    {
        SkeletonNode _meshNode;

        [ObservableProperty] float _boneScale = 1;
        [ObservableProperty] int _boneCount = 0;
        [ObservableProperty] ObservableCollection<SkeletonBoneNode> _bones = new();
        [ObservableProperty] SkeletonBoneNode? _selectedBone;

        public void Initialize(ISceneNode node)
        {
            var skeletonNode = node as SkeletonNode;
            Guard.IsNotNull(skeletonNode);
            _meshNode = skeletonNode;

            CreateBoneOverview(_meshNode.Skeleton);
        }

        partial void OnBoneScaleChanged(float value) => _meshNode.SkeletonScale = value;

        partial void OnSelectedBoneChanged(SkeletonBoneNode? value)
        {
            if (value == null)
                _meshNode.SelectedBoneIndex = null;
            else
                _meshNode.SelectedBoneIndex = value.BoneIndex;
        }

        void CreateBoneOverview(GameSkeleton skeleton)
        {
            SelectedBone = null;
            Bones.Clear();
            BoneCount = 0;

            if (skeleton == null)
                return;

            BoneCount = skeleton.BoneCount;
            for (var i = 0; i < skeleton.BoneCount; i++)
            {
                var parentBoneId = skeleton.GetParentBoneIndex(i);
                if (parentBoneId == -1)
                {
                    Bones.Add(new SkeletonBoneNode(i, parentBoneId, skeleton.BoneNames[i]));
                }
                else
                {
                    var treeParent = GetParent(Bones, parentBoneId);

                    if (treeParent != null)
                        treeParent.Children.Add(new SkeletonBoneNode(i, parentBoneId, skeleton.BoneNames[i]));
                }
            }
        }

        static SkeletonBoneNode? GetParent(ObservableCollection<SkeletonBoneNode> root, int parentBoneId)
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

        public void Dispose() { }
    }


    public partial class SkeletonBoneNode : ObservableObject
    {
        [ObservableProperty] string _boneName;
        [ObservableProperty] int _boneIndex;
        [ObservableProperty] int _parentBoneIndex;
        [ObservableProperty] ObservableCollection<SkeletonBoneNode> _children = [];

        public SkeletonBoneNode(int boneId, int parentBoneId, string boneName)
        {
            BoneIndex = boneId;
            BoneName = boneName;
            ParentBoneIndex = parentBoneId;
        }   
    }

}
