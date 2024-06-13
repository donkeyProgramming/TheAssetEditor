using System.Runtime.InteropServices;

namespace AssetManagement.GenericFormats.Unmanaged
{
    /// <summary>
    /// bit-compatible with fbxsdk::FbxVector4
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
