using System;
using System.Collections.Generic;
using System.Linq;
using CommonControls.FileTypes.RigidModel.Vertex;
using CommonControls.FileTypes.Animation;
using AssetManagement.GenericFormats.DataStructures.Managed;
using CommonControls.Common;
using Serilog;

namespace AssetManagement.MeshProcessing.Common
{
    public class CommonWeightProcessor
    {    
        static private readonly ILogger _logger = Logging.Create<CommonWeightProcessor>();

        /// <summary>
        /// Add 1 weight to the vertex, does "boundary" check
        /// </summary>        
        /// <exception cref="Exception">If vertex already has 4 weights</exception>
        public static void AddWeightToVertex(CommonVertex vertex, int boneIndex, float weight)
        {
            if (vertex.WeightCount == 4)       
            {                              
                throw new Exception("Error. Trying to add more than 4 weights");
            }

            vertex.WeightCount++;
            vertex.BoneIndex[vertex.WeightCount - 1] = (byte)boneIndex;
            vertex.BoneWeight[vertex.WeightCount - 1] = weight;
        }
        public static void AddWeightToVertexByBoneName(AnimationFile skeletonFile, CommonVertex vertex, string boneName, float weight)
        {
            if (boneName.Any()) // don't add empty influences (boneName == "")
            {
                var boneIndex = skeletonFile.GetIdFromBoneName(boneName);

                if (boneIndex == AnimationFile.InvalidBoneIndex)
                {
                    _logger.Warning($"Mesh Vertex Weights, refer to a bone {boneName}, that is not in the selected skeleton.");
                    return;
                }

                AddWeightToVertex(vertex, boneIndex, weight);

            }
        }        

        /// <summary>
        /// Check the vertex weights have the SUM = 1.0 within a certain tolerance
        /// </summary>
        /// <exception cref="Exception">Throw on invalid weight sum</exception>
        public static void CheckVertexWeights(CommonVertex vertex, float errorTolerance = 1e-1f)
        {
            vertex.WeightCount = 4;
            const double weightErrorTolerance = 0.1;

            var weightSum = 0.0f;
            for (var weightIndex = 0; weightIndex < vertex.WeightCount; weightIndex++)
            {
                weightSum += vertex.BoneWeight[weightIndex];
            }

            if (Math.Abs(weightSum - 1.0) > weightErrorTolerance)
            {
                throw new Exception("Weighted Error: Sum of vertex weights != 1.0. User rigging error, or import fail.");
            }
        }
        /// <summary>
        /// Normalizes the weights, so SUM = 1.0f
        /// </summary>              
        public static void NormalizeVertexWeights(CommonVertex vertex)
        {
            var weightSum = 0.0f;
            for (var weightIndex = 0; weightIndex < vertex.WeightCount; weightIndex++)
            {
                weightSum += vertex.BoneWeight[weightIndex];
            }

            var scaleFactor = 1 / weightSum;

            for (var weightIndex = 0; weightIndex < vertex.WeightCount; weightIndex++)
            {
                vertex.BoneWeight[weightIndex] *= scaleFactor;
            }
        }

        /// <summary>
        /// Sorts the weights-influences {weight, index} byte weight, by descending weight value
        /// </summary>        
        public static void SortVertexWeightsByWeightValue(CommonVertex vertex)
        {
            var influences = new List<VertexWeight>(4)
                {
                    new VertexWeight(),
                    new VertexWeight(),
                    new VertexWeight(),
                    new VertexWeight(),
                };

            for (var weightIndex = 0; weightIndex < vertex.WeightCount; weightIndex++)
            {
                influences[weightIndex].BoneIndex = vertex.BoneIndex[weightIndex];
                influences[weightIndex].Weight = vertex.BoneWeight[weightIndex];
            }

            influences = influences.OrderByDescending(influence => influence.Weight).ToList();

            for (var weightIndex = 0; weightIndex < vertex.WeightCount; weightIndex++)
            {
                vertex.BoneIndex[weightIndex] = (byte)influences[weightIndex].BoneIndex;
                vertex.BoneWeight[weightIndex] = influences[weightIndex].BoneIndex;
            }
        }
    }
}
