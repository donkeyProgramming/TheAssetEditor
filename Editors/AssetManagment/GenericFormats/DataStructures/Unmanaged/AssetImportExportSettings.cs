using System.Runtime.InteropServices;
using AssetManagement.GenericFormats.Unmanaged;

namespace AssetManagement.GenericFormats.DataStructures.Unmanaged
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

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string fileName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string skeletonName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string units;

        public bool isIdSkeletonStringBone;
        public float scaleFatorToMeters;
        public int elementCount;
        public int meshCount;
        public int materialCount;
        public int animationsCount;
        public int boneCount;
        public bool containsDerformingData;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct AssetImportExportSettings
    {
        public ExtFileInfoStruct fileinfo;
        public FBXUnitFileInfo unitInfo;
        public FBXAnimInfo animTimingINfo;
    }
}
