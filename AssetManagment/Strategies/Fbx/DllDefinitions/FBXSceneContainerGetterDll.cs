using System;
using System.Runtime.InteropServices;

namespace AssetManagement.Strategies.Fbx.DllDefinitions
{
    public class FBXSCeneContainerGetterDll
    {
        const String dllFileName = "FBXWrapperNative.dll";

        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool GetPackedVertices(IntPtr ptrSceneContainer, int meshIndex, out IntPtr vertices, out int itemCount);

        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool GetIndices(IntPtr ptrSceneContainer, int meshIndex, out IntPtr vertices, out int itemCount);

        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool GetVertexWeights(IntPtr ptrSceneContainer, int meshIndex, out IntPtr vertices, out int itemCount);

        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetMeshCount(IntPtr ptrInstances);

        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetMeshName(IntPtr ptrSceneContainer, int meshIndex);

        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetSkeletonName(IntPtr ptrSceneContainer);

        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetFileInfo(IntPtr ptrSceneContainer);
    }
}
