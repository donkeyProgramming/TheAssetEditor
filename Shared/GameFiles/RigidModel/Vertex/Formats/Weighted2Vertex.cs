using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Shared.Core.ByteParsing;
using Shared.GameFormats.RigidModel.Transforms;

namespace Shared.GameFormats.RigidModel.Vertex.Formats
{
    public class Weighted2VertexCreator : IVertexCreator
    {
        public VertexFormat Type => VertexFormat.Weighted;
        public uint GetVertexSize(RmvVersionEnum rmvVersion)
        {
            if (rmvVersion == RmvVersionEnum.RMV2_V8)
                return (uint)ByteHelper.GetSize<Data>() + 4;
            else
                return (uint)ByteHelper.GetSize<Data>();
        }
        public bool ForceComputeNormals => false;

        public CommonVertex[] ReadArray(RmvVersionEnum rmvVersion, byte[] buffer, int offset, int vertexSize, int vertexCount)
        {
            if (rmvVersion == RmvVersionEnum.RMV2_V8)
            {
                var verts = ByteHelper.LoadArray<RmvWeighted2ColourVertex>(buffer, offset, vertexSize * vertexCount);
                var processedVerts = ProcessVertexColourList(verts, rmvVersion);
                return processedVerts;
            }
            else
            {
                var verts = ByteHelper.LoadArray<RmvWeighted2Vertex>(buffer, offset, vertexSize * vertexCount);
                var processedVerts = ProcessVertexList(verts, rmvVersion);
                return processedVerts;
            }
        }

        static CommonVertex[] ProcessVertexColourList(ReadOnlySpan<RmvWeighted2ColourVertex> rawData, RmvVersionEnum rmvVersion)
        {
            var count = rawData.Length;
            var output = new CommonVertex[count];

            for (var i = 0; i < count; i++)
            {
                var item = rawData[i];
                output[i] = new CommonVertex()
                {
                    Position = VertexLoadHelper.CreatVector4HalfFloat2(item.Position.X, item.Position.Y, item.Position.Z, item.Position.W),

                    Normal = VertexLoadHelper.CreatVector3_FromByte(item.Normal),
                    BiNormal = VertexLoadHelper.CreatVector3_FromByte(item.BiNormal),
                    Tangent = VertexLoadHelper.CreatVector3_FromByte(item.Tangent),
                    Uv = new Vector2(item.Uv.X, item.Uv.Y),
                    Colour = VertexLoadHelper.CreatVector4_FromByte(item.Colour),

                    BoneIndex = [item.BoneIndex.X, item.BoneIndex.Y],
                    BoneWeight = [item.BoneWeight.X / 255.0f, item.BoneWeight.Y / 255.0f],
                    WeightCount = 2
                };
            }

            return output;
        }

        static CommonVertex[] ProcessVertexList(ReadOnlySpan<RmvWeighted2Vertex> rawData, RmvVersionEnum rmvVersion)
        {
            var count = rawData.Length;
            var output = new CommonVertex[count];

            for (var i = 0; i < count; i++)
            {
                var item = rawData[i];
                output[i] = new CommonVertex()
                {
                    Position = VertexLoadHelper.CreatVector4HalfFloat2(item.Position.X, item.Position.Y, item.Position.Z, item.Position.W),

                    Normal = VertexLoadHelper.CreatVector3_FromByte(item.Normal),
                    BiNormal = VertexLoadHelper.CreatVector3_FromByte(item.BiNormal),
                    Tangent = VertexLoadHelper.CreatVector3_FromByte(item.Tangent),
                    Uv = new Vector2(item.Uv.X, item.Uv.Y),
                    Colour = new Vector4(0, 0, 0, 1),

                    BoneIndex = [item.BoneIndex.X, item.BoneIndex.Y],
                    BoneWeight = [item.BoneWeight.X / 255.0f, item.BoneWeight.Y / 255.0f],
                    WeightCount = 2
                };
            }

            return output;
        }

        public byte[] Write(RmvVersionEnum rmvVersion, CommonVertex vertex)
        {
            if (vertex.WeightCount != 2 || vertex.BoneIndex.Length != 2 || vertex.BoneWeight.Length != 2)
                throw new Exception($"Unexpected vertex weights for {Type}");

            if (rmvVersion == RmvVersionEnum.RMV2_V8)
            {
                var typedVert = new DataWithColour()
                {
                    position = VertexLoadHelper.CreatePositionVector4ExtraPrecision(vertex.Position),
                    boneIndex = vertex.BoneIndex.ToArray(),
                    boneWeight = vertex.BoneWeight.Select(x => (byte)(x * 255.0f)).ToArray(),
                    normal = VertexLoadHelper.CreateNormalVector3(vertex.Normal),

                    colour = VertexLoadHelper.Create4BytesFromVector4(vertex.Colour),
                    uv = VertexLoadHelper.CreatePositionVector2(vertex.Uv),

                    biNormal = VertexLoadHelper.CreateNormalVector3(vertex.BiNormal),
                    tangent = VertexLoadHelper.CreateNormalVector3(vertex.Tangent),
                };
                return ByteHelper.GetBytes(typedVert);
            }
            else
            {
                var typedVert = new Data()
                {
                    position = VertexLoadHelper.CreatePositionVector4(vertex.Position),
                    boneIndex = vertex.BoneIndex.ToArray(),
                    boneWeight = vertex.BoneWeight.Select(x => (byte)(x * 255.0f)).ToArray(),
                    normal = VertexLoadHelper.CreateNormalVector3(vertex.Normal),

                    uv = VertexLoadHelper.CreatePositionVector2(vertex.Uv),

                    biNormal = VertexLoadHelper.CreateNormalVector3(vertex.BiNormal),
                    tangent = VertexLoadHelper.CreateNormalVector3(vertex.Tangent),
                };
                return ByteHelper.GetBytes(typedVert);
            }
        }

        struct Data    //28
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] position;     // 4 x 2

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public byte[] boneIndex;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public byte[] boneWeight;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] normal;       // 4 x 1

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] uv;           // 2 x 2

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] biNormal;     // 4 x 1

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] tangent;      // 4 x 1
        }

        struct DataWithColour    //32
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] position;     // 4 x 2

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public byte[] boneIndex;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public byte[] boneWeight;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] normal;       // 4 x 1

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] uv;           // 2 x 2

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] biNormal;     // 4 x 1

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] tangent;      // 4 x 1

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] colour;     // 4 x 1
        }

        public struct RmvWeighted2ColourVertex
        {
            public HalfVector4 Position { get; set; }
            public ByteVector2 BoneIndex { get; set; }
            public ByteVector2 BoneWeight { get; set; }
            public ByteVector4 Normal { get; set; }
            public HalfVector2 Uv { get; set; }
            public ByteVector4 BiNormal { get; set; }
            public ByteVector4 Tangent { get; set; }
            public ByteVector4 Colour { get; set; }
        }

        public struct RmvWeighted2Vertex
        {
            public HalfVector4 Position { get; set; }
            public ByteVector2 BoneIndex { get; set; }
            public ByteVector2 BoneWeight { get; set; }
            public ByteVector4 Normal { get; set; }
            public HalfVector2 Uv { get; set; }
            public ByteVector4 BiNormal { get; set; }
            public ByteVector4 Tangent { get; set; }
        }


    }
}
