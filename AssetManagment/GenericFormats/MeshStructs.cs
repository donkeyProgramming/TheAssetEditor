using AssetManagement.GenericFormats.Native;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace AssetManagement.GenericFormats
{
    // - structs that are bit for bit compatible with types used on the C++ side
    namespace Native
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct FBXControlPoint
        {
            public double x;
            public double Y;
            public double z;
            public double w;
        }

        [StructLayout(LayoutKind.Sequential, Size = 16)]
        public struct XMFLOAT4
        {
            public float x;
            public float y;
            public float z;
            public float w;
        }

        [StructLayout(LayoutKind.Sequential, Size = 12)]
        public struct XMFLOAT3
        {
            public float x;
            public float y;
            public float z;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct XMFLOAT2
        {
            public float x;
            public float y;
        }

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

        [StructLayout(LayoutKind.Sequential)]
        public struct PackedCommonVertex
        {
            public XMFLOAT4 Position;
            public XMFLOAT3 Normal;
            public XMFLOAT3 BiNormal;
            public XMFLOAT3 Tangent;
            public XMFLOAT2 Uv;
            public XMFLOAT4 Color;

            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct, SizeConst = 4)]
            public VertexInfluence[]? influences; // fixed array length 4        

            public int WeightCount;
        };
    }

    public class PackedMesh
    {
        public string Name { set; get; }
        public List<PackedCommonVertex> Vertices { set; get; }
        public List<ushort> Indices { set; get; }
    }
};


