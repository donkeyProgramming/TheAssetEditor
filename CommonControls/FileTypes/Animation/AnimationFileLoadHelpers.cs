// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommonControls.FileTypes.RigidModel.Transforms;

namespace CommonControls.FileTypes.Animation
{
    static class AnimationFileLoadHelpers
    {
        public static RmvVector3 Decode_translation_24_888_ranged(sbyte[] bytes, AnimationFile.AnimationV8OptimizationData optimizationData, int boneIndex)
        {
            RmvVector3 v3Normalized = new RmvVector3();
            v3Normalized.X = (float)(bytes[0]) / 127.0f;
            v3Normalized.Y = (float)(bytes[1]) / 127.0f;
            v3Normalized.Z = (float)(bytes[2]) / 127.0f;

            RmvVector3 x1 = optimizationData.Range_map_translations[boneIndex].Min;
            RmvVector3 x2 = optimizationData.Range_map_translations[boneIndex].Max;

            RmvVector3 O;
            O.X = x2.X + v3Normalized.X * x1.X;
            O.Y = x2.Y + v3Normalized.Y * x1.Y;
            O.Z = x2.Z + v3Normalized.Z * x1.Z;

            return O;
        }

        public static RmvVector4 Decode_quaternion_32_s8888_ranged(sbyte[] bytes, AnimationFile.AnimationV8OptimizationData optimizationData, int boneIndex)
        {
            var v4NormalizedValue = new RmvVector4();
            v4NormalizedValue.X = (float)(bytes[0]) / 127.0f;
            v4NormalizedValue.Y = (float)(bytes[1]) / 127.0f;
            v4NormalizedValue.Z = (float)(bytes[2]) / 127.0f;
            v4NormalizedValue.W = (float)(bytes[3]) / 127.0f;

            var quat_range1 = optimizationData.Range_map_quaternion[boneIndex].Min;
            var quat_range2 = optimizationData.Range_map_quaternion[boneIndex].Max;

            var original_quaternion = new RmvVector4();
            original_quaternion.X = quat_range2.X + v4NormalizedValue.X * quat_range1.X;
            original_quaternion.Y = quat_range2.Y + v4NormalizedValue.Y * quat_range1.Y;
            original_quaternion.Z = quat_range2.Z + v4NormalizedValue.Z * quat_range1.Z;
            original_quaternion.W = quat_range2.W + v4NormalizedValue.W * quat_range1.W;

            return original_quaternion;
        }
    }
}
