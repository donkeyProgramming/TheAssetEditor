using System.Runtime.InteropServices;

namespace AssetManagement.GenericFormats.Unmanaged
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FBXSDKVersion
    {
        public int x;
        public int y;
        public int z;
    }
    

    [StructLayout(LayoutKind.Sequential)]
    public struct FBXUnitFileInfo
    {
        public string unitNameString;
        public float globalScale;
        public float SsaleToMeters;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FBXAnimInfo
    {
        public string skeletonName;
        public float animFPS;
        public float animTiem;
        public int animFrame;
    }
     

    [StructLayout(LayoutKind.Sequential)]
    public struct ExtFileInfoStruct
    {
        public XMINT3 sdkVersionUsed;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
        public string fileName;
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
        public string skeletonName;
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
        public string units;

        public float scaleFatorToMeters;
        public int elementCount;
        public int meshCount;
        public int materialCount;
        public int animationsCount;
        public int boneCount;
    };


    [StructLayout(LayoutKind.Sequential)]
    public struct FBXImportExportSettings
    {
        public ExtFileInfoStruct fileinfo;
        public FBXUnitFileInfo unitInfo;
        public FBXAnimInfo animTimingINfo;
    }

}
