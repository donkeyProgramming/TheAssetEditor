using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Shared.ByteParsing;

namespace Shared.GameFormats.RigidModel.Vertex.Formats
{
    public class Position16_bitVertexCreator : IVertexCreator
    {
        public VertexFormat Type => VertexFormat.Position16_bit;
        public bool AddTintColour { get; set; }
        public uint GetVertexSize(RmvVersionEnum rmvVersion)
        {

            return (uint)ByteHelper.GetSize<Data>();
        }
        public bool ForceComputeNormals => true;

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
                Position = new Vector4(ReadScaled(vertexData.position, out var w), 1),
                Extra = w == 1 ? [] : [w],
                Normal = Vector3.UnitY, //VertexLoadHelper.CreatVector4Byte(vertexData.normal).ToVector3(),
                BiNormal = Vector3.UnitY,
                Tangent = Vector3.UnitY,

                Uv = Vector2.Zero,// VertexLoadHelper.CreatVector2HalfFloat(vertexData.uv).ToVector2(),

                BoneIndex = new byte[] { },
                BoneWeight = new float[] { },
                WeightCount = 0
            };

            return vertex;
        }


        // Float4 position whose W, when positive, is a scale on XYZ. Most files store 1, but some
        // Attila terrain tiles and campaign vegetation do not, so the file's W is kept in Extra
        static Vector3 ReadScaled(byte[] data, out float w)
        {
            w = BitConverter.ToSingle(data, 12);
            var scale = w > 0 ? w : 1;
            return new Vector3(BitConverter.ToSingle(data, 0), BitConverter.ToSingle(data, 4), BitConverter.ToSingle(data, 8)) * scale;
        }

        public byte[] Write(RmvVersionEnum rmvVersion, CommonVertex vertex)
        {
            var w = vertex.Extra.Length == 1 ? vertex.Extra[0] : 1;
            var scale = w > 0 ? w : 1;
            var typedVert = new Data()
            {
                position = [.. BitConverter.GetBytes(vertex.Position.X / scale), .. BitConverter.GetBytes(vertex.Position.Y / scale), .. BitConverter.GetBytes(vertex.Position.Z / scale), .. BitConverter.GetBytes(w)],
            };
            return ByteHelper.GetBytes(typedVert);
        }

        public struct Data //16
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] position;     // 4 x 4

            //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            //public byte[] normal;       // 4 x 1
            //
            //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            //public byte[] uv;           // 2 x 2

            //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            //public byte[] biNormal;     // 4 x 1
            //
            //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            //public byte[] tangent;      // 4 x 1
        }
    }
}
