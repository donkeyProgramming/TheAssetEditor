using System;
using System.Runtime.InteropServices;

namespace AssetManagement.Strategies.Fbx.DllDefinitions
{    
    public class FBXSeneLoaderServiceDLL
    {
        const String dllFileName = "FBXWrapperNative.dll";

        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool GetFileInfo(IntPtr ptrInstance, IntPtr fbxFileInfo);

        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DeleteBaseObj(IntPtr ptrInstances);
                    
        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ProcessAndFillScene(IntPtr ptrFBXSceneLoaderService);

        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateSceneFBX(string path);

        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateFBXSceneImporterService();

        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateFBXContainer();        
        
        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void AddBoneName(IntPtr ptrInstances, string boneName, int len);

        [DllImport(dllFileName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ClearBoneNames(IntPtr ptrInstances);
    }
}
