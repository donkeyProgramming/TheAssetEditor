using System.Runtime.InteropServices;
using AssetManagement.GenericFormats.Unmanaged;

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
        //public int weightCount;
    };

    /// <summary>
    /// "VertexWeight" associates 1 vertex with 1 influencec {bone, weight}
    /// </summary>    
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct ExtVertexWeight
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
        public string boneName;
        public uint boneIndex; // TODO: should be removed, maybe, as it is not known when struct is first filled
        public uint vertexIndex;
        public float weight;
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct ExtBoneInfo
    {
        public int id;
        public int parentId;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string name;

        public XMFLOAT4 localRotation;
        public XMFLOAT3 localTranslation;

    };

}
