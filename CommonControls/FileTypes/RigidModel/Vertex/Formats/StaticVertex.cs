using CommonControls.FileTypes;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CommonControls.FileTypes.RigidModel.Vertex.Formats
{
    public class StaticVertexCreator : IVertexCreator
    {
        public VertexFormat Type => VertexFormat.Static;
        public uint GetVertexSize(RmvVersionEnum rmvVersion)
        {

            return (uint)ByteHelper.GetSize<Data>();
        }
        public bool ForceComputeNormals => false;

        // preserved for me to look at, for reference :)
        public CommonVertex ___old_Read(RmvVersionEnum rmvVersion, byte[] buffer, int offset, int vertexSize)
        {
            var vertexData = ByteHelper.ByteArrayToStructure<Data>(buffer, offset);

            var vertex = new CommonVertex()
            {
                Position = VertexLoadHelper.CreatVector4HalfFloat(vertexData.position).ToVector4(1),
                Normal = VertexLoadHelper.CreatVector4Byte(vertexData.normal).ToVector3(),
                BiNormal = VertexLoadHelper.CreatVector4Byte(vertexData.biNormal).ToVector3(),
                Tangent = VertexLoadHelper.CreatVector4Byte(vertexData.tangent).ToVector3(),

                Uv = VertexLoadHelper.CreatVector2HalfFloat(vertexData.uv).ToVector2(),

                Colour = VertexLoadHelper.CreatVector4Byte(vertexData.RGBA).ToVector4(),

                BoneIndex = new byte[0],
                BoneWeight = new float[0]
            };

            return vertex;
        }


        static Vector3 SwapXZ(Vector3 v)
        {
            return new Vector3(v.Z, v.Y, v.X);
        }

        public CommonVertex Read(RmvVersionEnum rmvVersion, byte[] buffer, int offset, int vertexSize)
        {
            var vertexData = ByteHelper.ByteArrayToStructure<Data>(buffer, offset);

            var vertex = new CommonVertex()
            {
                // VertexFormat = ´default` format has X and Z swapped 
                Position = VertexLoadHelper.CreatVector4HalfFloat(vertexData.position).ToVector4(1),

                // 'bitangent' is stored before 'tangent' when VertexFormat = ´default`
                Normal = SwapXZ(VertexLoadHelper.CreatVector4Byte(vertexData.normal).ToVector3()),
                Tangent = SwapXZ(VertexLoadHelper.CreatVector4Byte(vertexData.tangent).ToVector3()),
                BiNormal = SwapXZ(VertexLoadHelper.CreatVector4Byte(vertexData.biNormal).ToVector3()),

                Uv = VertexLoadHelper.CreatVector2HalfFloat(vertexData.uv).ToVector2(),

                Colour = VertexLoadHelper.CreatVector4Byte(vertexData.RGBA).ToVector4(),

                BoneIndex = new byte[0],
                BoneWeight = new float[0]
            };

            return vertex;
        }




        public byte[] Write(RmvVersionEnum rmvVersion, CommonVertex vertex)
        {
            if (vertex.WeightCount != 0 || vertex.BoneIndex.Length != 0 || vertex.BoneWeight.Length != 0)
                throw new Exception($"Unexpected vertex weights for {Type}");

            var typedVert = new Data()
            {
                position = VertexLoadHelper.CreatePositionVector4(vertex.Position),
                normal = VertexLoadHelper.CreateNormalVector3(SwapXZ(vertex.Normal)),

                uv = VertexLoadHelper.CreatePositionVector2(vertex.Uv),
                uvExtra = VertexLoadHelper.CreatePositionVector2(new Vector2(0, 0)),

                tangent = VertexLoadHelper.CreateNormalVector3(SwapXZ(vertex.Tangent)),
                biNormal = VertexLoadHelper.CreateNormalVector3(SwapXZ(vertex.BiNormal)),

                RGBA = VertexLoadHelper.CreatePositionVector4(vertex.Position),
            };

            return ByteHelper.GetBytes(typedVert);
        }

        struct Data //32
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] position;     // 4 x 2

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] uv;           // 2 x 2

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] uvExtra;     // 2 x 2

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] normal;       // 4 x 1

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] tangent;      // 4 x 1

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] biNormal;     // 4 x 1            

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] RGBA;     // 4 x 1
        }
    }
}
