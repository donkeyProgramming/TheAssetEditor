using Filetypes.ByteParsing;
using System;

namespace Audio.FileFormats.WWise.Hirc.V112
{
    public class CAkSound_V112 : HircItem, ICAkSound
    {
        public AkBankSourceData AkBankSourceData { get; set; }
        public NodeBaseParams NodeBaseParams { get; set; }

        protected override void CreateSpecificData(ByteChunk chunk)
        {
            AkBankSourceData = AkBankSourceData.Create(chunk);
            NodeBaseParams = NodeBaseParams.Create(chunk);
        }

        public uint GetDirectParentId() => NodeBaseParams.DirectParentID;
        public uint GetSourceId() => AkBankSourceData.akMediaInformation.SourceId;

        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();
    }

    public class AkBankSourceData
    {
        public uint PluginId { get; set; }
        public ushort PluginId_type { get; set; }
        public ushort PluginId_company { get; set; }
        public SourceType StreamType { get; set; }

        public AkMediaInformation akMediaInformation { get; set; }
        public uint uSize { get; set; }
        public static AkBankSourceData Create(ByteChunk chunk)
        {
            var output = new AkBankSourceData()
            {
                PluginId = chunk.ReadUInt32(),
                //PluginId_type = chunk.ReadUShort(),
                //PluginId_company = chunk.ReadUShort(),
                StreamType = (SourceType)chunk.ReadByte()
            };


            output.PluginId_type = (ushort)(output.PluginId >> 0 & 0x000F);
            output.PluginId_company = (ushort)(output.PluginId >> 4 & 0x03FF);

            if (output.StreamType != SourceType.Streaming)
            {
                //   throw new Exception();
            }

            if (output.PluginId_type == 0x02)
                output.uSize = chunk.ReadUInt32();

            output.akMediaInformation = AkMediaInformation.Create(chunk, output.StreamType);

            return output;
        }
    }


    public class AkMediaInformation
    {
        public uint SourceId { get; set; }
        public uint FileId { get; set; }
        public uint uFileOffset { get; set; }
        public uint uInMemoryMediaSize { get; set; }
        public byte uSourceBits { get; set; }

        public static AkMediaInformation Create(ByteChunk chunk, SourceType sourceType)
        {
            var instance = new AkMediaInformation();
            instance.SourceId = chunk.ReadUInt32();
            instance.FileId = chunk.ReadUInt32();
            if (sourceType == SourceType.Data_BNK)
                instance.uFileOffset = chunk.ReadUInt32();
            instance.uInMemoryMediaSize = chunk.ReadUInt32();
            instance.uSourceBits = chunk.ReadByte();
            return instance;

        }
    }
}
