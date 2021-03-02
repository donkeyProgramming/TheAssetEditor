using KitbasherEditor.ViewModels.AnimatedBlendIndexRemapping;
using KitbasherEditor.ViewModels.SceneExplorerNodeViews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KitbasherEditor.Services
{
    public static class BoneMappingHelper
    {
        public static void AutomapDirectBoneLinksBasedOnNames(AnimatedBone boneToGetMapping, IEnumerable<AnimatedBone> externalBonesList)
        {
            var otherBone = FindBoneBasedOnName(boneToGetMapping.Name, externalBonesList);
            if (otherBone != null)
            {
                boneToGetMapping.MappedBoneIndex = otherBone.BoneIndex;
                boneToGetMapping.MappedBoneName = otherBone.Name;
            }

            foreach (var bone in boneToGetMapping.Children)
                AutomapDirectBoneLinksBasedOnNames(bone, externalBonesList);
        }

        public static void AutomapDirectBoneLinksBasedOnHierarchy(AnimatedBone boneToGetMapping, AnimatedBone otherBoneToStartFrom)
        {
            boneToGetMapping.MappedBoneIndex = otherBoneToStartFrom.BoneIndex;
            boneToGetMapping.MappedBoneName = otherBoneToStartFrom.Name;

            for (int i = 0; i < boneToGetMapping.Children.Count(); i++)
            {
                if (i < otherBoneToStartFrom.Children.Count())
                    AutomapDirectBoneLinksBasedOnHierarchy(boneToGetMapping.Children[i], otherBoneToStartFrom.Children[i]);
            }
        }


        public static AnimatedBone FindBoneBasedOnName(string name, IEnumerable<AnimatedBone> boneList)
        {
            foreach (var bone in boneList)
            {
                if (bone.Name == name)
                    return bone;

                var result = FindBoneBasedOnName(name, bone.Children);
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}
