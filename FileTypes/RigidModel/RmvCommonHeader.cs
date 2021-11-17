using Filetypes.RigidModel.Transforms;
using System;
using System.Runtime.InteropServices;

namespace Filetypes.RigidModel
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
