using Shared.GameFormats.RigidModel.Vertex;
using Shared.GameFormats.RigidModel.Vertex.Formats;

namespace Shared.GameFormats.RigidModel
{
    public class RmvMesh
    {
        public CommonVertex[] VertexList { get; set; }
        public ushort[] IndexList;

        /// <summary>
        /// The vertex block as it was read, for a layout this code does not decode (banners,
        /// bow waves, vegetation, sway, ...). VertexList is empty then, and the bytes are written
        /// back unchanged. Null for every decoded mesh.
        /// </summary>
        public byte[]? RawVertexData { get; set; }

        /// <summary>
        /// Set when the layout came from the stride rather than from the declared format id
        /// (vegetation, grass, sway, banners, ...), and used to write the vertices back. Null when
        /// the declared id names the layout.
        /// </summary>
        public IVertexCreator? VertexLayout { get; set; }

        /// <summary>
        /// The section is laid out material, indices, vertices, and its declared size stops at the
        /// vertex block. Only a tree's generated billboard LOD does this, always as the last mesh.
        /// </summary>
        public bool IndicesFirst { get; set; }

        /// <summary>
        /// Whatever the section holds after its index block. A rope has no indices of its own and
        /// keeps the game's triangulation of its tube here (212, 404 and 788 u16 for 9, 17 and 33
        /// nodes); the section size is authoritative, so the bytes stay with the mesh.
        /// </summary>
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

