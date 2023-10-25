using AssetManagement.GenericFormats;
using AssetManagement.GenericFormats.DataStructures.Managed;
using AssetManagement.GenericFormats.DataStructures.Unmanaged;
using AssetManagement.Strategies.Fbx.DllDefinitions;
using CommonControls.FileTypes.Animation;
//using CommonControls.FileTypes.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace AssetManagement.Marshalling
{
    public class SceneMarshaller
    {
        public static SceneContainer CopyToManaged(IntPtr ptrFbxSceneContainer)
        {
            var fileInfo = FBXSceneContainerDll.GetFileInfo(ptrFbxSceneContainer);
            var fileInfoStruct = Marshal.PtrToStructure<ExtFileInfoStruct>(fileInfo);

            var newScene = new SceneContainer();
            newScene.FileInfoData.FillFromStruct(in fileInfoStruct);
            newScene.Meshes = GetAllPackedMeshes(ptrFbxSceneContainer);
            newScene.SkeletonName = GetSkeletonNameFromSceneContainer(ptrFbxSceneContainer);

            return newScene;
          
        }



        //public static void SetVertices(IntPtr fbxContainer, int meshIndex, ExtPackedCommonVertex[] vertices)
        //{
        //    //FBXSceneContainerDll.SetVertices(fbxContainer, meshIndex, vertices, vertices.Length);
        //    FBXSceneContainerDll.AllocateIndices(meshIndex, vertices);

        //    var ptrVertives = FBXSceneContainerDll.GetVertices(fbxContainer, meshIndex, out var indexCount);

        //}
        static public void CopyToNative(IntPtr ptrNativeceneContainer, SceneContainer sceneContainer, AnimationFile skeletonFile = null)
        {
            CopyPackedMeshesToNative(ptrNativeceneContainer, sceneContainer.Meshes);
            SetBones(ptrNativeceneContainer, sceneContainer.Bones);

            if (sceneContainer.SkeletonName != null && sceneContainer.SkeletonName != "")
            {
                FBXSceneContainerDll.SetSkeletonName(ptrNativeceneContainer, sceneContainer.SkeletonName);
            }
            else
            {
                FBXSceneContainerDll.SetSkeletonName(ptrNativeceneContainer, "");
            }
            // TODO: add:
            // - weights
            // - animations
            // - etc
        }

        private static void CopyPackedMeshesToNative(IntPtr ptrNativeceneContainer, List<PackedMesh> packedMeshes)
        {
            FBXSceneContainerDll.AllocateMeshes(ptrNativeceneContainer, packedMeshes.Count);                      

            for (int meshIndex = 0; meshIndex < packedMeshes.Count; meshIndex++) 
            {
                SetPackedMesh(ptrNativeceneContainer, meshIndex, packedMeshes[meshIndex]);
            }
        }

        public static ExtPackedCommonVertex[] GetVertices(IntPtr fbxContainer, int meshIndex)
        {
            var ptrVertives = FBXSceneContainerDll.GetVertices(fbxContainer, meshIndex, out var indexCount);
            var newVertexlIST = MarshalUtil.CopyArrayFromUnmanaged<ExtPackedCommonVertex>(ptrVertives, indexCount);

            return newVertexlIST;
        }

        //public static void SetIndices(IntPtr fbxContainer, int meshIndex, uint[] indices)
        //{            
        //    FBXSceneContainerDll.SetIndices(fbxContainer, meshIndex, indices, indices.Length);
        //}       

        public static uint[] GetIndices(IntPtr fbxContainer, int meshIndex)
        {
            var pIndices = FBXSceneContainerDll.GetIndices(fbxContainer, meshIndex, out var indexCount);
            var newIndexList = MarshalUtil.CopyArrayFromUnmanaged<uint>(pIndices, indexCount);

            return newIndexList;
        }
               

        public static void SetPackedMesh(IntPtr ptrFbxContainer, int meshIndex, PackedMesh packedMesh)
        {
            var ptrIndices = FBXSceneContainerDll.AllocateIndices(ptrFbxContainer, meshIndex, packedMesh.Indices.Count);
            var ptrVertices = FBXSceneContainerDll.AllocateVertices(ptrFbxContainer, meshIndex, packedMesh.Vertices.Count);            
            var ptrWeights = FBXSceneContainerDll.AllocateVertexWeights(ptrFbxContainer, meshIndex, packedMesh.VertexWeights.Count);
            FBXSceneContainerDll.SetMeshName(ptrFbxContainer, meshIndex, packedMesh.Name);

            MarshalUtil.CopyArrayToUnmanaged<uint>(packedMesh.Indices.ToArray(), ptrIndices, packedMesh.Indices.Count);
            MarshalUtil.CopyArrayToUnmanaged<ExtPackedCommonVertex>(packedMesh.Vertices.ToArray(), ptrVertices, packedMesh.Vertices.Count);            
            MarshalUtil.CopyArrayToUnmanaged<ExtVertexWeight>(packedMesh.VertexWeights.ToArray(), ptrWeights, packedMesh.VertexWeights.Count);
        }

        // TODO: finish this and clean up
        public static void SetBones(IntPtr ptrFbxContainer, List<ExtBoneInfo> bones)
        {
            var ptrBoneArrayAddress = FBXSceneContainerDll.AllocateBones(ptrFbxContainer, bones.Count);

            for (int boneIndex = 0; boneIndex < bones.Count; boneIndex++)
            {
                ExtBoneInfo boneToCopy = new ExtBoneInfo()
                {
                    id = bones[boneIndex].id,
                    parentId = bones[boneIndex].parentId,
                    name = bones[boneIndex].name,                    
                    localRotation = bones[boneIndex].localRotation,
                    localTranslation = bones[boneIndex].localTranslation,
                };

                Marshal.StructureToPtr<ExtBoneInfo>(boneToCopy, ptrBoneArrayAddress + boneIndex * Marshal.SizeOf(typeof(ExtBoneInfo)), false);
            }
        }


        public static PackedMesh GetPackedMesh(IntPtr fbxContainer, int meshIndex)
        {
            var indices = GetIndices(fbxContainer, meshIndex);
            var vertices = GetVertices(fbxContainer, meshIndex);

            var namePtr = FBXSceneContainerDll.GetMeshName(fbxContainer, meshIndex);
            var meshName = Marshal.PtrToStringUTF8(namePtr);

            if (vertices == null || indices == null || meshName == null)
                throw new Exception("Params/Input Data Invalid: Vertices, Indices or Name == null");

            var packedMesh = new PackedMesh();
            packedMesh.Vertices = new List<ExtPackedCommonVertex>();
            packedMesh.Indices = new List<uint>();
            packedMesh.Vertices.AddRange(vertices);
            packedMesh.Indices.AddRange(indices);
            packedMesh.Name = meshName;

            var tempWeights = GetExtVertexWeights(fbxContainer, meshIndex);
            packedMesh.VertexWeights = (tempWeights != null) ? tempWeights.ToList() : null;

            return packedMesh;
        }

        

        public static ExtVertexWeight[] GetExtVertexWeights(IntPtr fbxContainer, int meshIndex)
        {
            FBXSceneContainerDll.GetVertexWeights(fbxContainer, meshIndex, out var ptrVertexWeights, out var weightCount);

            if (weightCount == 0) { return null; };

            var newWeightList = MarshalUtil.CopyArrayFromUnmanaged<ExtVertexWeight>(ptrVertexWeights, weightCount);

            return newWeightList;

            // TODO: remove?
            //if (ptrVertexWeights == IntPtr.Zero || weightCount == 0)
            //{
            //    return new ExtVertexWeight[0];
            //}

            //var data = new ExtVertexWeight[weightCount];
            //for (var weightIndex = 0; weightIndex < weightCount; weightIndex++)
            //{
            //    var ptr = Marshal.PtrToStructure(ptrVertexWeights + weightIndex * Marshal.SizeOf(typeof(ExtVertexWeight)), typeof(ExtVertexWeight));

            //    if (ptr == null)
            //    {
            //        throw new Exception("Fatal Error: ptr == null");
            //    }
            //    data[weightIndex] = (ExtVertexWeight)ptr;
            //}

            //return data;
        }

        static public List<PackedMesh> GetAllPackedMeshes(IntPtr fbxSceneContainer)
        {
            var meshList = new List<PackedMesh>();
            var meshCount = FBXSceneContainerDll.GetMeshCount(fbxSceneContainer);

            for (var i = 0; i < meshCount; i++)
            {
                meshList.Add(GetPackedMesh(fbxSceneContainer, i));
            }
            return meshList;


        }

        static public string GetSkeletonNameFromSceneContainer(IntPtr ptrFbxSceneContainer)
        {
            var skeletonNamePtr = FBXSceneContainerDll.GetSkeletonName(ptrFbxSceneContainer);

            if (skeletonNamePtr == IntPtr.Zero)
                return "";

            var skeletonName = Marshal.PtrToStringUTF8(skeletonNamePtr);

            if (skeletonName == null)
                return "";

            return skeletonName;
        }

        static public string GetString(IntPtr ptrString, int length)
        {
            if (ptrString == IntPtr.Zero)
                return "";

            var skeletonName = Marshal.PtrToStringUTF8(ptrString);

            if (skeletonName == null)
                return "";

            return skeletonName;
        }
    }
}
