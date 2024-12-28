using System.Collections.ObjectModel;
using Shared.GameFormats.Animation;
using Shared.Ui.Editors.BoneMapping;

namespace Editors.AnimatioReTarget.Editor.BoneHandling
{
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
                if (mapping == null)
                    continue;

                var mappedValue = mapping.MappedBoneIndex.Value;

                boneNode.MappedIndex = mappedValue;
                boneNode.HasMapping = mappedValue != -1;
                ApplyMapping(boneNode.Children, mappingConfiguration);
            }
        }

        public static int CountMappedBones(ObservableCollection<SkeletonBoneNode_new> skeletonNodes)
        {
            var totalMappedCount = 0;
            foreach (var boneNode in skeletonNodes)
            {
                if (boneNode.HasMapping)
                    totalMappedCount++;

                var mappedChildren = CountMappedBones(boneNode.Children);
                totalMappedCount += mappedChildren;
            }

            return totalMappedCount;
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


        public static SkeletonBoneNode_new? GetNodeFromName(string boneName, IEnumerable<SkeletonBoneNode_new> boneList)
        {
            foreach (var bone in boneList)
            {
                if (bone.BoneName == boneName)
                    return bone;

                var result = GetNodeFromName(boneName, bone.Children);
                if (result != null)
                    return result;
            }

            return null;
        }


    }
}
