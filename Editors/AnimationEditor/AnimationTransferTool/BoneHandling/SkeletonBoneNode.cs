using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Shared.GameFormats.Animation;
using Shared.Ui.BaseDialogs.MathViews;
using Shared.Ui.Editors.BoneMapping;

namespace Editors.AnimationVisualEditors.AnimationTransferTool.BoneHandling
{
    public partial class SkeletonBoneNode_new : ObservableObject
    {
        public SkeletonBoneNode_new(string boneName, int boneIndex, int parentBoneIndex)
        {
            BoneName = boneName;
            BoneIndex = boneIndex;
            ParnetBoneIndex = parentBoneIndex;
        }

        [ObservableProperty] int _boneIndex =0;
        [ObservableProperty] string _boneName  = "";
        [ObservableProperty] int _parnetBoneIndex = -1;
        [ObservableProperty] int _mappedIndex = -1;
        [ObservableProperty] bool _hasMapping = false;

        [ObservableProperty] bool _isLocalOffset = false;  // Not implemented, testing
        [ObservableProperty] float _boneLengthMult = 1;
        [ObservableProperty] Vector3ViewModel _rotationOffset = new(0, 0, 0);
        [ObservableProperty] Vector3ViewModel _translationOffset = new(0, 0, 0);

        [ObservableProperty] bool _forceSnapToWorld  = false;
        [ObservableProperty] bool _freezeTranslation = false;
        [ObservableProperty] bool _freezeRotation = false;
        [ObservableProperty] bool _freezeRotationZ  = false;
        [ObservableProperty] bool _applyTranslation  = true;
        [ObservableProperty] bool _applyRotation = true;

        [ObservableProperty] SkeletonBoneNode_new? _selectedRelativeBone = null;

        [ObservableProperty] ObservableCollection<SkeletonBoneNode_new> _children = [];
    }


    public static class SkeletonBoneNodeHelper
    {
        public static ObservableCollection<SkeletonBoneNode_new> Build(AnimationFile skeleton)
        {
            var output = new ObservableCollection<SkeletonBoneNode_new>();
            foreach (var bone in skeleton.Bones)
            {
                var newBoneNode = new SkeletonBoneNode_new(bone.Name, bone.Id, bone.ParentId);
                if (bone.ParentId == -1)
                {
                    output.Add(newBoneNode);
                    continue;
                }

                var parent = GetNodeFromId(bone.ParentId, output);
                parent.Children.Add(newBoneNode);
            }

            return output;
        }

        public static void ApplyMapping(ObservableCollection<SkeletonBoneNode_new> skeletonNodes, RemappedAnimatedBoneConfiguration mappingConfiguration)
        {
            foreach (var boneNode in skeletonNodes)
            {
                var mapping = GetNodeFromId(boneNode.BoneIndex, mappingConfiguration.MeshBones);
                var mappedValue = mapping.MappedBoneIndex.Value;

                boneNode.MappedIndex = mappedValue;
                boneNode.HasMapping = mappedValue != -1;
                ApplyMapping(boneNode.Children, mappingConfiguration);
            }
        }

        public static SkeletonBoneNode_new? GetNodeFromId(int boneIndex, IEnumerable<SkeletonBoneNode_new> boneList)
        {
            foreach (var bone in boneList)
            {
                if (bone.BoneIndex == boneIndex)
                    return bone;

                var result = GetNodeFromId(boneIndex, bone.Children);
                if (result != null)
                    return result;
            }

            return null;
        }

        public static AnimatedBone? GetNodeFromId(int boneIndex, IEnumerable<AnimatedBone> mappingList)
        {
            foreach (var bone in mappingList)
            {
                if (bone.BoneIndex.Value == boneIndex)
                    return bone;

                var result = GetNodeFromId(boneIndex, bone.Children);
                if (result != null)
                    return result;
            }

            return null;
        }

    }
}
