using Shared.GameFormats.RigidModel.Vertex;
using Shared.GameFormats.RigidModel.Vertex.Formats;

namespace Shared.GameFormats.RigidModel
{
    public class RmvMesh
    {
        /// <summary>Indices are 16-bit, so a mesh can address at most this many vertices.</summary>
        public const int MaxVertexCount = ushort.MaxValue + 1;

        public CommonVertex[] VertexList { get; set; }
        public ushort[] IndexList;

        /// <summary>The vertex block as read, for a layout that is not decoded (VertexList is then empty). Null otherwise.</summary>
        public byte[]? RawVertexData { get; set; }

        /// <summary>The layout picked by stride when the declared format id doesn't name it. Null otherwise.</summary>
        public IVertexCreator? VertexLayout { get; set; }

        /// <summary>Laid out material, indices, vertices: a tree's billboard LOD, always the last mesh.</summary>
        public bool IndicesFirst { get; set; }

        /// <summary>Whatever the section holds after its index block, such as a rope's tube triangulation.</summary>
        public byte[] SectionTail { get; set; } = [];

        /// <summary>Bytes the vertex block takes in the file.</summary>
        public uint GetVertexBlockSize(VertexFormat format, RmvVersionEnum version)
        {
            if (RawVertexData != null)
                return (uint)RawVertexData.Length;
            if (VertexList.Length == 0)
                return 0;
            var vertexSize = VertexLayout?.GetVertexSize(version) ?? VertexFactory.Create().GetVertexSize(format, version);
            return (uint)VertexList.Length * vertexSize;
        }
    }
}

