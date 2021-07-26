using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonControls.Editors.BoneMapping
{
    public static class BoneMappingHelper
    {
        public static void AutomapDirectBoneLinksBasedOnNames(AnimatedBone boneToGetMapping, IEnumerable<AnimatedBone> externalBonesList)
        {
            var otherBone = FindBoneBasedOnName(boneToGetMapping.Name, externalBonesList);
            if (otherBone == null)
                otherBone = FindBoneBasedOnNameCommonReplacements(boneToGetMapping.Name, externalBonesList);

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

        static AnimatedBone FindBoneBasedOnNameCommonReplacements(string name, IEnumerable<AnimatedBone> boneList)
        {
            foreach (var bone in boneList)
            {
                // Try adding _0
                if (name + "_0" == bone.Name)
                    return bone;
                if (name == bone.Name + "_0")
                    return bone;

                // Try removing _0
                if (name.Contains("_0"))
                {
                    if (name.Replace("_0", "") == bone.Name)
                        return bone;
                }

                if (bone.Name.Contains("_0"))
                {
                    if (name == bone.Name.Replace("_0", ""))
                        return bone;
                }

                if (IsRepalcement(bone.Name, name, "arm_left_0", "upperarm_left"))
                    return bone;

                if (IsRepalcement(bone.Name, name, "arm_left_1", "lowerarm_left"))
                    return bone;

                if (IsRepalcement(bone.Name, name, "arm_left_2", "hand_left"))
                    return bone;

                if (IsRepalcement(bone.Name, name, "arm_left_0_roll_0", "upperarm_roll_left_0"))
                    return bone;

                if (IsRepalcement(bone.Name, name, "arm_left_1_roll_0", "lowerarm_roll_left_0"))
                    return bone;

                if (IsRepalcement(bone.Name, name, "leg_left_0", "upperleg_left"))
                    return bone;

                if (IsRepalcement(bone.Name, name, "leg_left_1", "lowerleg_left"))
                    return bone;

                if (IsRepalcement(bone.Name, name, "leg_left_2 ", "foot_left"))
                    return bone;

                if (IsRepalcement(bone.Name, name, "lowerarm_left_roll ", "lowerarm_roll_left"))
                    return bone;

                if (IsRepalcement(bone.Name, name, "upperarm_left_roll ", "upperarm_roll_left"))
                    return bone;

                var result = FindBoneBasedOnNameCommonReplacements(name, bone.Children);
                if (result != null)
                    return result;
            }

            return null;
        }

        static bool IsRepalcement(string boneNameA, string boneNameB, string replacementA, string replacementB)
        {
            if (boneNameA == replacementA && boneNameB == replacementB)
                return true;

            if (boneNameA == replacementB && boneNameB == replacementA)
                return true;

            replacementA = replacementA.Replace("left", "right");
            replacementB = replacementB.Replace("left", "right");

            if (boneNameA == replacementA && boneNameB == replacementB)
                return true;

            if (boneNameA == replacementB && boneNameB == replacementA)
                return true;

            return false;
        }
    }
}
