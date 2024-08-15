using Shared.Core.ByteParsing;
using Shared.GameFormats.RigidModel.LodHeader;
using Shared.GameFormats.RigidModel.Vertex;

namespace Shared.GameFormats.RigidModel
{
    public enum RmvVersionEnum : uint
    {
        RMV2_V5 = 5,
        RMV2_V6 = 6,
        RMV2_V7 = 7,
        RMV2_V8 = 8,
    }

    public class RmvFile
    {
        public RmvFileHeader Header { get; set; }
        public RmvLodHeader[] LodHeaders { get; set; }
        public RmvModel[][] ModelList { get; set; }

        public RmvFile() { }

        public void RecalculateOffsets()
        {
            for (var lodIndex = 0; lodIndex < Header.LodCount; lodIndex++)
            {
                for (var modelIndex = 0; modelIndex < ModelList[lodIndex].Length; modelIndex++)
                    UpdateModelHeader(ModelList[lodIndex][modelIndex], Header.Version);
            }

            var lodHeaderSize = LodHeaderFactory.Create().GetHeaderSize(Header.Version);

            var headerOffset = (uint)(ByteHelper.GetSize<RmvFileHeader>() + lodHeaderSize * Header.LodCount);
            uint modelOffset = 0;
            for (var lodIndex = 0; lodIndex < Header.LodCount; lodIndex++)
            {
                var lodHeader = LodHeaders[lodIndex];

                lodHeader.MeshCount = (uint)ModelList[lodIndex].Length;
                lodHeader.TotalLodVertexSize = (uint)ModelList[lodIndex].Sum(x => x.CommonHeader.VertexCount * VertexFactory.Create().GetVertexSize(x.Material.BinaryVertexFormat, Header.Version));
                lodHeader.TotalLodIndexSize = (uint)ModelList[lodIndex].Sum(x => x.CommonHeader.IndexCount * sizeof(ushort));
                lodHeader.FirstMeshOffset = headerOffset + modelOffset;
                LodHeaders[lodIndex] = lodHeader;

                modelOffset += (uint)ModelList[lodIndex].Sum(x => x.CommonHeader.MeshSectionSize);
            }
        }

        void UpdateModelHeader(RmvModel model, RmvVersionEnum rmvVersion)
        {
            var headerDataSize = ByteHelper.GetSize<RmvCommonHeader>() + model.Material.ComputeSize();
            var vertexSize = VertexFactory.Create().GetVertexSize(model.Material.BinaryVertexFormat, rmvVersion);

            var header = model.CommonHeader;

            header.VertexCount = (uint)model.Mesh.VertexList.Length;
            header.IndexCount = (uint)model.Mesh.IndexList.Length;

            var totalVertSize = vertexSize * header.VertexCount;
            var totalIndexSize = header.IndexCount * sizeof(ushort);

            header.VertexOffset = (uint)headerDataSize;
            header.IndexOffset = header.VertexOffset + totalVertSize;
            header.MeshSectionSize = (uint)headerDataSize + totalVertSize + totalIndexSize;

            model.CommonHeader = header;
        }
    }
}
