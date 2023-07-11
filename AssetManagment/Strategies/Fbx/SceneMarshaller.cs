using AssetManagement.GenericFormats;
using AssetManagement.GenericFormats.Native;
using AssetManagement.Strategies.Fbx.DllDefinitions;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace AssetManagement.Strategies.Fbx
{
    public class SceneMarshaller
    {
        public static SceneContainer ToManaged(IntPtr ptrFbxSceneContainer)
        {
            var newScene = new SceneContainer();
            newScene.Meshes = GetAllPackedMeshes(ptrFbxSceneContainer);
            return newScene;
            /*
            - destScene.Bones = GetAllBones();
            - destScene.Animations = GetAllBones();
            - etc, comming soon
            */
        }

        public static PackedCommonVertex[] GetPackesVertices(IntPtr fbxContainer, int meshIndex)
        {
            IntPtr pVerticesPtr = IntPtr.Zero;
            int length = 0;
            FBXSCeneContainerDll.GetPackedVertices(fbxContainer, meshIndex, out pVerticesPtr, out length);

            if (pVerticesPtr == IntPtr.Zero || length == 0)
            {
                return null;
            }

            PackedCommonVertex[] data = new PackedCommonVertex[length];
            for (int vertexIndex = 0; vertexIndex < length; vertexIndex++)
            {
                var ptr = Marshal.PtrToStructure(pVerticesPtr + vertexIndex * Marshal.SizeOf(typeof(PackedCommonVertex)), typeof(PackedCommonVertex));

                if (ptr != null)
                    data[vertexIndex] = (PackedCommonVertex)ptr;
            }

            return data;
        }

        public static ushort[] GetIndices(IntPtr fbxContainer, int meshIndex)
        {
            IntPtr pIndices = IntPtr.Zero;
            int length = 0;
            FBXSCeneContainerDll.GetIndices(fbxContainer, meshIndex, out pIndices, out length);

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
            var vertices = GetPackesVertices(fbxContainer, meshIndex);

            IntPtr namePtr = FBXSCeneContainerDll.GetMeshName(fbxContainer, meshIndex);
            var tempName = Marshal.PtrToStringUTF8(namePtr);

            if (vertices == null || indices == null || tempName == null)
                throw new Exception("Params/Input Data Invalid: Vertices, Indices or Name == null");

            PackedMesh packedMesh = new PackedMesh();
            packedMesh.Vertices = new List<PackedCommonVertex>();
            packedMesh.Indices = new List<ushort>();
            packedMesh.Vertices.AddRange(vertices);
            packedMesh.Indices.AddRange(indices);
            packedMesh.Name = tempName;

            return packedMesh;
        }

        static public List<PackedMesh> GetAllPackedMeshes(IntPtr fbxSceneContainer)
        {
            List<PackedMesh> meshList = new List<PackedMesh>();
            var meshCount = FBXSCeneContainerDll.GetMeshCount(fbxSceneContainer);

            for (int i = 0; i < meshCount; i++)
            {
                meshList.Add(GetPackedMesh(fbxSceneContainer, i));
            }

            return meshList;
        }

    }
}
