// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Shared.Ui.Editors.BoneMapping
{
    public static class BoneMappingHelper
    {
        public static void AutomapDirectBoneLinksBasedOnNames(AnimatedBone boneToGetMapping, IEnumerable<AnimatedBone> externalBonesList)
        {
            var otherBone = FindBoneBasedOnName(boneToGetMapping.Name.Value, externalBonesList);
            if (otherBone == null)
                otherBone = RecursiveFindBoneBasedOnNameCommonReplacements(boneToGetMapping.Name.Value, externalBonesList);

            if (otherBone != null)
            {
                boneToGetMapping.MappedBoneIndex.Value = otherBone.BoneIndex.Value;
                boneToGetMapping.MappedBoneName.Value = otherBone.Name.Value;
            }

            foreach (var bone in boneToGetMapping.Children)
                AutomapDirectBoneLinksBasedOnNames(bone, externalBonesList);
        }

        public static void AutomapDirectBoneLinksBasedOnHierarchy(AnimatedBone boneToGetMapping, AnimatedBone otherBoneToStartFrom)
        {
            boneToGetMapping.MappedBoneIndex.Value = otherBoneToStartFrom.BoneIndex.Value;
            boneToGetMapping.MappedBoneName.Value = otherBoneToStartFrom.Name.Value;

            for (var i = 0; i < boneToGetMapping.Children.Count(); i++)
            {
                if (i < otherBoneToStartFrom.Children.Count())
                    AutomapDirectBoneLinksBasedOnHierarchy(boneToGetMapping.Children[i], otherBoneToStartFrom.Children[i]);
            }
        }


        public static AnimatedBone FindBoneBasedOnName(string name, IEnumerable<AnimatedBone> boneList)
        {
            foreach (var bone in boneList)
            {
                if (bone.Name.Value == name)
                    return bone;

                var result = FindBoneBasedOnName(name, bone.Children);
                if (result != null)
                    return result;
            }

            return null;
        }

        static AnimatedBone RecursiveFindBoneBasedOnNameCommonReplacements(string name, IEnumerable<AnimatedBone> boneList)
        {
            foreach (var bone in boneList)
            {
                // Try adding _0
                if (name + "_0" == bone.Name.Value)
                    return bone;
                if (name == bone.Name + "_0")
                    return bone;

                // Try removing _0
                if (name.Contains("_0"))
                {
                    if (name.Replace("_0", "") == bone.Name.Value)
                        return bone;
                }

                if (bone.Name.Value.Contains("_0"))
                {
                    if (name == bone.Name.Value.Replace("_0", ""))
                        return bone;
                }

                var rules = new string[][]
                {
                    new string[] { "root", "bn_hips" },
                    new string[] { "spine_0", "bn_spine" },

                    new string[] { "hand_left", "bn_lefthand" },
                    new string[] { "finger_index_left_0", "bn_lefthandindex1" },
                    new string[] { "finger_index_left_1", "bn_lefthandindex2" },
                    new string[] { "finger_index_left_2", "bn_lefthandindex3" },

                    new string[] { "finger_ring_left_0", "bn_lefthandring1" },
                    new string[] { "finger_ring_left_1", "bn_lefthandring2" },
                    new string[] { "finger_ring_left_2", "bn_lefthandring3" },

                    new string[] { "thumb_left_0", "bn_lefthandthumb1" },
                    new string[] { "thumb_left_1", "bn_lefthandthumb2" },
                    new string[] { "thumb_left_2", "bn_lefthandthumb3" },

                    new string[] { "arm_left_0", "upperarm_left", "bn_leftarm" },
                    new string[] { "arm_left_1", "lowerarm_left", "bn_leftforearm" },
                    new string[] { "arm_left_2", "hand_left" },
                    new string[] { "arm_left_0_roll_0", "upperarm_roll_left_0", "bn_leftarmroll" },
                    new string[] { "arm_left_1_roll_0", "lowerarm_roll_left_0", "bn_leftforearmroll" },
                    new string[] { "lowerarm_left_roll", "lowerarm_roll_left", "bn_leftforearmroll" },
                    new string[] { "upperarm_left_roll", "upperarm_roll_left", "bn_leftarmroll" },
                    new string[] { "shoulder_pad_left", "shoulderpad_left_0" },
                    new string[] { "clav_left", "bn_leftshoulder" },

                    new string[] { "leg_left_0", "upperleg_left", "bn_leftupleg" },
                    new string[] { "leg_left_1", "lowerleg_left", "bn_leftleg" },
                    new string[] { "leg_left_2", "foot_left", "bn_leftfoot" },
                    new string[] { "toe_left_0", "bn_lefttoebase" },

                    new string[] { "neck_0", "bn_neck" },
                    new string[] { "eye_left", "bn_lefteye" },
                    new string[] { "eyebrow", "bn_eyebrows" },

                    new string[] { "be_prop_0", "weapon_1" },
                    new string[] { "be_prop_1", "weapon_2" },
                    new string[] { "be_prop_2", "weapon_3" },
                    new string[] { "be_prop_3", "weapon_4" },
                    new string[] { "be_prop_4", "weapon_5" },
                    new string[] { "be_prop_5", "weapon_6" },

                    // Reme 2 game skeleton fix:
                    new string[] { "finger_index_left_2", "bn_lefthandindex2" },
                    new string[] { "finger_ring_left_2", "bn_lefthandring2" },
                    new string[] { "thumb_left_2", "bn_lefthandthumb2" },
                };

                foreach (var rule in rules)
                {
                    if (IsBoneNamesMatch(bone.Name.Value, name, rule))
                        return bone;
                }

                var result = RecursiveFindBoneBasedOnNameCommonReplacements(name, bone.Children);
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



        static bool IsBoneNamesMatch(string boneNameA, string boneNameB, string[] alternativeNames)
        {
            // Clean up for easier comparing 
            boneNameA = CleanUpBoneName(boneNameA);
            boneNameB = CleanUpBoneName(boneNameB);
            for (var i = 0; i < alternativeNames.Length; i++)
                alternativeNames[i] = CleanUpBoneName(alternativeNames[i]);

            if (boneNameA == boneNameB)
                return true;

            if (alternativeNames.Contains(boneNameA) && alternativeNames.Contains(boneNameB))
                return true;

            // Swap left for right
            for (var i = 0; i < alternativeNames.Length; i++)
                alternativeNames[i] = alternativeNames[i].Replace("left", "right");

            if (alternativeNames.Contains(boneNameA) && alternativeNames.Contains(boneNameB))
                return true;

            return false;
        }

        static string CleanUpBoneName(string boneName)
        {
            return boneName
                   .Replace("bn_", "")
                   .Replace("_", "");
        }
    }
}
