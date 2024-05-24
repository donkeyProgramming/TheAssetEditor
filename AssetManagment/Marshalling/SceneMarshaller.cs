//using CommonControls.FileTypes.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using AssetManagement.GenericFormats.DataStructures.Managed;
using AssetManagement.GenericFormats.DataStructures.Unmanaged;
using AssetManagement.Strategies.Fbx.DllDefinitions;
using Shared.GameFormats.Animation;

namespace AssetManagement.Geometry.Marshalling
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

            for (var meshIndex = 0; meshIndex < packedMeshes.Count; meshIndex++)
            {
                SetPackedMesh(ptrNativeceneContainer, meshIndex, packedMeshes[meshIndex]);
            }
        }

        public static ExtPackedCommonVertex[] GetVertices(IntPtr fbxContainer, int meshIndex)
        {
            var ptrVertives = FBXSceneContainerDll.GetVertices(fbxContainer, meshIndex, out var indexCount);
            var newVertexList = MarshalUtil.CopyArrayFromUnmanaged<ExtPackedCommonVertex>(ptrVertives, indexCount);

            return newVertexList;
        }

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

            MarshalUtil.CopyArrayToUnmanaged(packedMesh.Indices.ToArray(), ptrIndices, packedMesh.Indices.Count);
            MarshalUtil.CopyArrayToUnmanaged(packedMesh.Vertices.ToArray(), ptrVertices, packedMesh.Vertices.Count);
            MarshalUtil.CopyArrayToUnmanaged(packedMesh.VertexWeights.ToArray(), ptrWeights, packedMesh.VertexWeights.Count);
        }

        // TODO: finish this and clean up
        public static void SetBones(IntPtr ptrFbxContainer, List<ExtBoneInfo> bones)
        {
            var ptrBoneArrayAddress = FBXSceneContainerDll.AllocateBones(ptrFbxContainer, bones.Count);

            for (var boneIndex = 0; boneIndex < bones.Count; boneIndex++)
            {
                var boneToCopy = new ExtBoneInfo()
                {
                    id = bones[boneIndex].id,
                    parentId = bones[boneIndex].parentId,
                    name = bones[boneIndex].name,
                    localRotation = bones[boneIndex].localRotation,
                    localTranslation = bones[boneIndex].localTranslation,
                };

                Marshal.StructureToPtr(boneToCopy, ptrBoneArrayAddress + boneIndex * Marshal.SizeOf(typeof(ExtBoneInfo)), false);
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
            packedMesh.Name = meshName;
            packedMesh.Vertices = new List<ExtPackedCommonVertex>();
            packedMesh.Indices = new List<uint>();
            packedMesh.Vertices.AddRange(vertices);
            packedMesh.Indices.AddRange(indices);

            var tempWeights = GetExtVertexWeights(fbxContainer, meshIndex);
            packedMesh.VertexWeights = tempWeights != null ? tempWeights.ToList() : null;

            return packedMesh;
        }

        public static ExtVertexWeight[] GetExtVertexWeights(IntPtr fbxContainer, int meshIndex)
        {
            FBXSceneContainerDll.GetVertexWeights(fbxContainer, meshIndex, out var ptrVertexWeights, out var weightCount);

            if (weightCount == 0) { return null; };

            var newWeightList = MarshalUtil.CopyArrayFromUnmanaged<ExtVertexWeight>(ptrVertexWeights, weightCount);

            return newWeightList;
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
    }
}
