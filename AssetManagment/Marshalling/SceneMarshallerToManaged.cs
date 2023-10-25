using AssetManagement.GenericFormats;
using AssetManagement.GenericFormats.DataStructures.Managed;
using AssetManagement.GenericFormats.DataStructures.Unmanaged;
using AssetManagement.Strategies.Fbx.DllDefinitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace AssetManagement.Strategies.Fbx
{
    public class SceneMarshallerToManaged
    {
        public static SceneContainer ToManaged(IntPtr ptrFbxSceneContainer)
        {
            var newScene = new SceneContainer();
            newScene.Meshes = GetAllPackedMeshes(ptrFbxSceneContainer);
            newScene.SkeletonName = GetSkeletonNameFromSceneContainer(ptrFbxSceneContainer);
            return newScene;
            /*
            - destScene.Bones = GetAllBones();
            - destScene.Animations = GetAllBones();
            - etc, comming soon
            */
        }

        public static ExtPackedCommonVertex[] GetVertices(IntPtr fbxContainer, int meshIndex)
        {
            IntPtr pVerticesPtr = IntPtr.Zero;
            int length = 0;
            pVerticesPtr = FBXSceneContainerDll.GetVertices(fbxContainer, meshIndex, out length);

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

        public static uint[] GetIndices(IntPtr fbxContainer, int meshIndex)
        {
            IntPtr pIndices = IntPtr.Zero;
            int length = 0;
            pIndices  = FBXSceneContainerDll.GetIndices(fbxContainer, meshIndex, out length);

            if (pIndices == IntPtr.Zero || length == 0)
                return null;

            var indexArray = new uint[length];

            for (int indicesIndex = 0; indicesIndex < length; indicesIndex++)
            {
                indexArray[indicesIndex] = (uint)Marshal.PtrToStructure(pIndices + indicesIndex * Marshal.SizeOf(typeof(uint)), typeof(uint));
            }
            return indexArray;
        }

        public static PackedMesh GetPackedMesh(IntPtr fbxContainer, int meshIndex)
        {
            var indices = GetIndices(fbxContainer, meshIndex);
            var vertices = GetVertices(fbxContainer, meshIndex);

            IntPtr namePtr = FBXSceneContainerDll.GetMeshName(fbxContainer, meshIndex);
            var tempName = Marshal.PtrToStringUTF8(namePtr);

            if (vertices == null || indices == null || tempName == null)
                throw new Exception("Params/Input Data Invalid: Vertices, Indices or Name == null");

            PackedMesh packedMesh = new PackedMesh();
            packedMesh.Vertices = new List<ExtPackedCommonVertex>();
            packedMesh.Indices = new List<uint>();
            packedMesh.Vertices.AddRange(vertices);
            packedMesh.Indices.AddRange(indices);
            packedMesh.Name = tempName;
            packedMesh.VertexWeights = GetVertexWeights(fbxContainer, meshIndex).ToList();

            return packedMesh;
        }

        public static ExtVertexWeight[] GetVertexWeights(IntPtr fbxContainer, int meshIndex)
        {
            FBXSceneContainerDll.GetVertexWeights(fbxContainer, meshIndex, out var vertexWeightsPtr, out var length);

            if (vertexWeightsPtr == IntPtr.Zero || length == 0)
            {
                return new ExtVertexWeight[0];
            }

            ExtVertexWeight[] data = new ExtVertexWeight[length];
            for (int weightIndex = 0; weightIndex < length; weightIndex++)
            {
                var ptr = Marshal.PtrToStructure(vertexWeightsPtr + weightIndex * Marshal.SizeOf(typeof(VertexWeight)), typeof(VertexWeight));

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
            var meshCount = FBXSceneContainerDll.GetMeshCount(fbxSceneContainer);

            for (int i = 0; i < meshCount; i++)
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

            string skeletonName = Marshal.PtrToStringUTF8(skeletonNamePtr);

            if (skeletonName == null)
                return "";

            return skeletonName;
        }
    }
}
