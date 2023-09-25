
using AssetManagement.GenericFormats.DataStructures.Unmanaged;
using AssetManagement.GenericFormats.DataStructures.Managed;
using CommonControls.FileTypes.RigidModel;
using CommonControls.FileTypes.RigidModel.Vertex;
using CommonControls.FileTypes.Animation;
using System.Collections.Generic;
using AssetManagement.GenericFormats;

namespace AssetManagement.AssetBuilders
{
    public class SceneContainerBuilder
    {
        private AnimationFile _skeletonFile;
        private AnimationFile _animationFile;
        private readonly SceneContainer _sceneContainer;
        private readonly SceneMeshBuilderHelper _meshBuilderHelper;

        public SceneContainerBuilder()
        {
            _sceneContainer = new SceneContainer();
            SceneMeshBuilderHelper _meshBuilderHelper = new SceneMeshBuilderHelper();
        }

        public void AddMesh(RmvModel inputRMV2Mesh)
        {
            var mesh = _meshBuilderHelper.BuildMesh(inputRMV2Mesh);

            _sceneContainer.Meshes.Add(mesh);
        }

        /// <summary>
        /// Add the contents of 1 RMV2 file to scene
        /// Will be usefule "right click export file" and when export VMDs
        /// </summary>    
        public void AddMeshes(RmvFile inputRMV2File)
        {
            var meshList = _meshBuilderHelper.BuildMeshes(inputRMV2File);

            _sceneContainer.Meshes.AddRange(meshList);
        }

        public void SetSkeleton(AnimationFile skeletonFile)
        {
            _skeletonFile = skeletonFile;
            ///....
        }

        public void SetAnimation(AnimationFile animationFile)
        {
            _animationFile = animationFile;
            ///....
        }

    }

    public class SceneMeshBuilderHelper
    {
        public PackedMesh BuildMesh(RmvModel model)
        {
            var outMesh = new PackedMesh();

            for (var i = 0; i < model.Mesh.IndexList.Length; i += 3)
            {
                MakeTriangle(model, outMesh, i);
            }

            return outMesh;
        }

        public List<PackedMesh> BuildMeshes(RmvFile file)
        {
            var meshList = new List<PackedMesh>();

            foreach (var model in file.ModelList[0])
            {
                var mesh = BuildMesh(model);
                meshList.Add(mesh);
            }

            return meshList;
        }

        private void MakeTriangle(RmvModel model, PackedMesh outMesh, int triangleIndex)
        {
            var faceCornerIndex1 = model.Mesh.IndexList[triangleIndex * 3 + 0];
            var faceCornerIndex2 = model.Mesh.IndexList[triangleIndex * 3 + 1];
            var faceCornerIndex3 = model.Mesh.IndexList[triangleIndex * 3 + 2];

            var cornerVertex1 = GetExtPackedCommonVertex(model.Mesh.VertexList[faceCornerIndex1]);
            var cornerVertex2 = GetExtPackedCommonVertex(model.Mesh.VertexList[faceCornerIndex2]);
            var cornerVertex3 = GetExtPackedCommonVertex(model.Mesh.VertexList[faceCornerIndex3]);

            outMesh.Vertices.Add(cornerVertex1);
            outMesh.Vertices.Add(cornerVertex2);
            outMesh.Vertices.Add(cornerVertex3);
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
