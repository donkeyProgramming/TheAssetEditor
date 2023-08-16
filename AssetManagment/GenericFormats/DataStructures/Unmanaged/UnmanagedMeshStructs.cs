using System.Runtime.InteropServices;

namespace AssetManagement.GenericFormats.DataStructures.Unmanaged
{
    [StructLayout(LayoutKind.Sequential)]
    public struct BoneAnimCurveKey
    {
        public XMFLOAT3 translation;
        public XMFLOAT4 quaternion;
        public double timeStamp;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct ExtVertexInfluence
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
        public string boneName;
        public uint boneIndex;
        public float weight;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct ExtPackedCommonVertex
    {
        public XMFLOAT4 Position;
        public XMFLOAT3 Normal;
        public XMFLOAT3 BiNormal;
        public XMFLOAT3 Tangent;
        public XMFLOAT2 Uv;
        public XMFLOAT4 Color;        
        public int weightCount;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct ExtVertexWeight
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
        public string boneName;
        public int boneIndex;
        public int vertexIndex;
        public float weight;
    }
}
