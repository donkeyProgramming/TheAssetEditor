// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Collections.Generic;
using AssetManagement.GenericFormats.DataStructures.Unmanaged;
using AssetManagement.AnimationProcessor;
using Shared.GameFormats.RigidModel.Vertex;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.Animation;

namespace AssetManagement.AssetBuilders
{
    /// <summary>
    /// Creats per-vers inclueces from RMV2 weights
    /// so weights are a unested array, easy to transer/copy
    /// </summary>
    class VertexWeightCreator
    {
        private readonly AnimationFile _skeletonFile;
        private readonly RmvModel _inModel;
        private readonly List<ExtVertexWeight> _vertexTempVertexWeights = new List<ExtVertexWeight>();

        private VertexWeightCreator() { }
        public VertexWeightCreator(RmvModel inModel, AnimationFile skeletonFile)
        {
            _inModel = inModel;
            _skeletonFile = skeletonFile;
        }

        public List<ExtVertexWeight> CreateVertexWeigts()
        {
            AddVertexWeights();

            return _vertexTempVertexWeights;
        }

        private void AddVertexWeights()
        {
            for (int vertexBufferIndex = 0; vertexBufferIndex < _inModel.Mesh.VertexList.Length; vertexBufferIndex++)
            {
                AddCornerWeights(_inModel.Mesh.VertexList[vertexBufferIndex], (uint)vertexBufferIndex);
            }
        }

        private void AddCornerWeights(CommonVertex inVertex, uint newVertexIndex)
        {
            // add as many weights as is stored in the RMVmodel vertex, 
            for (uint weightIndex = 0; weightIndex < inVertex.WeightCount; weightIndex++)
            {
                // CA Rule: Duplicate bone indices are "illegal", and are used as "terminators" to indicate weight count                               
                if (weightIndex > 0 && inVertex.BoneIndex[0] == inVertex.BoneIndex[weightIndex])
                {
                    continue; // skip these as they will cause problems in the SKD, as they add duplicate bones/vertex
                }

                if (inVertex.BoneIndex[weightIndex] == 0.0f)
                {
                    continue; // avoid null weight, as then also cause issues also cause issues
                }

                var vertexWeight = new ExtVertexWeight()
                {
                    vertexIndex = newVertexIndex,
                    boneName = SkeletonHelper.GetBoneNameFromId(_skeletonFile, inVertex.BoneIndex[weightIndex]),                    
                    boneIndex = inVertex.BoneIndex[weightIndex],
                    weight = inVertex.BoneWeight[weightIndex],
                };

                _vertexTempVertexWeights.Add(vertexWeight);
            };
        }
    }
}
