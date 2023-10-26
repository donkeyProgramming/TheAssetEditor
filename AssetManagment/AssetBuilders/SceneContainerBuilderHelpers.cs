using AssetManagement.GenericFormats.DataStructures.Unmanaged;
using CommonControls.FileTypes.Animation;
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace AssetManagement.AssetBuilders
{
    internal static class SceneContainerBuilderHelpers
    {
        internal static XMFLOAT4 GetBoneRotation(AnimationFile skeletonFile, int boneIndex)
        {
            var localRotation = new XMFLOAT4();
            localRotation.x = skeletonFile.AnimationParts[0].DynamicFrames[0].Quaternion[boneIndex].X;
            localRotation.y = skeletonFile.AnimationParts[0].DynamicFrames[0].Quaternion[boneIndex].Y;
            localRotation.z = skeletonFile.AnimationParts[0].DynamicFrames[0].Quaternion[boneIndex].Z;
            localRotation.w = skeletonFile.AnimationParts[0].DynamicFrames[0].Quaternion[boneIndex].W;

            return localRotation;
        }

        internal static XMFLOAT3 GetBoneTranslation(AnimationFile skeletonFile, int boneIndex)
        {
            var localTranslation = new XMFLOAT3();
            localTranslation.x = skeletonFile.AnimationParts[0].DynamicFrames[0].Transforms[boneIndex].X;
            localTranslation.y = skeletonFile.AnimationParts[0].DynamicFrames[0].Transforms[boneIndex].Y;
            localTranslation.z = skeletonFile.AnimationParts[0].DynamicFrames[0].Transforms[boneIndex].Z;

            return localTranslation;
        }
    }
}
