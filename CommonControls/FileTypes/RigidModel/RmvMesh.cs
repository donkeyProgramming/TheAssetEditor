using CommonControls.FileTypes.RigidModel.Vertex;

namespace CommonControls.FileTypes.RigidModel
{
    public class RmvMesh
    {
        public CommonVertex[] VertexList { get; set; }
        public ushort[] IndexList;
    }
}

