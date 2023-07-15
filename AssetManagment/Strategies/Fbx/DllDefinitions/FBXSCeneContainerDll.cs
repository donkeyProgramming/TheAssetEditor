using System;
using System.Runtime.InteropServices;

namespace AssetManagement.Strategies.Fbx.DllDefinitions
{
    public class FBXSCeneContainerDll
    {
        [DllImport("FBXWrapperNative.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool GetPackedVertices(IntPtr ptrInstances, int meshIndex, out IntPtr vertices, out int itemCount);

        [DllImport("FBXWrapperNative.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool GetIndices(IntPtr ptrInstances, int meshIndex, out IntPtr vertices, out int itemCount);

        [DllImport("FBXWrapperNative.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetMeshCount(IntPtr ptrInstances);

        [DllImport("FBXWrapperNative.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetMeshName(IntPtr ptrInstances, int meshIndex);
    }
}
