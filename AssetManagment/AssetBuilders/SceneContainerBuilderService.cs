using System.Collections.Generic;
using AssetManagement.GenericFormats.DataStructures.Unmanaged;
using AssetManagement.GenericFormats.DataStructures.Managed;
using CommonControls.FileTypes.RigidModel;
using System.Linq;
using CommonControls.FileTypes.RigidModel.Vertex;
using CommonControls.FileTypes.Animation;

namespace AssetManagement.AssetBuilders
{
    // TODO: finish this class (export)

    /// <summary>
    /// Builds a "SceneContainer" from an RMV2 + ANIM skeleton + ANIM animations
    ///
    /// </summary>
    public class SceneContainerBuilderService
    {
        private readonly RmvFile _inRMV2File;
        private readonly AnimationFile _inputSkeletonFile;
        private readonly AnimationFile _inputAnimationFile;

        public SceneContainerBuilderService(RmvFile inRMV2File, AnimationFile skeleton, AnimationFile animation = null)
        {
            _inRMV2File = inRMV2File;
            _inputSkeletonFile = skeleton;
            _inputAnimationFile = animation;
        }

        public SceneContainer BuildScene()
        {
            var newScene = new SceneContainer();

            var meshBuilderService = new SceneMeshBuilderService(_inRMV2File);
            newScene.Meshes = meshBuilderService.BuildMeshes();

            return newScene;
        }
    }

    public class SceneMeshBuilderService
    {
        private readonly RmvFile _inputRMV2File;
        public SceneMeshBuilderService(RmvFile inputRMV2File)
        {
            _inputRMV2File = inputRMV2File;
        }

        public List<PackedMesh> BuildMeshes()
        {
            if (!_inputRMV2File.ModelList.Any())
                return null;

            var meshList = new List<PackedMesh>();

            foreach (var model in _inputRMV2File.ModelList[0]) // use only LOD 0, for now
            {
                var outMesh = MakeUnindexedMesh(model);
                meshList.Add(outMesh);
            }

            return meshList;
        }

        private static PackedMesh MakeUnindexedMesh(RmvModel model)
        {
            var outMesh = new PackedMesh();

            for (var i = 0; i < model.Mesh.IndexList.Length; i += 3)
            {
                MakeTriangle(model, outMesh, i);
            }

            return outMesh;
        }

        private static void MakeTriangle(RmvModel model, PackedMesh outMesh, int triangleIndex)
        {
            var faceCornerIndex1 = model.Mesh.IndexList[triangleIndex * 3 + 0];
            var faceCornerIndex2 = model.Mesh.IndexList[triangleIndex * 3 + 1];
            var faceCornerIndex3 = model.Mesh.IndexList[triangleIndex * 3 + 2];

            var cornerVertex1 = GetPackedCommonVertex(model.Mesh.VertexList[faceCornerIndex1]);
            var cornerVertex2 = GetPackedCommonVertex(model.Mesh.VertexList[faceCornerIndex2]);
            var cornerVertex3 = GetPackedCommonVertex(model.Mesh.VertexList[faceCornerIndex3]);

            outMesh.Vertices.Add(cornerVertex1);
            outMesh.Vertices.Add(cornerVertex2);
            outMesh.Vertices.Add(cornerVertex3);
        }

        private static ExtPackedCommonVertex GetPackedCommonVertex(CommonVertex inVertex)
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

    // TODO:
    // public class SceneWeightingBuilderService
    // public class SceneAnimationBuilderService
}
