// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Shared.GameFormats.Animation;

namespace AssetManagement.AnimationProcessor
{
    public class SkeletonHelper
    {
        /// <summary>
        /// Get .ANIM bone name associate with RMV2 vertex bone id/index
        /// </summary>        
        static public int GetIdFromBoneName(AnimationFile skeleton, string name)
        {
            var boneInfo = skeleton.Bones
                .Where(x => string.Compare(x.Name, name, StringComparison.InvariantCultureIgnoreCase) == 0)
                .FirstOrDefault();

            return (boneInfo == null) ? -1 : boneInfo.Id;
        }

        /// <summary>
        /// Safe method for looking up bones, as opposed to using the bone index directly, avoid accidental overflow
        /// </summary>
        static public string GetBoneNameFromId(AnimationFile skeleton, int boneIndex)
        {
            return (boneIndex >= skeleton.Bones.Length) ? "" : skeleton.Bones[boneIndex].Name;
        }

        /// <summary>
        /// Determines if the file contains a skeleton (or an anim clip)
        /// </summary>        
        static public bool IsFileSkeleton(AnimationFile skeletonFile)
        {
            var boneCount = skeletonFile.Bones.Length;

            if (!skeletonFile.AnimationParts.Any() || !skeletonFile.AnimationParts[0].DynamicFrames.Any())
            {
                return false;
            }

            var frame0 = skeletonFile.AnimationParts[0].DynamicFrames[0];

            // a skeleton has geomtry data for all bones at frame 0
            if (frame0.Quaternion.Count == frame0.Transforms.Count && boneCount == frame0.Quaternion.Count)
            {
                return true;
            }

            return false;
        }
    }
}
