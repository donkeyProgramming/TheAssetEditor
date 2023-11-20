using System;
using System.Runtime.InteropServices;
using AssetManagement.GenericFormats.DataStructures.Unmanaged;

namespace AssetManagement.Strategies.Fbx.DllDefinitions
{
    public class FBXSceneContainerDll
    {
        const string dllFileName = "FBXWrapperNative.dll";

        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetVertices(IntPtr ptrSceneContainer, int meshIndex, out int itemCount);


        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetIndices(IntPtr ptrSceneContainer, int meshIndex, out int itemCount);


        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetVertexWeights(IntPtr ptrSceneContainer, int meshIndex, out IntPtr ptrVertexWeights, out int itemCount);


        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetMeshCount(IntPtr ptrInstances);


        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetMeshName(IntPtr ptrSceneContainer, int meshIndex);


        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetSkeletonName(IntPtr ptrSceneContainer);


        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetFileInfo(IntPtr ptrSceneContainer);


        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void AllocateMeshes(IntPtr ptrSceneContainer, int meshCount);


        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr AllocateIndices(IntPtr ptrSceneContainer, int meshIndex, int indexCount);


        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr AllocateVertices(IntPtr ptrSceneContainer, int meshIndex, int VertexCount);

        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr AllocateBones(IntPtr ptrSceneContainer, int boneCount);

        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr AllocateVertexWeights(IntPtr ptrSceneContainer, int meshIndex, int weightCount);


        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetIndicesPtr(IntPtr ptrSceneContainer, int meshIndex);


        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetIndices(IntPtr ptrSceneContainer, int meshIndex, uint[] ppIndices, int indexCount);


        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetVertices(IntPtr ptrSceneContainer, int meshIndex, ExtPackedCommonVertex[] ppIndices, int indexCount);


        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetSkeletonName(IntPtr ptrExporter, string path);

        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetMeshName(IntPtr ptrExporter, int meshIndex, string name);
    }
}
