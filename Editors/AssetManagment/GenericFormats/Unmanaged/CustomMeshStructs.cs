using System.Runtime.InteropServices;

namespace AssetManagement.GenericFormats.Unmanaged
{
    [StructLayout(LayoutKind.Sequential)]
    public struct BoneKey
    {
        public XMFLOAT3 translation;
        public XMFLOAT4 quaternion;
        public double timeStampe;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct VertexInfluence
    {
        public uint boneIndex;
        public float weight;
    };
     

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct VertexWeight
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
        public string boneName;
        public int vertexIndex;
        public float vertexWeight;
    }

}
