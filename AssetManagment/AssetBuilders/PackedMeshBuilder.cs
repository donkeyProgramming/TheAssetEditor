// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using CommonControls.FileTypes.Animation;
using CommonControls.FileTypes.RigidModel.Vertex;
using CommonControls.FileTypes.RigidModel;
using AssetManagement.GenericFormats.DataStructures.Unmanaged;
using AssetManagement.GenericFormats.DataStructures.Managed;

namespace AssetManagement.AssetBuilders
{
    public interface IPackedMeshBuilder
    {
        public PackedMesh Create(RmvModel model, AnimationFile skeletonFile);
        public List<PackedMesh> CreateList(RmvFile file, AnimationFile skeletonFile);
    }

    public class IndexedPackedMeshBuilder : IPackedMeshBuilder
    {
        public PackedMesh Create(RmvModel model, AnimationFile skeletonFile)
        {
            var outMesh = new PackedMesh() { Name = model.Material.ModelName };

            AddMesh(model, outMesh, skeletonFile);

            if (skeletonFile != null)
            {
                AddVertexWeights(model, outMesh, skeletonFile);
            }

            return outMesh;
        }

        public List<PackedMesh> CreateList(RmvFile file, AnimationFile skeletonFile)
        {
            var meshList = new List<PackedMesh>();

            foreach (var model in file.ModelList[0])
            {
                var packedMesh = Create(model, skeletonFile);
                meshList.Add(packedMesh);
            }

            return meshList;
        }

        private void AddMesh(RmvModel inMmodel, PackedMesh outMesh, AnimationFile skeletonFile)
        {
            outMesh.Vertices = new ExtPackedCommonVertex[inMmodel.Mesh.VertexList.Length].ToList();
            outMesh.Indices = new uint[inMmodel.Mesh.IndexList.Length].ToList();

            for (var indexBufferIndex = 0; indexBufferIndex < inMmodel.Mesh.IndexList.Length; indexBufferIndex++)
            {
                outMesh.Indices[indexBufferIndex] = (uint)inMmodel.Mesh.IndexList[indexBufferIndex];
            }

            for (var vertexBufferIndex = 0; vertexBufferIndex < inMmodel.Mesh.VertexList.Length; vertexBufferIndex++)
            {
                outMesh.Vertices[vertexBufferIndex] = CreateExtPackedVertex(inMmodel.Mesh.VertexList[vertexBufferIndex]);
                AddCornerWeights(inMmodel.Mesh.VertexList[vertexBufferIndex], outMesh, skeletonFile, (uint)vertexBufferIndex);
            }
        }

        private void AddVertexWeights(RmvModel inModel, PackedMesh outMesh, AnimationFile skeletonFile)
        {
            for (var vertexBufferIndex = 0; vertexBufferIndex < inModel.Mesh.VertexList.Length; vertexBufferIndex++)
            {
                AddCornerWeights(inModel.Mesh.VertexList[vertexBufferIndex], outMesh, skeletonFile, (uint)vertexBufferIndex);
            }
        }

        private static void AddCornerWeights(CommonVertex inVertex, PackedMesh outMesh, AnimationFile skeletonFile, uint newVertexIndex)
        {
            // add as many weights as is stored in the RMVmodel vertex, 
            for (uint weightIndex = 0; weightIndex < inVertex.WeightCount; weightIndex++)
            {
                // CA Rule: Duplicate bone indices are "illegal", and are used as "terminators" to indicate weight count                               
                if (weightIndex > 0 && inVertex.BoneIndex[0] == inVertex.BoneIndex[weightIndex])
                {
                    continue; // skip null weights, causes issue with the FBX SDK
                }

                var vertexWeight = new ExtVertexWeight()
                {
                    vertexIndex = newVertexIndex, 
                    boneName = skeletonFile.Bones[inVertex.BoneIndex[weightIndex]].Name, // TODO: add a GetBoneNameFromIndex() to AnimationFile for safety
                    boneIndex = inVertex.BoneIndex[weightIndex],
                    weight = inVertex.BoneWeight[weightIndex],
                };

                outMesh.VertexWeights.Add(vertexWeight);
            };
        }

        private static ExtPackedCommonVertex CreateExtPackedVertex(CommonVertex inVertex)
        {
            var outVertex = new ExtPackedCommonVertex();

            outVertex.Position.x = inVertex.Position.X;
            outVertex.Position.y = inVertex.Position.Y;
            outVertex.Position.z = inVertex.Position.Z;
            outVertex.Position.w = inVertex.Position.W;

            outVertex.Uv.x = inVertex.Uv.X;
            outVertex.Uv.y = inVertex.Uv.Y;

            outVertex.Normal.x = inVertex.Normal.X;
            outVertex.Normal.y = inVertex.Normal.Y;
            outVertex.Normal.z = inVertex.Normal.Z;

            return outVertex;
        }
    }
}
