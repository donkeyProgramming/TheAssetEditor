using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Shared.Core.ByteParsing;
using Shared.GameFormats.RigidModel.Transforms;

namespace Shared.GameFormats.RigidModel.Vertex.Formats
{
    public class Weighted4VertexCreator : IVertexCreator
    {
        public VertexFormat Type => VertexFormat.Cinematic;
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
                var verts = ByteHelper.LoadArray<RmvWeighted4ColourVertex>(buffer, offset, vertexSize * vertexCount);
                var processedVerts = ProcessVertexColourList(verts, rmvVersion);
                return processedVerts;
            }
            else
            {
                var verts = ByteHelper.LoadArray<RmvWeighted4Vertex>(buffer, offset, vertexSize * vertexCount);
                var processedVerts = ProcessVertexList(verts, rmvVersion);
                return processedVerts;
            }
        }

        static CommonVertex[] ProcessVertexColourList(ReadOnlySpan<RmvWeighted4ColourVertex> rawData, RmvVersionEnum rmvVersion)
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

                    BoneIndex = [item.BoneIndex.X, item.BoneIndex.Y, item.BoneIndex.Z, item.BoneIndex.W],
                    BoneWeight = [item.BoneWeight.X / 255.0f, item.BoneWeight.Y / 255.0f, item.BoneWeight.Z / 255.0f, item.BoneWeight.W / 255.0f],
                    WeightCount = 4
                };
            }

            return output;
        }

        static CommonVertex[] ProcessVertexList(ReadOnlySpan<RmvWeighted4Vertex> rawData, RmvVersionEnum rmvVersion)
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

                    BoneIndex = [item.BoneIndex.X, item.BoneIndex.Y, item.BoneIndex.Z, item.BoneIndex.W],
                    BoneWeight = [item.BoneWeight.X / 255.0f, item.BoneWeight.Y / 255.0f, item.BoneWeight.Z / 255.0f, item.BoneWeight.W / 255.0f],
                    WeightCount = 4
                };
            }

            return output;
        }

        public byte[] Write(RmvVersionEnum rmvVersion, CommonVertex vertex)
        {
            if (vertex.WeightCount != 4 || vertex.BoneIndex.Length != 4 || vertex.BoneWeight.Length != 4)
                throw new Exception($"Unexpected vertex weights for {Type}");

            var newPos = vertex.Position;
            newPos.W = 0;

            if (rmvVersion == RmvVersionEnum.RMV2_V8)
            {
                var typedVert = new DataWithColour()
                {
                    position = VertexLoadHelper.CreatePositionVector4ExtraPrecision(newPos),
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
                    position = VertexLoadHelper.CreatePositionVector4(newPos),
                    boneIndex = vertex.BoneIndex.ToArray(),
                    boneWeight = vertex.BoneWeight.Select(x => (byte)(x * 255.0f)).ToArray(),
                    normal = VertexLoadHelper.CreateNormalVector3(vertex.Normal),

                    uv = VertexLoadHelper.CreatePositionVector2(vertex.Uv),

                    biNormal = VertexLoadHelper.CreateNormalVector3(vertex.BiNormal),
                    tangent = VertexLoadHelper.CreateNormalVector3(vertex.Tangent),
                };
                var bytes = ByteHelper.GetBytes(typedVert);
                return bytes;
            }
        }



        public struct Data //32
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] position;     // 4 x 2

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] boneIndex;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
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

        public struct DataWithColour // 36
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] position;     // 4 x 2

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] boneIndex;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
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

        public struct RmvWeighted4ColourVertex
        {
            public HalfVector4 Position { get; set; }
            public ByteVector4 BoneIndex { get; set; }
            public ByteVector4 BoneWeight { get; set; }
            public ByteVector4 Normal { get; set; } 
            public HalfVector2 Uv { get; set; }
            public ByteVector4 BiNormal { get; set; }      
            public ByteVector4 Tangent { get; set; }     
            public ByteVector4 Colour { get; set; }     
        }

        public struct RmvWeighted4Vertex
        {
            public HalfVector4 Position { get; set; }
            public ByteVector4 BoneIndex { get; set; }
            public ByteVector4 BoneWeight { get; set; }
            public ByteVector4 Normal { get; set; }
            public HalfVector2 Uv { get; set; }
            public ByteVector4 BiNormal { get; set; }
            public ByteVector4 Tangent { get; set; }
        }
    }
}
