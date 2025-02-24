using Microsoft.Xna.Framework;
using Shared.GameFormats.RigidModel.Vertex.Formats;

namespace Shared.GameFormats.RigidModel.Vertex
{
    public interface IVertexCreator
    {
        VertexFormat Type { get; }
        CommonVertex Read(RmvVersionEnum rmvVersion, byte[] buffer, int offset, int vertexSize);
        byte[] Write(RmvVersionEnum rmvVersion, CommonVertex vertex);
        uint GetVertexSize(RmvVersionEnum rmvVersion);
        bool ForceComputeNormals { get; }
    }

    public class CommonVertex
    {
        public Vector4 Position { get; set; }
        public Vector3 Normal { get; set; }
        public Vector3 BiNormal { get; set; }
        public Vector3 Tangent { get; set; }
        public Vector2 Uv { get; set; }
        public Vector2 Uv1 { get; set; }
        public Vector4 Colour { get; set; }

        public byte[] BoneIndex;
        public float[] BoneWeight;
        public int WeightCount { get; set; } = 0;

        public Vector3 GetPosistionAsVec3() => new Vector3(Position.X, Position.Y, Position.Z);
    }

    public class VertexFactory
    {
        Dictionary<VertexFormat, IVertexCreator> _vertexCreators = new Dictionary<VertexFormat, IVertexCreator>();
        public VertexFactory()
        {
            _vertexCreators[VertexFormat.Static] = new StaticVertexCreator();
            _vertexCreators[VertexFormat.Position16_bit] = new Position16_bitVertexCreator();
            _vertexCreators[VertexFormat.CustomTerrain] = new CustomTerrainVertexCreator();
            _vertexCreators[VertexFormat.CustomTerrain2] = new CustomTerrain2VertexCreator();
            _vertexCreators[VertexFormat.Collision_Format] = new CollisionVertexCreator();

            _vertexCreators[VertexFormat.Weighted] = new Weighted2VertexCreator();
            _vertexCreators[VertexFormat.Cinematic] = new Weighted4VertexCreator();
        }

        public static VertexFactory Create() => new VertexFactory();

        public CommonVertex[] CreateVertexFromBytes(RmvVersionEnum rmvVersion, VertexFormat format, byte[] buffer, int vertexCount, int vertexStart, int vertexSize)
        {
            var creator = _vertexCreators[format];

            var vertexList = new CommonVertex[vertexCount];
            for (var i = 0; i < vertexCount; i++)
                vertexList[i] = creator.Read(rmvVersion, buffer, vertexStart + i * vertexSize, vertexSize);
            return vertexList;
        }

        public uint GetVertexSize(VertexFormat format, RmvVersionEnum rmvVersion) => _vertexCreators[format].GetVertexSize(rmvVersion);

        public bool IsKnownVertex(VertexFormat format) => _vertexCreators.ContainsKey(format);

        public byte[] Save(RmvVersionEnum rmvVersion, VertexFormat vertexType, CommonVertex vertex) => _vertexCreators[vertexType].Write(rmvVersion, vertex);

        public void ReComputeNormals(VertexFormat binaryVertexFormat, ref CommonVertex[] vertexList, ref ushort[] indexList)
        {
            if (!_vertexCreators[binaryVertexFormat].ForceComputeNormals)
                return;

            for (var i = 0; i < indexList.Length; i += 3)
            {
                var index0 = indexList[i + 0];
                var index1 = indexList[i + 1];
                var index2 = indexList[i + 2];

                var u = vertexList[index0].GetPosistionAsVec3() - vertexList[index1].GetPosistionAsVec3();
                var v = vertexList[index0].GetPosistionAsVec3() - vertexList[index2].GetPosistionAsVec3();
                var normal = Vector3.Normalize(Vector3.Cross(u, v)) * -1;

                vertexList[index0].Normal = normal;
                vertexList[index1].Normal = normal;
                vertexList[index2].Normal = normal;
            }
        }
    }
}

