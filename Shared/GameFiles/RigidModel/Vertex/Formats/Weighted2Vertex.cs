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
                return (uint)ByteHelper.GetSize<RmvWeighted2ColourVertex>();
            else
                return (uint)ByteHelper.GetSize<RmvWeighted2Vertex>();
        }
        public bool ForceComputeNormals => false;

        public CommonVertex[] ReadArray(RmvVersionEnum rmvVersion, byte[] buffer, int offset, int vertexSize, int vertexCount)
        {
            var output = new CommonVertex[vertexCount];

            if (rmvVersion == RmvVersionEnum.RMV2_V8)
            {
                var verts = ByteHelper.LoadArray<RmvWeighted2ColourVertex>(buffer, offset, vertexSize * vertexCount);
                for (var i = 0; i < vertexCount; i++)
                    output[i] = RmvWeighted2ColourVertex.ToCommon(verts[i]);
            }
            else
            {
                var verts = ByteHelper.LoadArray<RmvWeighted2Vertex>(buffer, offset, vertexSize * vertexCount);
                for (var i = 0; i < vertexCount; i++)
                    output[i] = RmvWeighted2Vertex.ToCommon(verts[i]);
            }

            return output;
        }

        public byte[] Write(RmvVersionEnum rmvVersion, CommonVertex vertex)
        {
            if (vertex.WeightCount != 2 || vertex.BoneIndex.Length != 2 || vertex.BoneWeight.Length != 2)
                throw new Exception($"Unexpected vertex weights for {Type}");

            if (rmvVersion == RmvVersionEnum.RMV2_V8)
            {
                var typedVert = RmvWeighted2ColourVertex.ToRmv(vertex);

                return ByteHelper.GetBytes(typedVert);
            }
            else
            {
                var typedVert = RmvWeighted2Vertex.ToRmv(vertex);
                return ByteHelper.GetBytes(typedVert);
            }
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

            public static CommonVertex ToCommon(RmvWeighted2ColourVertex item)
            {
                return new CommonVertex()
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

            public static RmvWeighted2ColourVertex ToRmv(CommonVertex vertex)
            {
                var newPos = vertex.Position;
                newPos.W = 0;
                return new RmvWeighted2ColourVertex()
                {
                    Position = new HalfVector4()
                    {
                        X = new SharpDX.Half(vertex.Position.X),
                        Y = new SharpDX.Half(vertex.Position.Y),
                        Z = new SharpDX.Half(vertex.Position.Z),
                        W = new SharpDX.Half(vertex.Position.W)
                    },
                    BoneIndex = new ByteVector2()
                    {
                        X = vertex.BoneIndex[0],
                        Y = vertex.BoneIndex[1],
                    },
                    BoneWeight = new ByteVector2()
                    {
                        X = (byte)(vertex.BoneWeight[0] * 255),
                        Y = (byte)(vertex.BoneWeight[1] * 255),

                    },

                    Normal = VertexLoadHelper.CreateNormalVector3_v2(vertex.Normal),

                    Uv = new HalfVector2()
                    {
                        X = vertex.Uv.X,
                        Y = vertex.Uv.Y,
                    },

                    BiNormal = VertexLoadHelper.CreateNormalVector3_v2(vertex.BiNormal),
                    Tangent = VertexLoadHelper.CreateNormalVector3_v2(vertex.Tangent),
                    Colour = VertexLoadHelper.Create4BytesFromVector4_v2(vertex.Colour),
                };
            }
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

            public static CommonVertex ToCommon(RmvWeighted2Vertex item)
            {
                return new CommonVertex()
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

            public static RmvWeighted2Vertex ToRmv(CommonVertex vertex)
            {
                var newPos = vertex.Position;
                newPos.W = 0;
                return new RmvWeighted2Vertex()
                {
                    Position = VertexLoadHelper.CreatePositionVector4ExtraPrecision_v2(newPos),
                    BoneIndex = new ByteVector2()
                    {
                        X = vertex.BoneIndex[0],
                        Y = vertex.BoneIndex[1],
                    },
                    BoneWeight = new ByteVector2()
                    {
                        X = (byte)(vertex.BoneWeight[0] * 255),
                        Y = (byte)(vertex.BoneWeight[1] * 255),

                    },

                    Normal = VertexLoadHelper.CreateNormalVector3_v2(vertex.Normal),

                    Uv = new HalfVector2()
                    {
                        X = vertex.Uv.X,
                        Y = vertex.Uv.Y,
                    },

                    BiNormal = VertexLoadHelper.CreateNormalVector3_v2(vertex.BiNormal),
                    Tangent = VertexLoadHelper.CreateNormalVector3_v2(vertex.Tangent),
                };
            }



        }
    }
}
