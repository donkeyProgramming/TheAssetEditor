using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc.V112
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

        public uint GetDirectParentId() => NodeBaseParams.DirectParentId;
        public uint GetSourceId() => AkBankSourceData.AkMediaInformation.SourceId;
        public SourceType GetStreamType() => AkBankSourceData.StreamType;

        public override void UpdateSize() => throw new NotImplementedException();
        public override byte[] GetAsByteArray() => throw new NotImplementedException();
    }

    public class AkBankSourceData
    {
        public uint PluginId { get; set; }
        public ushort PluginIdType { get; set; }
        public ushort PluginIdCompany { get; set; }
        public SourceType StreamType { get; set; }
        public AkMediaInformation AkMediaInformation { get; set; }
        public uint USize { get; set; }

        public static AkBankSourceData Create(ByteChunk chunk)
        {
            var output = new AkBankSourceData()
            {
                PluginId = chunk.ReadUInt32(),
                StreamType = (SourceType)chunk.ReadByte()
            };


            output.PluginIdType = (ushort)(output.PluginId >> 0 & 0x000F);
            output.PluginIdCompany = (ushort)(output.PluginId >> 4 & 0x03FF);

            if (output.StreamType != SourceType.Streaming)
            {
                //   throw new Exception()
            }

            if (output.PluginIdType == 0x02)
                output.USize = chunk.ReadUInt32();

            output.AkMediaInformation = AkMediaInformation.Create(chunk, output.StreamType);

            return output;
        }
    }

    public class AkMediaInformation
    {
        public uint SourceId { get; set; }
        public uint FileId { get; set; }
        public uint UFileOffset { get; set; }
        public uint UInMemoryMediaSize { get; set; }
        public byte USourceBits { get; set; }

        public static AkMediaInformation Create(ByteChunk chunk, SourceType sourceType)
        {
            var instance = new AkMediaInformation();
            instance.SourceId = chunk.ReadUInt32();
            instance.FileId = chunk.ReadUInt32();
            if (sourceType == SourceType.Data_BNK)
                instance.UFileOffset = chunk.ReadUInt32();
            instance.UInMemoryMediaSize = chunk.ReadUInt32();
            instance.USourceBits = chunk.ReadByte();
            return instance;
        }
    }
}
