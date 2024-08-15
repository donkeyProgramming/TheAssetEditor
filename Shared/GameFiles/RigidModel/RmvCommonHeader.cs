using System.Runtime.InteropServices;
using System.Text;
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

        public static RmvCommonHeader CreateDefault()
        {
            return new RmvCommonHeader()
            {
                ModelTypeFlag = 0,
                RenderFlag = 0,

                ShaderParams = RmvShaderParams.CreateDefault()
            };
            
        }
    }
}
