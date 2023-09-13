using System.Runtime.InteropServices;

namespace AssetManagement.GenericFormats.DataStructures.Unmanaged
{
    /// <summary>
    /// bit-compatible with fbxsdk::FbxVector4, for uses with "copy only FBX mesh data, do processing on C# side"
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct FBXControlPoint
    {
        public double x;
        public double Y;
        public double z;
        public double w;
    }
}
