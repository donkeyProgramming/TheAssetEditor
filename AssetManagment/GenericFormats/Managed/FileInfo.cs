using AssetManagement.Marhalling;
using AssetManagement.GenericFormats.Unmanaged;

namespace AssetManagement.GenericFormats.Managed
{
    public class VersionStruct
    {
        public int X { set; get; }
        public int Y { set; get; }
        public int Z { set; get; }
    }

    public class FileInfo
    {
        public VersionStruct SdkVersionUsed { set; get; }
        public string Units { set; get; }
        public float ScaleFatorToMeters { set; get; }
        public int ElementCount { set; get; }
        public int MeshCount { set; get; }
    }

    // TODO: remove from commmit?
    public class FileInfoMarhal : IMarshalable<ExtFileInfoStruct>
    {
        public VersionStruct SdkVersionUsed { set; get; }
        public string Units { set; get; }
        public float ScaleFatorToMeters { set; get; }
        public int ElementCount { set; get; }
        public int MeshCount { set; get; }

        public void FillStruct(out ExtFileInfoStruct destStruct)
        {
            destStruct.sdkVersionUsed.x = SdkVersionUsed.Y;
            destStruct.sdkVersionUsed.y = SdkVersionUsed.X;
            destStruct.sdkVersionUsed.z = SdkVersionUsed.Z;
            destStruct.units = Units;
            destStruct.scaleFatorToMeters = ScaleFatorToMeters;
            destStruct.elementCount = ElementCount;
            destStruct.meshCount = MeshCount;
        }

        public void FillFromStruct(in ExtFileInfoStruct srcStruct)
        {
            SdkVersionUsed.Y = srcStruct.sdkVersionUsed.x;
            SdkVersionUsed.X = srcStruct.sdkVersionUsed.y;
            SdkVersionUsed.Z = srcStruct.sdkVersionUsed.z;
            Units = srcStruct.units;
            ScaleFatorToMeters = srcStruct.scaleFatorToMeters;
            ElementCount = srcStruct.elementCount;
            MeshCount = srcStruct.meshCount;
        }
    }

}
