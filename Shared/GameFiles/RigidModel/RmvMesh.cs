using Shared.GameFormats.RigidModel.Vertex;

namespace Shared.GameFormats.RigidModel
{
    public class RmvMesh
    {
        public CommonVertex[] VertexList { get; set; }
        public ushort[] IndexList;
    }
}

