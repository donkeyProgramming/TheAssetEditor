using System;
using System.Runtime.InteropServices;

namespace AssetManagement.Strategies.Fbx.DllDefinitions
{
    public class FBXSeneLoaderServiceDLL
    {
        [DllImport("FBXWrapperNative.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void DeleteBaseObj(IntPtr ptrInstances);

        [DllImport("FBXWrapperNative.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ProcessAndFillScene(IntPtr ptrFBXSceneLoaderService);

        [DllImport("FBXWrapperNative.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateSceneFBX(string path);

        [DllImport("FBXWrapperNative.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateFBXSceneImporterService();

        [DllImport("FBXWrapperNative.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateFBXContainer();

        [DllImport("FBXWrapperNative.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetSkeletonNameFromScene(IntPtr ptrSceneLoader);

        [DllImport("FBXWrapperNative.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void AddBoneName(IntPtr ptrInstances, string boneName, int len);

        [DllImport("FBXWrapperNative.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ClearBoneNames(IntPtr ptrInstances);
    }
}
