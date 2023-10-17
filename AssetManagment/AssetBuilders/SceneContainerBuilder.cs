
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

        public void AddMesh(RmvModel inputRMV2Mesh)
        {
            var mesh = PackedMeshBuilderHelper.MakePackedMesh(inputRMV2Mesh);

            CurrentSceneContainer.Meshes.Add(mesh);


        }

        /// <summary>
        /// Add the contents of 1 RMV2 file to scene
        /// Will be usefule "right click export file" and when export VMDs
        /// </summary>    
        public void AddMeshList(RmvFile inputRMV2File, AnimationFile skeletonFile)
        {
            var meshList = PackedMeshBuilderHelper.MakePackedMeshList(inputRMV2File, skeletonFile);

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

            var frame0QuatsCounts = frame0.Quaternion.Count;
            var frame0QTranslationCounts = frame0.Transforms.Count;


            if (frame0QuatsCounts != frame0QTranslationCounts || boneCount != frame0QuatsCounts || boneCount != frame0QTranslationCounts)
            {
                return false;
            }

            return true;
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

    public class PackedMeshBuilderHelper
    {
        static public PackedMesh MakePackedMesh(RmvModel model)
        {
            var outMesh = new PackedMesh();

            MakeUnindexedPackedMesh(model, outMesh);

            outMesh.Name = model.Material.ModelName;

            // TODO: finish iml of vertex weights
            //for (int vertexIndex = 0; vertexIndex < model.Mesh.VertexList.Length; vertexIndex++)
            //{

            //    for (int v = 0; v < model.Mesh.VertexList[vertexIndex].WeightCount; v++)
            //    {
            //        var boneIndex = model.Mesh.VertexList[vertexIndex].BoneIndex[v]


            //    }


            //}




            return outMesh;
        }

        private static void MakeUnindexedPackedMesh(RmvModel model, PackedMesh outMesh)
        {
            for (var triangleIndex = 0; triangleIndex < model.Mesh.IndexList.Length / 3; triangleIndex++)
            {
                MakeTriangle(model, outMesh, triangleIndex);
            }
        }

        static public List<PackedMesh> MakePackedMeshList(RmvFile file, AnimationFile skeletonFile)
        {
            var meshList = new List<PackedMesh>();

            foreach (var model in file.ModelList[0])
            {
                var mesh = MakePackedMesh(model);
                
                // TODO: test if this method works right
                FillVertexWeights(model, mesh, skeletonFile);


                meshList.Add(mesh);
            }

            return meshList;
        }

        static private void MakeTriangle(RmvModel model, PackedMesh outMesh, int triangleIndex)
        {
            for (int corneIndex = 0; corneIndex < 3; corneIndex++)
            {
                var faceCornerIndex = model.Mesh.IndexList[triangleIndex * 3 + corneIndex];
                var cornerVertex = GetExtPackedCommonVertex(model.Mesh.VertexList[faceCornerIndex]);                
                
                outMesh.Vertices.Add(cornerVertex);                
                outMesh.Indices.Add((ushort)outMesh.Indices.Count);
            }


            //var faceCornerIndex1 = model.Mesh.IndexList[triangleIndex * 3 + 0];
            //var faceCornerIndex2 = model.Mesh.IndexList[triangleIndex * 3 + 1];
            //var faceCornerIndex3 = model.Mesh.IndexList[triangleIndex * 3 + 2];

            //var cornerVertex1 = GetExtPackedCommonVertex(model.Mesh.VertexList[faceCornerIndex1]);
            //var cornerVertex2 = GetExtPackedCommonVertex(model.Mesh.VertexList[faceCornerIndex2]);
            //var cornerVertex3 = GetExtPackedCommonVertex(model.Mesh.VertexList[faceCornerIndex3]);


            //outMesh.Vertices.Add(cornerVertex1);
            //outMesh.Vertices.Add(cornerVertex2);
            //outMesh.Vertices.Add(cornerVertex3);

            //// add unindex indicies 0,1,2....
            
            //outMesh.Indices.Add((ushort)outMesh.Indices.Count);            
            //outMesh.Indices.Add((ushort)outMesh.Indices.Count);            
            //outMesh.Indices.Add((ushort)outMesh.Indices.Count);
        }

        static private void FillVertexWeights(RmvModel model, PackedMesh outMesh, AnimationFile skeletonFile)
        {
            var vertexIndex = 0;
            for (var triangleIndex = 0; triangleIndex < model.Mesh.IndexList.Length / 3; triangleIndex++, vertexIndex++)
            {

                for (int cornerIndex = 0; cornerIndex < 3; cornerIndex++)
                {
                    var faceCornerIndex1 = model.Mesh.IndexList[triangleIndex * 3 + cornerIndex];
                    AddCornerWeights(model, outMesh, skeletonFile, faceCornerIndex1, vertexIndex);
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

        private static void AddCornerWeights(RmvModel model, PackedMesh outMesh, AnimationFile skeletonFile, ushort faceCornerIndex1, int vertexIndex)
        {
            for (int weightIndex = 0; weightIndex < model.Mesh.VertexList[faceCornerIndex1].WeightCount; weightIndex++)
            {
                var vertexWeight = new ExtVertexWeight()
                {
                    vertexIndex = vertexIndex,
                    boneName = skeletonFile.GetBoneNameFromId(model.Mesh.VertexList[faceCornerIndex1].BoneIndex[weightIndex]),
                    boneIndex = model.Mesh.VertexList[faceCornerIndex1].BoneIndex[weightIndex],
                    weight = model.Mesh.VertexList[faceCornerIndex1].BoneWeight[weightIndex],
                };

                outMesh.VertexWeights.Add(vertexWeight);
            };
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
