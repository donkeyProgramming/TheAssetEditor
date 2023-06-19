using CommonControls.FileTypes.Animation;
using CommonControls.Services;
using System;
using System.Collections.Generic;
using System.Text;
using Serilog;
using Accessibility;
using CommonControls.FileTypes.RigidModel;
using CommonControls.FileTypes.RigidModel.Vertex;
using Assimp.Unmanaged;
using Assimp;
using System.Net.WebSockets;

namespace CommonControls.ModelImportExport
{


    public class AssimpExporter
    {
        ILogger _logger = Serilog.Log.ForContext<AssimpExporter>();


        private Assimp.Scene _asssimpScene;
        private AnimationFile _skeletonFile;
        private PackFileService _packFileService;

        public AssimpExporter()
        {
            _asssimpScene = new Assimp.Scene();



            //_asssimpScene.










        }
        public void AddSkeletonIdNodeExporter(string skeletonOdStriog)
        {
            _asssimpScene.RootNode.Children.Insert(1, new Node("skeleton//" + skeletonOdStriog, _asssimpScene.RootNode));
        }

        public Assimp.Node AddSkeletonIdNode(Assimp.Node parent, string skeletonOdStriog)
        {
            var newNode = new Node("skeleton//" + skeletonOdStriog, _asssimpScene.RootNode);
            _asssimpScene.RootNode.Children.Insert(1, newNode);

            //newNode.GetType().Attributes().
            return newNode;
        }


        public void AdSkeletonFromAnimFIle(AnimationFile animSkeleton)
        {

            for (int i = 0; i < animSkeleton.Bones.Length; i++)
            {
                ref var bone = ref animSkeleton.Bones[i];



                if (bone.Id == AnimationFile.BoneInfo.NO_PARENT_ID)
                {

                    //AddSkeletonIdNode


                }



            }


        }


        //public void AdSkeletonFromAnimFIle(AnimationFile animSkeleton)

        //private void Add(RmvFile sourceFMRV2)
        //{


        //    _assScene.Meshes.Materials.Add(sourceFMRV2);
        //    _assScene.Materials[0] = null;
        //    _assScene.NumMaterials = 1;
        //    _assScene.Materials[0]

        //    _assScene.mMeshes = new aiMesh*[1];
        //    _assScene.mMeshes[0] = nullptr;
        //    _assScene.mNumMeshes = 1;

        //    _assScene.mMeshes[0] = new aiMesh();
        //    _assScene.mMeshes[0]->mMaterialIndex = 0;

        //    _assScene.mRootNode->mMeshes = new unsigned int[1];
        //    _assScene.mRootNode->mMeshes[0] = 0;
        //    _assScene.mRootNode->mNumMeshes = 1;


        //}

        /// <summary>
        /// Copy the values of packed vertex, into simp Values
        /// </summary>
        public void RMv2PackedVertexToAssimpVertex(
            CommonVertex v,
            Assimp.Mesh destMesh,
            int vertexIndex)
        {
            destMesh.Vertices[vertexIndex] = new Assimp.Vector3D(v.Position.X, v.Position.Y, v.Position.Z);
            destMesh.Normals[vertexIndex] = new Assimp.Vector3D(v.Normal.X, v.Normal.Y, v.Normal.Z);
            destMesh.TextureCoordinateChannels[0][vertexIndex] = new Assimp.Vector3D(v.Uv.X, v.Uv.Y, 0);
            destMesh.Tangents[vertexIndex] = new Assimp.Vector3D(v.Tangent.X, v.Tangent.Y, v.Tangent.Z);
            destMesh.BiTangents[vertexIndex] = new Assimp.Vector3D(v.BiNormal.X, v.BiNormal.Y, v.BiNormal.Z);
        }



        public Assimp.Mesh GetAsAssimpFromRMV2Mesh(RmvMesh inputMesh)
        {
            var newMesh = new Assimp.Mesh(Assimp.PrimitiveType.Triangle);

            for (int vertexIndex = 0; vertexIndex < inputMesh.VertexList.Length; vertexIndex++)
            {
                RMv2PackedVertexToAssimpVertex(inputMesh.VertexList[vertexIndex], newMesh, vertexIndex);
            }


            return newMesh;
        }






    }
}
