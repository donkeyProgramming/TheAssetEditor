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
   
    /// <summary>
    /// So far only class that uses the "copy from temp interop struct to proper classs" interface
    /// </summary>
    public class FileInfoData : IMarshalable<ExtFileInfoStruct>
    {
        public VersionStruct SdkVersionUsed { set; get; } = new VersionStruct();
        public string FileName { set; get; } = "this ass stuff better work"; // TODO: remove
        public string SkeletonName { set; get; }
        public string Units { set; get; }
        public float ScaleFatorToMeters { set; get; }
        public int ElementCount { set; get; }
        public int MeshCount { set; get; }
        public int MaterialCount { set; get; }

        public void FillStruct(out ExtFileInfoStruct destStruct)        
        {
            destStruct.sdkVersionUsed.x = SdkVersionUsed.X;
            destStruct.sdkVersionUsed.y = SdkVersionUsed.Y;
            destStruct.sdkVersionUsed.z = SdkVersionUsed.Z;
            destStruct.fileName = FileName;
            destStruct.skeletonName = SkeletonName;
            destStruct.units = Units;
            destStruct.scaleFatorToMeters = ScaleFatorToMeters;
            destStruct.elementCount = ElementCount;
            destStruct.meshCount = MeshCount;
            destStruct.materialCount = MaterialCount;
        }

        public void FillFromStruct(in ExtFileInfoStruct srcStruct)
        {
            SdkVersionUsed.X = srcStruct.sdkVersionUsed.x;
            SdkVersionUsed.Y = srcStruct.sdkVersionUsed.y;
            SdkVersionUsed.Z = srcStruct.sdkVersionUsed.z;
            FileName = srcStruct.fileName;
            SkeletonName = srcStruct.skeletonName;
            Units = srcStruct.units;
            ScaleFatorToMeters = srcStruct.scaleFatorToMeters;
            ElementCount = srcStruct.elementCount;
            MeshCount = srcStruct.meshCount;
            MaterialCount = srcStruct.materialCount;
        }
    }

}
