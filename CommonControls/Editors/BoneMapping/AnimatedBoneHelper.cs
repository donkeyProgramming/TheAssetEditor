using Filetypes.RigidModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CommonControls.Editors.BoneMapping
{
    public class AnimatedBoneHelper
    {
        public static List<IndexRemapping> BuildRemappingList(AnimatedBone bone)
        {
            var output = new List<IndexRemapping>();
            RecursiveBuildMappingList(bone, output);
            return output;
        }

        static void RecursiveBuildMappingList(AnimatedBone bone, List<IndexRemapping> output)
        {
            if (bone.MappedBoneIndex.Value != -1)
            {
                var mapping = new IndexRemapping(bone.BoneIndex.Value, bone.MappedBoneIndex.Value, bone.IsUsedByCurrentModel.Value);
                output.Add(mapping);
            }

            foreach (var child in bone.Children)
                RecursiveBuildMappingList(child, output);
        }

        public static ObservableCollection<AnimatedBone> CreateFromSkeleton(AnimationFile file, List<int> boneIndexUsedByModel = null)
        {
            var output = new ObservableCollection<AnimatedBone>();

            foreach (var boneInfo in file.Bones)
            {
                var parent = FindBoneInList(boneInfo.ParentId, output);
                var newNode = new AnimatedBone(boneInfo.Id, boneInfo.Name);
                if (boneIndexUsedByModel == null)
                    newNode.IsUsedByCurrentModel.Value = true;
                else
                    newNode.IsUsedByCurrentModel.Value = boneIndexUsedByModel.Contains((byte)boneInfo.Id);

                if (parent == null)
                    output.Add(newNode);
                else
                    parent.Children.Add(newNode);
            }
            return output;
        }

        static AnimatedBone FindBoneInList(int parentId, IEnumerable<AnimatedBone> boneList)
        {
            foreach (var bone in boneList)
            {
                if (bone.BoneIndex.Value == parentId)
                    return bone;
            }

            foreach (var bone in boneList)
            {
                var res = FindBoneInList(parentId, bone.Children);
                if (res != null)
                    return res;
            }

            return null;
        }
    }
}
