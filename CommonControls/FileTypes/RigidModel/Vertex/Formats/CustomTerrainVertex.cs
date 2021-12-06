using CommonControls.FileTypes;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace CommonControls.FileTypes.RigidModel.Vertex.Formats
{

    public class CustomTerrainVertexreator : IVertexCreator
    {
        public VertexFormat Type => VertexFormat.CustomTerrain;
        public bool AddTintColour { get; set; }
        public uint VertexSize => (uint)ByteHelper.GetSize<Data>();
        public bool ForceComputeNormals => true;

        public CommonVertex Create(byte[] buffer, int offset, int vertexSize)
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


        public byte[] ToBytes(CommonVertex vertex)
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
