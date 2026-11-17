using Microsoft.Xna.Framework;
using Shared.GameFormats.RigidModel.Vertex.Formats;

namespace Shared.GameFormats.RigidModel.Vertex
{
    public interface IVertexCreator
    {
        VertexFormat Type { get; }
        //CommonVertex Read(RmvVersionEnum rmvVersion, byte[] buffer, int offset, int vertexSize);
        CommonVertex[] ReadArray(RmvVersionEnum rmvVersion, byte[] buffer, int offset, int vertexSize, int vertexCount);
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

        public byte[] BoneIndex { get; set; }
        public float[] BoneWeight { get; set; }
        public int WeightCount { get; set; } = 0;

        /// <summary>Layout-specific channels with no field above, see ExtraVertexLayout. Empty for the common layouts.</summary>
        public float[] Extra { get; set; } = [];

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

        public uint GetVertexSize(VertexFormat format, RmvVersionEnum rmvVersion) => _vertexCreators[format].GetVertexSize(rmvVersion);

        public bool IsKnownVertex(VertexFormat format) => _vertexCreators.ContainsKey(format);

        /// <summary>
        /// The layout for a declared format id and the stride the file actually has. The common
        /// layouts are taken when their size matches; otherwise the pair picks one of the layouts
        /// several ids share (vegetation declares CustomTerrain, grass and terrain bases declare
        /// Position16, ...). Null when nothing matches; the block is then kept as raw bytes.
        /// </summary>
        public IVertexCreator? ResolveLayout(VertexFormat declared, uint stride, RmvVersionEnum version)
        {
            if (_vertexCreators.TryGetValue(declared, out var creator) && creator.GetVertexSize(version) == stride)
                return creator;

            // A material with no vertex-format field (banners, bow waves) is resolved by stride alone
            if (declared == VertexFormat.Unknown)
            {
                return stride switch
                {
                    8 => new PositionHalfVertexLayout(),
                    12 => new PositionUvVertexLayout(VertexFormat.Rome2Water),
                    20 => new SwayVertexLayout(),
                    24 => new BowWaveVertexLayout(),
                    28 => new StaticNoUv2VertexLayout(),
                    60 => new VegetationVertexLayout(),
                    _ => _vertexCreators.Values.FirstOrDefault(x => x.GetVertexSize(version) == stride && x is not Weighted2VertexCreator && x is not Weighted4VertexCreator),
                };
            }

            return (declared, stride) switch
            {
                (VertexFormat.Sway, 20) => new SwayVertexLayout(),
                (VertexFormat.Sway, 16) => _vertexCreators[VertexFormat.Position16_bit],     // Attila's campaign vegetation
                (VertexFormat.CustomTerrain, 60) => new VegetationVertexLayout(),
                (VertexFormat.Position16_bit, 28) => new GrassVertexLayout(),
                (VertexFormat.Position16_bit, 8) => new PositionHalfVertexLayout(),
                (VertexFormat.Rome2Tree, 28) => new TreeBillboardVertexLayout(),
                (VertexFormat.Rome2Tree, 12) => new PositionUvVertexLayout(VertexFormat.Rome2Tree),
                (VertexFormat.Rome2Water, 12) => new PositionUvVertexLayout(VertexFormat.Rome2Water),
                _ => null,
            };
        }

        public byte[] Save(RmvVersionEnum rmvVersion, VertexFormat vertexType, CommonVertex vertex) => _vertexCreators[vertexType].Write(rmvVersion, vertex);

        /// <summary>Sets every vertex's normal from the triangles that use it, for layouts that store none.</summary>
        public static void ComputeFaceNormals(CommonVertex[] vertexList, ushort[] indexList)
        {
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

