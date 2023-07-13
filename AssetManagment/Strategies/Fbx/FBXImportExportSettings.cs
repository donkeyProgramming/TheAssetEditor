using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;

namespace AssetManagement.Strategies.Fbx
{
    public struct FBXSDKVersion
    {
        public int x;
        public int y;
        public int z;
    }

    public struct FBXFileInfo
    {
        public FBXSDKVersion sdkVersion; // SDK version file is saved with;
        public bool isASCII; // FBX can be saves as binary or json/xml like ASCII text
    }
    public struct FBXUnitFileInfo
    {
        public string unitNameString;
        public float globalScale;
        public float SsaleToMeters;
    }
    public struct FBXAnimTimingInfo
    {
        public float animFPS;
        public float animTiem;
        public int animFrame;
    }

    public class FBXImportExportSettings
    {
        public string fileName;
        public FBXFileInfo fileinfo;
        public FBXUnitFileInfo unitInfo;
        public FBXAnimTimingInfo animTimingINfo;
    }
}