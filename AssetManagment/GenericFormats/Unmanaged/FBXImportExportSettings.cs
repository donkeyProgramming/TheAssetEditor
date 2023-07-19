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
    public struct FBXFileInfo
    {
        public FBXSDKVersion sdkVersion; // SDK version file is saved with;
        public bool isASCII; // FBX can be saves as binary or json/xml like ASCII text
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
    public struct FBXImportExportSettings    
    {     
        public FBXFileInfo fileinfo;
        public FBXUnitFileInfo unitInfo;
        public FBXAnimInfo animTimingINfo;
    }
}