// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using AssetManagement.GenericFormats.DataStructures.Managed;
using System.Linq;
using AssetManagement.AnimationProcessor;
using Shared.GameFormats.Animation;

namespace AssetManagement.MeshProcessing.Packed
{
    public class PackedMeshProcessor
    {
        /// <summary>
        /// Checks if the bones in vertex weights exist in the skeleton
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="animationFile"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static bool CompareMeshVertexWeigtBonesToSkeleton(PackedMesh mesh, AnimationFile animationFile)
        {
            // TODO: TEST THIS
            if (mesh.VertexWeights.Any(weight => SkeletonHelper.GetIdFromBoneName(animationFile, weight.boneName) == -1))
                return false;

            return true;
        }

    }
}
