using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Shared.Core.ByteParsing;

namespace Shared.GameFormats.RigidModel.Vertex.Formats
{

    public class CustomTerrainVertexCreator : IVertexCreator
    {
        public VertexFormat Type => VertexFormat.CustomTerrain;
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
                Position = VertexLoadHelper.CreatVector4Float(vertexData.position).ToVector4(1),
                Normal = VertexLoadHelper.CreatVector4Float(vertexData.normal).ToVector3(),
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
            throw new NotImplementedException();
        }

        public struct Data //32
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] position;     // 4 x 4

            // No idea what all this really iss 
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] normal;       // 4 x 1

            //
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] uv;           // 2 x 2

            //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            //public byte[] biNormal;     // 4 x 1
            //
            //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            //public byte[] tangent;      // 4 x 1
        }
    }
}
