using Common;
using Filetypes.RigidModel.LodHeader;
using Serilog;
using System;
using System.IO;
using System.Linq;

namespace Filetypes.RigidModel
{
    public enum RmvVersionEnum : uint
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

        public RmvRigidModel(byte[] modelByteData)
        { 
            ILogger logger = Logging.Create<RmvRigidModel>();
            logger.Here().Information($"Loading Rmv2RigidModel");
            if (modelByteData.Length == 0)
                throw new Exception("Trying to load Rmv2RigidModel with no data, data size = 0");

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
                    MeshList[lodIndex][meshIndex] = new RmvSubModel(modelByteData, offset, Header.Version);
                    
                    sizeOffset += (int)MeshList[lodIndex][meshIndex].Header.MeshSectionSize;
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
            if (Header.Version == RmvVersionEnum.RMV2_V6)
                return ByteHelper.GetSize(typeof(Rmv2LodHeader_V6));
            else if(Header.Version == RmvVersionEnum.RMV2_V7 || Header.Version == RmvVersionEnum.RMV2_V8)
                return ByteHelper.GetSize(typeof(Rmv2LodHeader_V7_V8));
            
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
                if (Header.Version == RmvVersionEnum.RMV2_V6)
                    header = ByteHelper.ByteArrayToStructure<Rmv2LodHeader_V6>(data, offset + lodHeaderSize * i);
                else
                    header = ByteHelper.ByteArrayToStructure<Rmv2LodHeader_V7_V8>(data, offset + lodHeaderSize * i);

                lodHeaders[i] = header;
            }

            return lodHeaders;
        }

        public void SaveToByteArray(BinaryWriter writer)
        {
            writer.Write(ByteHelper.GetBytes(Header));

            if (Header.Version == RmvVersionEnum.RMV2_V7 || Header.Version == RmvVersionEnum.RMV2_V8)
            {
                for (int i = 0; i < LodHeaders.Length; i++)
                    writer.Write(ByteHelper.GetBytes((Rmv2LodHeader_V7_V8)LodHeaders[i]));
            }
            else if (Header.Version == RmvVersionEnum.RMV2_V6)
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

                    for (var stringIndex = 0; stringIndex < model.StringParams.Count; stringIndex++)
                    {
                        writer.Write(ByteParsing.ByteParsers.Int32.EncodeValue(stringIndex, out _));
                        writer.Write(ByteParsing.ByteParsers.String.Encode(model.StringParams[stringIndex], out _));
                    }

                    for (var floatIndex = 0; floatIndex < model.FloatParams.Count; floatIndex++)
                    {
                        writer.Write(ByteParsing.ByteParsers.Int32.EncodeValue(floatIndex, out _));
                        writer.Write(ByteParsing.ByteParsers.Single.EncodeValue(model.FloatParams[floatIndex], out _));
                    }

                    for (var intIndex = 0; intIndex < model.IntParams.Count; intIndex++)
                    {
                        writer.Write(ByteParsing.ByteParsers.Int32.EncodeValue(intIndex, out _));
                        writer.Write(ByteParsing.ByteParsers.Int32.EncodeValue(model.IntParams[intIndex], out _));
                    }

                    for (var vec4Index = 0; vec4Index < model.Vec4Params.Count; vec4Index++)
                    {
                        writer.Write(ByteParsing.ByteParsers.Int32.EncodeValue(vec4Index, out _));
                        writer.Write(ByteParsing.ByteParsers.Single.EncodeValue(model.Vec4Params[vec4Index].X, out _));
                        writer.Write(ByteParsing.ByteParsers.Single.EncodeValue(model.Vec4Params[vec4Index].Y, out _));
                        writer.Write(ByteParsing.ByteParsers.Single.EncodeValue(model.Vec4Params[vec4Index].Z, out _));
                        writer.Write(ByteParsing.ByteParsers.Single.EncodeValue(model.Vec4Params[vec4Index].W, out _));
                    }

                    model.Mesh.SaveToByteArray(writer);
                }
            }
        }

        public void UpdateOffsets(RmvVersionEnum version)
        {
            for (int lodIndex = 0; lodIndex < Header.LodCount; lodIndex++)
            {
                for (var modelIndex = 0; modelIndex < MeshList[lodIndex].Length; modelIndex++)
                {
                    var header = MeshList[lodIndex][modelIndex].Header;
                    header.UpdateHeader(MeshList[lodIndex][modelIndex].Mesh, MeshList[lodIndex][modelIndex], version);
                    MeshList[lodIndex][modelIndex].Header = header;
                }
            }

            var headerOffset = (uint)(ByteHelper.GetSize<RmvModelHeader>() + GetLodHeaderSize() * Header.LodCount);
            uint modelOffset = 0;
            for (int lodIndex = 0; lodIndex < Header.LodCount; lodIndex++)
            {
                var lodHeader = LodHeaders[lodIndex];
                lodHeader.MeshCount = (uint)MeshList[lodIndex].Length;
                lodHeader.TotalLodVertexSize = (uint)MeshList[lodIndex].Sum(x=>x.Header.VertexCount * RmvMesh.GetVertexSize(x.Header.VertextType, version, out _));
                lodHeader.TotalLodIndexSize = (uint)MeshList[lodIndex].Sum(x => x.Header.IndexCount * sizeof(ushort));
                lodHeader.FirstMeshOffset = headerOffset + modelOffset;
                LodHeaders[lodIndex] = lodHeader;

                modelOffset += (uint)MeshList[lodIndex].Sum(x => x.Header.MeshSectionSize);
            }
        }
    }
}
