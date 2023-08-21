using AssetManagement.GenericFormats.DataStructures.Managed;
using AssetManagement.GenericFormats.DataStructures.Unmanaged;
using AssetManagement.Strategies.Fbx.DllDefinitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace AssetManagement.Strategies.Fbx
{
    public class SceneMarshaller
    {
        public static SceneContainer ToManaged(IntPtr ptrFbxSceneContainer)
        {
            var fileInfo = FBXSCeneContainerGetterDll.GetFileInfo(ptrFbxSceneContainer);
            var fileInfoStruct = Marshal.PtrToStructure<ExtFileInfoStruct>(fileInfo);

            var newScene = new SceneContainer();
            newScene.FileInfoData.FillFromStruct(in fileInfoStruct);
            newScene.Meshes = GetAllPackedMeshes(ptrFbxSceneContainer);
            newScene.SkeletonName = GetSkeletonNameFromSceneContainer(ptrFbxSceneContainer);

            return newScene;
            /*
            TODO: to come:
            - destScene.Bones = GetAllBones();
            - destScene.Animations = GetAllBones();
            - etc, comming soon
            */
        }

        public static ExtPackedCommonVertex[] GetPackedVertices(IntPtr fbxContainer, int meshIndex)
        {
            IntPtr pVerticesPtr = IntPtr.Zero;
            int length = 0;
            FBXSCeneContainerGetterDll.GetPackedVertices(fbxContainer, meshIndex, out pVerticesPtr, out length);

            if (pVerticesPtr == IntPtr.Zero || length == 0)
            {
                return null;
            }

            ExtPackedCommonVertex[] data = new ExtPackedCommonVertex[length];
            for (int vertexIndex = 0; vertexIndex < length; vertexIndex++)
            {
                var ptr = Marshal.PtrToStructure(pVerticesPtr + vertexIndex * Marshal.SizeOf(typeof(ExtPackedCommonVertex)), typeof(ExtPackedCommonVertex));

                if (ptr != null)                
                    data[vertexIndex] = (ExtPackedCommonVertex)ptr;                                    
            }

            return data;
        }

        public static ushort[] GetIndices(IntPtr fbxContainer, int meshIndex)
        {
            IntPtr pIndices = IntPtr.Zero;
            int length = 0;
            FBXSCeneContainerGetterDll.GetIndices(fbxContainer, meshIndex, out pIndices, out length);

            if (pIndices == IntPtr.Zero || length == 0)
                return null;

            var indexArray = new ushort[length];

            for (int indicesIndex = 0; indicesIndex < length; indicesIndex++)
            {
                indexArray[indicesIndex] = (ushort)Marshal.PtrToStructure(pIndices + indicesIndex * Marshal.SizeOf(typeof(ushort)), typeof(ushort));
            }
            return indexArray;
        }

        public static PackedMesh GetPackedMesh(IntPtr fbxContainer, int meshIndex)
        {
            var indices = GetIndices(fbxContainer, meshIndex);
            var vertices = GetPackedVertices(fbxContainer, meshIndex);

            IntPtr namePtr = FBXSCeneContainerGetterDll.GetMeshName(fbxContainer, meshIndex);
            var meshName = Marshal.PtrToStringUTF8(namePtr);

            if (vertices == null || indices == null || meshName == null)
                throw new Exception("Params/Input Data Invalid: Vertices, Indices or Name == null");

            PackedMesh packedMesh = new PackedMesh();
            packedMesh.Vertices = new List<ExtPackedCommonVertex>();
            packedMesh.Indices = new List<ushort>();
            packedMesh.Vertices.AddRange(vertices);
            packedMesh.Indices.AddRange(indices);
            packedMesh.Name = meshName;
            packedMesh.VertexWeights = GetExtVertexWeights(fbxContainer, meshIndex).ToList();

            return packedMesh;
        }

        public delegate void GetArrayDelegate(IntPtr ptrSceneContainer, int meshIndex, out IntPtr ptrArray, out int itemCount);

        public static T[] GetArray<T>(
        IntPtr fbxContainer, 
        int meshIndex,
        GetArrayDelegate getArrayDelegate)
        {
            getArrayDelegate(fbxContainer, meshIndex, out var ptrArray, out var length);

            if (ptrArray == IntPtr.Zero || length == 0)
            {
                return new T[0];
            }

            T[] data = new T[length];
            for (int arrayIndex = 0; arrayIndex < length; arrayIndex++)
            {
                var ptr = Marshal.PtrToStructure(ptrArray + arrayIndex * Marshal.SizeOf(typeof(T)), typeof(T));

                if (ptr == null)
                {
                    throw new Exception("Fatal Error: ptr == null");
                }
                data[arrayIndex] = (T)ptr;
            }

            return data;
        }

        public static ExtVertexWeight[] GetExtVertexWeights(IntPtr fbxContainer, int meshIndex)
        {
            FBXSCeneContainerGetterDll.GetVertexWeights(fbxContainer, meshIndex, out var ExtVertexWeightsPtr, out var length);

            if (ExtVertexWeightsPtr == IntPtr.Zero || length == 0)
            {
                return new ExtVertexWeight[0];
            }

            ExtVertexWeight[] data = new ExtVertexWeight[length];
            for (int weightIndex = 0; weightIndex < length; weightIndex++)
            {
                var ptr = Marshal.PtrToStructure(ExtVertexWeightsPtr + weightIndex * Marshal.SizeOf(typeof(ExtVertexWeight)), typeof(ExtVertexWeight));

                if (ptr == null)
                {
                    throw new Exception("Fatal Error: ptr == null");
                }
                data[weightIndex] = (ExtVertexWeight)ptr;
            }

            return data;
        }

        static public List<PackedMesh> GetAllPackedMeshes(IntPtr fbxSceneContainer)
        {
            List<PackedMesh> meshList = new List<PackedMesh>();
            var meshCount = FBXSCeneContainerGetterDll.GetMeshCount(fbxSceneContainer);

            for (int i = 0; i < meshCount; i++)
            {
                meshList.Add(GetPackedMesh(fbxSceneContainer, i));
            }
            return meshList;
        }

        //static public string GetSkeletonNameFromSceneLoader(IntPtr ptrFbxScenLoader)
        //{
        //    var skeletonNamePtr = FBXSeneLoaderServiceDLL.GetSkeletonNameFromSceneLoader(ptrFbxScenLoader);

        //    if (skeletonNamePtr == IntPtr.Zero)
        //        return "";

        //    string skeletonName = Marshal.PtrToStringUTF8(skeletonNamePtr);

        //    if (skeletonName == null)
        //        return "";

        //    return skeletonName;
        //}
        static public string GetSkeletonNameFromSceneContainer(IntPtr ptrFbxSceneContainer)
        {
            var skeletonNamePtr = FBXSCeneContainerGetterDll.GetSkeletonName(ptrFbxSceneContainer);

            if (skeletonNamePtr == IntPtr.Zero)
                return "";

            string skeletonName = Marshal.PtrToStringUTF8(skeletonNamePtr);

            if (skeletonName == null)
                return "";

            return skeletonName;
        }
    }
}
