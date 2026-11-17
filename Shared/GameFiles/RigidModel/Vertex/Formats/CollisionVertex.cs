
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Shared.ByteParsing;

namespace Shared.GameFormats.RigidModel.Vertex.Formats
{
    public class CollisionVertexCreator : IVertexCreator
    {
        public VertexFormat Type => VertexFormat.Collision_Format;
        public bool AddTintColour { get; set; }
        public uint GetVertexSize(RmvVersionEnum rmvVersion)
        {
            return (uint)ByteHelper.GetSize<Data>();
        }

        // The file stores a float3 normal per vertex, so there is nothing to recompute
        public bool ForceComputeNormals => false;

        public CommonVertex[] ReadArray(RmvVersionEnum rmvVersion, byte[] buffer, int offset, int vertexSize, int vertexCount)
        {
            var vertexList = new CommonVertex[vertexCount];
            for (var i = 0; i < vertexCount; i++)
                vertexList[i] = Read(rmvVersion, buffer, offset + i * vertexSize, vertexSize);
            return vertexList;
        }

        CommonVertex Read(RmvVersionEnum rmvVersion, byte[] buffer, int offset, int vertexSize)
        {
            var vertexData = ByteHelper.ByteArrayToStructure<Data>(buffer, offset);

            var vertex = new CommonVertex()
            {
                Position = new Vector4(ReadVector3(vertexData.position), 1),
                Normal = ReadVector3(vertexData.normal),
                BiNormal = Vector3.UnitY,
                Tangent = Vector3.UnitY,

                Uv = Vector2.Zero,// VertexLoadHelper.CreatVector2HalfFloat(vertexData.uv).ToVector2(),

                BoneIndex = new byte[] { },
                BoneWeight = new float[] { },
                WeightCount = 0
            };

            return vertex;
        }


        public byte[] Write(RmvVersionEnum rmvVersion, CommonVertex vertex)
        {
            var typedVert = new Data()
            {
                position = WriteVector3(new Vector3(vertex.Position.X, vertex.Position.Y, vertex.Position.Z)),
                normal = WriteVector3(vertex.Normal),
            };
            return ByteHelper.GetBytes(typedVert);
        }

        static Vector3 ReadVector3(byte[] data) => new(BitConverter.ToSingle(data, 0), BitConverter.ToSingle(data, 4), BitConverter.ToSingle(data, 8));

        static byte[] WriteVector3(Vector3 value) => [.. BitConverter.GetBytes(value.X), .. BitConverter.GetBytes(value.Y), .. BitConverter.GetBytes(value.Z)];

        public struct Data //24
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
            public byte[] position;     // float3, not scaled by a W

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
            public byte[] normal;       // float3
        }
    }
}
