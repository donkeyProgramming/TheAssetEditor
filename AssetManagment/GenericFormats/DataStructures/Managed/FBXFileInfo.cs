using AssetManagement.Marshalling;
using AssetManagement.GenericFormats.DataStructures.Unmanaged;

namespace AssetManagement.GenericFormats.DataStructures.Managed
{
    /// <summary>
    /// Storage for the X, Y, Z version data, FBX SDK uses
    /// </summary>
    public class VersionStruct
    {
        public int X { set; get; }
        public int Y { set; get; }
        public int Z { set; get; }
    }

    /// <summary>
    /// "mirror" file info values, from the C++ FBX SDK side
    /// So far only class that uses the "copy from temp interop struct to proper classs" interface
    /// </summary>
    public class FBXFileInfo : IMarshalable<ExtFileInfoStruct>
    { 
        public VersionStruct SdkVersionUsed { set; get; } = new VersionStruct();
        public string FileName { set; get; } = "";
        public string SkeletonName { set; get; }
        public string Units { set; get; }
        public float ScaleFatorToMeters { set; get; }
        public int ElementCount { set; get; }
        public int MeshCount { set; get; }
        public int BoneCount { set; get; }
        public int MaterialCount { set; get; }
        public int AnimationsCount { set; get; }
        public bool ContainsDerformingData { set; get; }
        public bool IsSkeletonIdStringBone { set; get; }
        override public void FillStruct(out ExtFileInfoStruct destStruct)
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
            destStruct.animationsCount = AnimationsCount;
            destStruct.boneCount = BoneCount;
            destStruct.containsDerformingData = ContainsDerformingData;
            destStruct.isIdSkeletonStringBone = IsSkeletonIdStringBone;
        }

        override public void FillFromStruct(in ExtFileInfoStruct srcStruct)
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
            AnimationsCount = srcStruct.animationsCount;
            BoneCount = srcStruct.boneCount;
            ContainsDerformingData = srcStruct.containsDerformingData;
            IsSkeletonIdStringBone = srcStruct.isIdSkeletonStringBone;
        }
    }
}
