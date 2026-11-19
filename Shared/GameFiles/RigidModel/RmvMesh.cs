using Shared.GameFormats.RigidModel.Vertex;

namespace Shared.GameFormats.RigidModel
{
    public class RmvMesh
    {
        /// <summary>Indices are 16-bit, so a mesh can address at most this many vertices.</summary>
        public const int MaxVertexCount = ushort.MaxValue + 1;

        public CommonVertex[] VertexList { get; set; }
        public ushort[] IndexList;
    }
}

