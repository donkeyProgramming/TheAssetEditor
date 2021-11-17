using Microsoft.Xna.Framework;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Filetypes.RigidModel.Vertex.Formats
{
    public class CinematicVertexCreator : IVertexCreator
    {
        public VertexFormat Type => VertexFormat.Cinematic;
        public bool AddTintColour { get; set; }
        public uint VertexSize => (uint)ByteHelper.GetSize<Data>() + TintColourSize();

        public CommonVertex Create(byte[] buffer, int offset, int vertexSize)
        {
            var vertexData = ByteHelper.ByteArrayToStructure<Data>(buffer, offset);

            var tintColour = new Vector4(0, 0, 0, 1);
            if (AddTintColour)
            {
                var tintBuffer = ByteParsing.ByteParsers.Byte.ReadArray(buffer, offset + vertexSize - 4, 4);
                tintColour = VertexLoadHelper.CreatVector4HalfFloat(tintBuffer).ToVector4();
            }

            var vertex = new CommonVertex()
            {
                Position = VertexLoadHelper.CreatVector4HalfFloat(vertexData.position).ToVector4(1),
                Normal = VertexLoadHelper.CreatVector4Byte(vertexData.normal).ToVector3(),
                BiNormal = VertexLoadHelper.CreatVector4Byte(vertexData.biNormal).ToVector3(),
                Tangent = VertexLoadHelper.CreatVector4Byte(vertexData.tangent).ToVector3(),

                Uv = VertexLoadHelper.CreatVector2HalfFloat(vertexData.uv).ToVector2(),

                Colour = tintColour,

                BoneIndex = new byte[] { vertexData.boneIndex[0], vertexData.boneIndex[1], vertexData.boneIndex[2], vertexData.boneIndex[3] },
                BoneWeight = new float[] { vertexData.boneWeight[0] / 255.0f, vertexData.boneWeight[1] / 255.0f, vertexData.boneWeight[2] / 255.0f, vertexData.boneWeight[3] / 255.0f },
                WeightCount = 4
            };

            return vertex;
        }


        public byte[] ToBytes(CommonVertex vertex)
        {
            if (AddTintColour)
                throw new NotImplementedException("TODO");

            if (vertex.WeightCount != 4 || vertex.BoneIndex.Length != 4 || vertex.BoneWeight.Length != 4)
                throw new Exception($"Unexpected vertex weights for {Type}");

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

        uint TintColourSize()
        {
            if (AddTintColour)
                return 4;
            return 0;
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
    }
}
