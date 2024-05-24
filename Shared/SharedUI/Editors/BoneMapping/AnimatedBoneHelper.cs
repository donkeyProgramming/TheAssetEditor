// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Shared.GameFormats.Animation;

namespace Shared.Ui.Editors.BoneMapping
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

        public static ObservableCollection<AnimatedBone> CreateFlatSkeletonList(AnimationFile file)
        {
            var output = new ObservableCollection<AnimatedBone>();
            foreach (var boneInfo in file.Bones)
            {
                var newNode = new AnimatedBone(boneInfo.Id, boneInfo.Name);
                output.Add(newNode);
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

        public static int GetUsedBonesCount(AnimatedBone b)
        {
            var value = 0;
            foreach (var child in b.Children)
                value += GetUsedBonesCount(child);

            if (b.IsUsedByCurrentModel.Value)
                value = value + 1;
            return value;
        }

        public static void FilterBoneList(Regex regex, bool onlySHowUsedBones, IEnumerable<AnimatedBone> completeList)
        {
            foreach (var item in completeList)
            {
                var isVisible = IsBoneVisibleInFilter(item, onlySHowUsedBones, regex, true);
                item.IsVisible.Value = isVisible;
                if (isVisible)
                    FilterBoneList(regex, onlySHowUsedBones, item.Children);
            }
        }

        static bool IsBoneVisibleInFilter(AnimatedBone bone, bool onlySHowUsedBones, Regex regex, bool checkChildren)
        {
            var contains = regex.Match(bone.Name.Value.ToLower()).Success;

            if (onlySHowUsedBones)
            {
                if (contains && bone.IsUsedByCurrentModel.Value)
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
                    var res = IsBoneVisibleInFilter(child, onlySHowUsedBones, regex, checkChildren);
                    if (res == true)
                        return true;
                }
            }

            return false;
        }

    }
}
