using Common;
using Filetypes.RigidModel.LodHeader;
using Serilog;
using System;
using System.IO;
using System.Linq;

namespace Filetypes.RigidModel
{
    public enum RmvVersionEnum
    { 
        RMV2_V6 = 6,
        RMV2_V7 = 7,
        RMV2_V8 = 8,
    }

    public class RmvRigidModel
    {
        public RmvModelHeader Header { get; set; }
        public RmvLodHeader[] LodHeaders { get; set; }
        public RmvSubModel[][] MeshList { get; set; }
        public string FileName { get; private set; }

        public RmvRigidModel(byte[] modelByteData, string fileName)
        { 
            ILogger logger = Logging.Create<RmvRigidModel>();
            logger.Here().Information($"Loading Rmv2RigidModel: {fileName}");
            if (modelByteData.Length == 0)
                throw new Exception("Trying to load Rmv2RigidModel with no data, data size = 0");

            FileName = fileName;
            Header = LoadModelHeader(modelByteData);
            LodHeaders = LoadLodHeaders(modelByteData);

            MeshList = new RmvSubModel[Header.LodCount][];
            for (int lodIndex = 0; lodIndex < Header.LodCount; lodIndex++)
            {
                var lodMeshCount = LodHeaders[lodIndex].MeshCount;
                MeshList[lodIndex] = new RmvSubModel[lodMeshCount];
            
                var sizeOffset = 0;
                for (int meshIndex = 0; meshIndex < lodMeshCount; meshIndex++)
                {
                    int offset = (int)LodHeaders[lodIndex].FirstMeshOffset + sizeOffset;
                    MeshList[lodIndex][meshIndex] = new RmvSubModel(modelByteData, offset, Header.Version, Header.SkeletonName);
                    
                    sizeOffset += (int)MeshList[lodIndex][meshIndex].Header.ModelSize;
                }
            }

            logger.Here().Information("Loading done");
        }

        public RmvRigidModel() { }

        RmvModelHeader LoadModelHeader(byte[] data)
        {
           return ByteHelper.ByteArrayToStructure<RmvModelHeader>(data, 0);
        }

        int GetLodHeaderSize()
        {
            if (Header.Version == 6)
                return ByteHelper.GetSize(typeof(Rmv2LodHeader_V6));
            else if(Header.Version == 7 || Header.Version == 8)
                return ByteHelper.GetSize(typeof(Rmv2LodHeader_V7));
            
            throw new Exception("Unknown rmv2 version - " + Header.Version);
        }

        RmvLodHeader[] LoadLodHeaders(byte[] data)
        {
            var offset = ByteHelper.GetSize(typeof(RmvModelHeader));
            var lodHeaderSize = GetLodHeaderSize();

            var lodHeaders = new RmvLodHeader[Header.LodCount];
            for (int i = 0; i < Header.LodCount; i++)
            {
                RmvLodHeader header;
                if (Header.Version == 6)
                    header = ByteHelper.ByteArrayToStructure<Rmv2LodHeader_V6>(data, offset + lodHeaderSize * i);
                else
                    header = ByteHelper.ByteArrayToStructure<Rmv2LodHeader_V7>(data, offset + lodHeaderSize * i);

                lodHeaders[i] = header;
            }

            return lodHeaders;
        }

        public void SaveToByteArray(BinaryWriter writer)
        {
            writer.Write(ByteHelper.GetBytes(Header));

            if (Header.Version == 7 || Header.Version == 8)
            {
                for (int i = 0; i < LodHeaders.Length; i++)
                    writer.Write(ByteHelper.GetBytes((Rmv2LodHeader_V7)LodHeaders[i]));
            }
            else if (Header.Version == 6)
            {
                for (int i = 0; i < LodHeaders.Length; i++)
                    writer.Write(ByteHelper.GetBytes((Rmv2LodHeader_V6)LodHeaders[i]));
            }
            else
            {
                throw new Exception("Not a know version - can not save");
            }

            for (int lodIndex = 0; lodIndex < Header.LodCount; lodIndex++)
            {
                var modelList = MeshList[lodIndex];
                for(var modelIndex = 0; modelIndex < modelList.Length; modelIndex++)
                {
                    var model = modelList[modelIndex];
                    writer.Write(ByteHelper.GetBytes(model.Header));
                    for (var attachmentPointIndex = 0; attachmentPointIndex < model.AttachmentPoints.Count; attachmentPointIndex++)
                        writer.Write(ByteHelper.GetBytes(model.AttachmentPoints[attachmentPointIndex]));

                    for (var textureIndex = 0; textureIndex < model.Textures.Count; textureIndex++)
                        writer.Write(ByteHelper.GetBytes(model.Textures[textureIndex]));
                    
                    writer.Write(ByteHelper.GetBytes(model.AlphaSettings));

                    var header = model.Header;
                    model.Mesh.SaveToByteArray(writer, model.Header.VertextType);
                }
            }
        }


        public void UpdateOffsets()
        {
            for (int lodIndex = 0; lodIndex < Header.LodCount; lodIndex++)
            {
                for (var modelIndex = 0; modelIndex < MeshList[lodIndex].Length; modelIndex++)
                {
                    var header = MeshList[lodIndex][modelIndex].Header;
                    header.UpdateHeader(MeshList[lodIndex][modelIndex].Mesh, MeshList[lodIndex][modelIndex].Textures, MeshList[lodIndex][modelIndex].AttachmentPoints);
                    MeshList[lodIndex][modelIndex].Header = header;
                }
            }
            var headerOffset = (uint)(ByteHelper.GetSize<RmvModelHeader>() + GetLodHeaderSize() * Header.LodCount);
            uint modelOffset = 0;
            for (int lodIndex = 0; lodIndex < Header.LodCount; lodIndex++)
            {
                var lodHeader = LodHeaders[lodIndex];
                lodHeader.MeshCount = (uint)MeshList[lodIndex].Length;
                lodHeader.TotalLodVertexSize = (uint)MeshList[lodIndex].Sum(x=>x.Header.VertexCount * RmvMesh.GetVertexSize(x.Header.VertextType, Header.Version));
                lodHeader.TotalLodIndexSize = (uint)MeshList[lodIndex].Sum(x => x.Header.FaceCount * sizeof(ushort));
                lodHeader.FirstMeshOffset = headerOffset + modelOffset;
                LodHeaders[lodIndex] = lodHeader;

                modelOffset += (uint)MeshList[lodIndex].Sum(x => x.Header.ModelSize);
            }
        }
    }
}
