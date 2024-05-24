using System.Runtime.InteropServices;
using Shared.GameFormats.RigidModel.Transforms;
using Shared.GameFormats.RigidModel.Types;

namespace Shared.GameFormats.RigidModel
{
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public struct RmvCommonHeader
    {
        public ModelMaterialEnum ModelTypeFlag;
        public ushort RenderFlag;
        public uint MeshSectionSize;
        public uint VertexOffset;
        public uint VertexCount;
        public uint IndexOffset;
        public uint IndexCount;

        public RvmBoundingBox BoundingBox;
        public RmvShaderParams ShaderParams;
    }
}
