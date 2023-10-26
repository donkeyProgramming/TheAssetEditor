
using AssetManagement.GenericFormats.DataStructures.Unmanaged;
using AssetManagement.GenericFormats.DataStructures.Managed;
using CommonControls.FileTypes.RigidModel;
using CommonControls.FileTypes.RigidModel.Vertex;
using CommonControls.FileTypes.Animation;
using System.Collections.Generic;
using AssetManagement.GenericFormats;
using System.Linq;

namespace AssetManagement.AssetBuilders
{
    public class SceneContainerBuilder
    {
        private AnimationFile _skeletonFile = null;
        private AnimationFile _animationFile = null;
        public SceneContainer CurrentSceneContainer { get; private set; }

        public SceneContainerBuilder()
        {
            CurrentSceneContainer = new SceneContainer();
        }

        public void AddMesh(RmvModel inputRMV2Mesh, AnimationFile skeletonFile)
        {
            var mesh = PackedMeshCreator.CreatePackedMesh(inputRMV2Mesh, skeletonFile);

            CurrentSceneContainer.Meshes.Add(mesh);
        }

        /// <summary>
        /// Add the contents of 1 RMV2 file to scene
        /// Will be usefule "right click export file" and when export VMDs
        /// </summary>    
        public void AddMeshList(RmvFile inputRMV2File, AnimationFile skeletonFile)
        {
            var meshList = PackedMeshCreator.MakePackedMeshList(inputRMV2File, skeletonFile);            

            CurrentSceneContainer.Meshes.AddRange(meshList);
            CurrentSceneContainer.SkeletonName = inputRMV2File.Header.SkeletonName;
        }

        /// <summary>
        /// Sets/replaces the skeleton
        /// </summary>        
        public void SetSkeleton(AnimationFile skeletonFile)
        {
            if (skeletonFile == null)
            {
                return;
            }

            _skeletonFile = skeletonFile;
            CopyBones(skeletonFile);

            ///....
            ///
        }

        /// <summary>
        /// To be a proper skeleton, frame 0 has to have geomtric info all bones
        /// </summary>
        /// <param name="skeletonFile"></param>
        /// <returns></returns>
        private bool IsFileValidSkeleton(AnimationFile skeletonFile)
        {
            var boneCount = skeletonFile.Bones.Length;

            if (!skeletonFile.AnimationParts.Any() || !skeletonFile.AnimationParts[0].DynamicFrames.Any())
            {
                return false;
            }

            var frame0 = skeletonFile.AnimationParts[0].DynamicFrames[0];

            if (frame0.Quaternion.Count == frame0.Transforms.Count && boneCount == frame0.Quaternion.Count)
            {
                return true;
            }

            return false;
        }

        private void CopyBones(AnimationFile skeletonFile)
        {
            if (!IsFileValidSkeleton(_skeletonFile))
            {
                return;
            }

            CurrentSceneContainer.Bones = new List<ExtBoneInfo>();

            for (int boneIndex = 0; boneIndex < skeletonFile.Bones.Length; boneIndex++)
            {
                var newBone = new ExtBoneInfo()
                {
                    id = skeletonFile.Bones[boneIndex].Id,
                    parentId = skeletonFile.Bones[boneIndex].ParentId,
                    name = skeletonFile.Bones[boneIndex].Name,
                    localTranslation = SceneContainerBuilderHelpers.GetBoneTranslation(skeletonFile, boneIndex),
                    localRotation = SceneContainerBuilderHelpers.GetBoneRotation(skeletonFile, boneIndex),
                };

                CurrentSceneContainer.Bones.Add(newBone);
            };
        }

        /// <summary>
        /// Set Amimation clip
        /// </summary>        
        public void SetAnimation(AnimationFile animationFile)
        {
            _animationFile = animationFile;
            ///....
        }

    }

    public class PackedMeshCreator
    {
        static public PackedMesh CreatePackedMesh(RmvModel model, AnimationFile skeletonFile)
        {
            var outMesh = new PackedMesh();
            outMesh.Name = model.Material.ModelName;

            CreateBasicPackedMesh(model, outMesh);

            if (skeletonFile != null)
            {
                AddMeshVertexWeights(model, outMesh, skeletonFile);
                
                // TODO: WAY TOO SLOw, if you have to check for  1.0 < weight < 1.0, find another way
                //      CheckForNullWeightedVertices(outMesh); 
            }

            return outMesh;
        }
        /// <summary>
        /// Converts the mesh to unindexed, so that no vertex is use more than once
        /// It means there will be 3 times as many vertices, the indices will be sequential and not really needed
        /// </summary>        
        private static void CreateBasicPackedMesh(RmvModel model, PackedMesh outMesh)
        {
            for (var triangleIndex = 0; triangleIndex < model.Mesh.IndexList.Length / 3; triangleIndex++)
            {
                MakeTriangle(model, outMesh, triangleIndex);
            }
            outMesh.Name = model.Material.ModelName;

            // TODO: is MEGA slow, if needed check for null weights another way
            //CheckForNullWeightedVertices(outMesh);
        }

        private static void AddMeshVertexWeights(RmvModel model, PackedMesh outMesh, AnimationFile skeletonFile)
        {
            int currentVertexIndex = 0;
            for (var triangleIndex = 0; triangleIndex < model.Mesh.IndexList.Length / 3; triangleIndex++)
            {
                for (int faceCorner = 0; faceCorner < 3; faceCorner++, currentVertexIndex++)
                {
                    var faceCornerIndex = model.Mesh.IndexList[triangleIndex * 3 + faceCorner];
                    var cornerVertex = GetExtPackedCommonVertex(model.Mesh.VertexList[faceCornerIndex]);

                    // adds 1-4 weights from the RMVModel vertex
                    AddCornerWeights(model.Mesh.VertexList[faceCornerIndex], outMesh, skeletonFile, currentVertexIndex);                    
                }
            }
            outMesh.Name = model.Material.ModelName;

            // TODO: is MEGA slow, if needed check for null weights another way
            //CheckForNullWeightedVertices(outMesh);
        }

        private static bool AlmostEualtoOne(float weight)
        {
            return (weight > 0.95f && weight < 1.05f) ? true : false;
        }

        private static void CheckForNullWeightedVertices(PackedMesh outMesh)
        {
            for (int testVertex = 0; testVertex < outMesh.Vertices.Count; testVertex++)
            {
                float testWeight = 0.0f;

                foreach (var vertexWeight in outMesh.VertexWeights)
                {
                    if (vertexWeight.vertexIndex == testVertex)
                    {
                        testWeight += vertexWeight.weight;
                    }
                }

                if (!AlmostEualtoOne(testWeight))
                {
                    throw new System.Exception("vertex is null-weighted");
                }
            }
        }

        static public List<PackedMesh> MakePackedMeshList(RmvFile file, AnimationFile skeletonFile)
        {
            var meshList = new List<PackedMesh>();

            foreach (var model in file.ModelList[0])
            {
                var mesh = CreatePackedMesh(model, skeletonFile);
                meshList.Add(mesh);
            }

            return meshList;
        }

        static private void MakeTriangle(RmvModel model, PackedMesh outMesh, int triangleIndex)
        {
            for (int faceCorner = 0; faceCorner < 3; faceCorner++)
            {
                var faceCornerIndex = model.Mesh.IndexList[triangleIndex * 3 + faceCorner];
                var cornerVertex = GetExtPackedCommonVertex(model.Mesh.VertexList[faceCornerIndex]);

                var currentVertexIndex = (uint)outMesh.Indices.Count;

                outMesh.Vertices.Add(cornerVertex);
                outMesh.Indices.Add(currentVertexIndex);
            }
        }

        static private void FillVertexWeights(RmvModel model, PackedMesh outMesh, AnimationFile skeletonFile)
        {
            var vertexIndex = 0;

            for (var triangleIndex = 0; triangleIndex < model.Mesh.IndexList.Length / 3; triangleIndex++)
            {

                for (int cornerIndex = 0; cornerIndex < 3; cornerIndex++)
                {
                    var faceCornerIndex = model.Mesh.IndexList[triangleIndex * 3 + cornerIndex];
                    AddCornerWeights(model.Mesh.VertexList[faceCornerIndex], outMesh, skeletonFile, vertexIndex++);
                }


                //var faceCornerIndex1 = model.Mesh.IndexList[triangleIndex * 3 + 0];
                //var faceCornerIndex2 = model.Mesh.IndexList[triangleIndex * 3 + 1];
                //var faceCornerIndex3 = model.Mesh.IndexList[triangleIndex * 3 + 2];

                //// add unindex indicies 0,1,2....
                //AddCornerWeights(model, outMesh, skeletonFile, faceCornerIndex1, vertexIndex++);
                //AddCornerWeights(model, outMesh, skeletonFile, faceCornerIndex2, vertexIndex++);
                //AddCornerWeights(model, outMesh, skeletonFile, faceCornerIndex3, vertexIndex);

            }


        }

        /// <summary>
        /// Adds the weights from 1 RmvModel vertex to the unindexed PackedMesh
        /// </summary>        
        private static void AddCornerWeights(CommonVertex inVertex, PackedMesh outMesh, AnimationFile skeletonFile, int newVertexIndex)
        {
            // add as many weights as is stored in the RMVmodel vertex, 
            for (int weightIndex = 0; weightIndex < inVertex.WeightCount; weightIndex++)
            {
                var vertexWeight = new ExtVertexWeight()
                {
                    vertexIndex = newVertexIndex,
                    boneName = skeletonFile.GetBoneNameFromIndex(inVertex.BoneIndex[weightIndex]),
                    boneIndex = inVertex.BoneIndex[weightIndex],
                    weight = inVertex.BoneWeight[weightIndex],
                };

                outMesh.VertexWeights.Add(vertexWeight);
            };
                        
            // TODO: Checker VERY SLOW easier to simply add the weights together for each RMV vertex and check?
            //  for checking that weights are assigned correct, if any vertex has weight sum != 1.0,      
            //CheckForNullWeightedVertices(outMesh);
        }

        private static ExtPackedCommonVertex GetExtPackedCommonVertex(CommonVertex inVertex)
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

    //    public class SceneSkeletonBuilder

    // TODO:
    // public class SceneWeightingBuilderService
    // public class SceneAnimationBuilderService
}
