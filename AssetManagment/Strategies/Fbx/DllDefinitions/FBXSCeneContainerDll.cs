using System;
using System.Runtime.InteropServices;

namespace AssetManagement.Strategies.Fbx.DllDefinitions
{
    public class FBXSCeneContainerDll
    {
        const String dllFileName = "FBXWrapperNative.dll";

        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool GetPackedVertices(IntPtr ptrInstances, int meshIndex, out IntPtr vertices, out int itemCount);

        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool GetIndices(IntPtr ptrInstances, int meshIndex, out IntPtr vertices, out int itemCount);
        
        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool GetVertexWeights(IntPtr ptrInstances, int meshIndex, out IntPtr vertices, out int itemCount);

        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetMeshCount(IntPtr ptrInstances);

        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetMeshName(IntPtr ptrInstances, int meshIndex);
    }
}
