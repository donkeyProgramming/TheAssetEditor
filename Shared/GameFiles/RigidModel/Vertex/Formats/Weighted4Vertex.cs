using Microsoft.Xna.Framework;
using Shared.ByteParsing;
using Shared.GameFormats.RigidModel.Transforms;

namespace Shared.GameFormats.RigidModel.Vertex.Formats
{
    public class Weighted4VertexCreator : IVertexCreator
    {
        public VertexFormat Type => VertexFormat.Cinematic;
        public uint GetVertexSize(RmvVersionEnum rmvVersion)
        {
            if (rmvVersion == RmvVersionEnum.RMV2_V8)
                return (uint)ByteHelper.GetSize<RmvWeighted4ColourVertex>();
            else
                return (uint)ByteHelper.GetSize<RmvWeighted4Vertex>();
        }

        public bool ForceComputeNormals => false;

        public CommonVertex[] ReadArray(RmvVersionEnum rmvVersion, byte[] buffer, int offset, int vertexSize, int vertexCount)
        {
            var output = new CommonVertex[vertexCount];

            if (rmvVersion == RmvVersionEnum.RMV2_V8)
            {
                var verts = ByteHelper.LoadArray<RmvWeighted4ColourVertex>(buffer, offset, vertexSize * vertexCount);
                for (var i = 0; i < vertexCount; i++)
                    output[i] = RmvWeighted4ColourVertex.ToCommon(verts[i]);
            }
            else
            {
                var verts = ByteHelper.LoadArray<RmvWeighted4Vertex>(buffer, offset, vertexSize * vertexCount);
                for (var i = 0; i < vertexCount; i++)
                    output[i] = RmvWeighted4Vertex.ToCommon(verts[i]);
            }

            return output;
        }

        public byte[] Write(RmvVersionEnum rmvVersion, CommonVertex vertex)
        {
            if (vertex.WeightCount != 4 || vertex.BoneIndex.Length != 4 || vertex.BoneWeight.Length != 4)
                throw new Exception($"Unexpected vertex weights for {Type}");

            if (rmvVersion == RmvVersionEnum.RMV2_V8)
            {
                var typedVert = RmvWeighted4ColourVertex.ToRmv(rmvVersion, vertex);
                return ByteHelper.GetBytes(typedVert);
            }
            else
            {
                var typedVert = RmvWeighted4Vertex.ToRmv(rmvVersion, vertex);
                var bytes = ByteHelper.GetBytes(typedVert);
                return bytes;
            }
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

            public static CommonVertex ToCommon(RmvWeighted4ColourVertex item)
            {
                return new CommonVertex()
                {
                    Position = VertexLoadHelper.CreatVector4HalfFloat2(item.Position.X, item.Position.Y, item.Position.Z, item.Position.W),

                    Normal = VertexLoadHelper.CreatVector3_FromByte(item.Normal),
                    BiNormal = VertexLoadHelper.CreatVector3_FromByte(item.BiNormal),
                    Tangent = VertexLoadHelper.CreatVector3_FromByte(item.Tangent),
                    Uv = new Vector2(item.Uv.X, item.Uv.Y),
                    Colour = VertexLoadHelper.CreateColourFromBytes(item.Colour.X, item.Colour.Y, item.Colour.Z, item.Colour.W),

                    BoneIndex = [item.BoneIndex.X, item.BoneIndex.Y, item.BoneIndex.Z, item.BoneIndex.W],
                    BoneWeight = [item.BoneWeight.X / 255.0f, item.BoneWeight.Y / 255.0f, item.BoneWeight.Z / 255.0f, item.BoneWeight.W / 255.0f],
                    WeightCount = 4
                };
            }

            public static RmvWeighted4ColourVertex ToRmv(RmvVersionEnum version, CommonVertex vertex)
            {
                return new RmvWeighted4ColourVertex()
                {
                    Position = VertexLoadHelper.CreatePositionForVersion(vertex.Position, version, isSkinned: true),
                    BoneIndex = new ByteVector4()
                    {
                        X = vertex.BoneIndex[0],
                        Y = vertex.BoneIndex[1],
                        Z = vertex.BoneIndex[2],
                        W = vertex.BoneIndex[3]
                    },
                    BoneWeight = new ByteVector4()
                    {
                        X = (byte)(vertex.BoneWeight[0] * 255),
                        Y = (byte)(vertex.BoneWeight[1] * 255),
                        Z = (byte)(vertex.BoneWeight[2] * 255),
                        W = (byte)(vertex.BoneWeight[3] * 255)
                    },

                    Normal = VertexLoadHelper.CreateNormalVector3_v2(vertex.Normal),

                    Uv = new HalfVector2()
                    {
                        X = vertex.Uv.X,
                        Y = vertex.Uv.Y,
                    },

                    BiNormal = VertexLoadHelper.CreateNormalVector3_v2(vertex.BiNormal),
                    Tangent = VertexLoadHelper.CreateNormalVector3_v2(vertex.Tangent),
                    Colour = VertexLoadHelper.CreateColourBytes(vertex.Colour),
                };
            }
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

            public static CommonVertex ToCommon(RmvWeighted4Vertex item)
            {
                return new CommonVertex()
                {
                    Position = VertexLoadHelper.CreatVector4HalfFloat2(item.Position.X, item.Position.Y, item.Position.Z, item.Position.W),

                    Normal = VertexLoadHelper.CreatVector3_FromByte(item.Normal),
                    BiNormal = VertexLoadHelper.CreatVector3_FromByte(item.BiNormal),
                    Tangent = VertexLoadHelper.CreatVector3_FromByte(item.Tangent),
                    Uv = new Vector2(item.Uv.X, item.Uv.Y),
                    Colour = Vector4.Zero,

                    BoneIndex = [item.BoneIndex.X, item.BoneIndex.Y, item.BoneIndex.Z, item.BoneIndex.W],
                    BoneWeight = [item.BoneWeight.X / 255.0f, item.BoneWeight.Y / 255.0f, item.BoneWeight.Z / 255.0f, item.BoneWeight.W / 255.0f],
                    WeightCount = 4
                };
            }

            public static RmvWeighted4Vertex ToRmv(RmvVersionEnum version, CommonVertex vertex)
            {
                return new RmvWeighted4Vertex()
                {
                    Position = VertexLoadHelper.CreatePositionForVersion(vertex.Position, version, isSkinned: true),
                    BoneIndex = new ByteVector4()
                    {
                        X = vertex.BoneIndex[0],
                        Y = vertex.BoneIndex[1],
                        Z = vertex.BoneIndex[2],
                        W = vertex.BoneIndex[3]
                    },

                    BoneWeight = new ByteVector4()
                    {
                        X = (byte)(vertex.BoneWeight[0] * 255),
                        Y = (byte)(vertex.BoneWeight[1] * 255),
                        Z = (byte)(vertex.BoneWeight[2] * 255),
                        W = (byte)(vertex.BoneWeight[3] * 255)
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
