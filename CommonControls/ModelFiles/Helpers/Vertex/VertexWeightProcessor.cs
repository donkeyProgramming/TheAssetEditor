using CommonControls.FileTypes.RigidModel.Vertex;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CommonControls.ModelFiles.Helpers.Vertex
{
    public class VertexWeightProcessor
    {
        /// <summary>
        /// Add 1 weight to the vertex
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

        /// <summary>
        /// Check the vertex weights have the SUM = 1.0 within a certain tolerance
        /// </summary>
        /// <exception cref="Exception">Throw on invalid weight sum</exception>
        public static void CheckVertexWeights(CommonVertex vertex, float errorTolerance = 1e-1f)
        {
            // TODO: SUM should be 1.0f or close, if more than "X" different, throw exception            
            vertex.WeightCount = 4;
            const double weightErrorTolerance = 0.1;

            float weightSum = 0.0f;
            for (int weightIndex = 0; weightIndex < vertex.WeightCount; weightIndex++)
            {
                weightSum += vertex.BoneWeight[weightIndex];
            }

            if (Math.Abs(weightSum - 1.0) > weightErrorTolerance)
            {
                throw new Exception("Weighted Error: Sum of vertex weights != 1.0. User rigging error, or import fail.");
            }
        }

        /// <summary>
        /// Sorts the weights-influences {weight, index} byte weight, so byte ascending weight
        /// </summary>        
        public static void NormalizeVertexWeights(CommonVertex vertex)
        {
            float weightSum = 0.0f;
            for (int weightIndex = 0; weightIndex < vertex.WeightCount; weightIndex++)
            {
                weightSum += vertex.BoneWeight[weightIndex];
            }

            float scaleFactor = 1 / weightSum;

            for (int weightIndex = 0; weightIndex < vertex.WeightCount; weightIndex++)
            {
                vertex.BoneWeight[weightIndex] *= scaleFactor;
            }
        }

        class VertexInfluence
        {
            public int index = 0;
            public float weight = 0.0f;
        };
        /// <summary>
        /// Normalizes the weights, so SUM = 1.0f
        /// </summary>        
        public static void SortVertexWeights(CommonVertex vertex)
        {
            List<VertexInfluence> influences = new List<VertexInfluence>(4)
                {
                    new VertexInfluence(),
                    new VertexInfluence(),
                    new VertexInfluence(),
                    new VertexInfluence(),
                };

            for (int weightIndex = 0; weightIndex < vertex.WeightCount; weightIndex++)
            {
                influences[weightIndex].index = vertex.BoneIndex[weightIndex];
                influences[weightIndex].weight = vertex.BoneWeight[weightIndex];
            }

            influences = influences.OrderByDescending(influence => influence.weight).ToList();

            for (int weightIndex = 0; weightIndex < vertex.WeightCount; weightIndex++)
            {
                vertex.BoneIndex[weightIndex] = (byte)influences[weightIndex].index;
                vertex.BoneWeight[weightIndex] = influences[weightIndex].weight;
            }
        }
    }
}
