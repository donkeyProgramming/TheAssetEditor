using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc.V122
{
    public class CAkSound_V122 : HircItem, ICAkSound
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
        public ushort PluginId_company { get; set; }
        public SourceType StreamType { get; set; }

        public AkMediaInformation AkMediaInformation { get; set; }
        public uint USize { get; set; }
        public static AkBankSourceData Create(ByteChunk chunk)
        {
            var output = new AkBankSourceData()
            {
                PluginId = chunk.ReadUInt32(),
                //PluginId_type = chunk.ReadUShort(),
                //PluginId_company = chunk.ReadUShort(),
                StreamType = (SourceType)chunk.ReadByte()
            };

            output.PluginIdType = (ushort)(output.PluginId >> 0 & 0x000F);
            output.PluginId_company = (ushort)(output.PluginId >> 4 & 0x03FF);

            if (output.StreamType != SourceType.Streaming)
            {
                //   throw new Exception();
            }

            if (output.PluginIdType == 0x02)
                output.USize = chunk.ReadUInt32();

            output.AkMediaInformation = AkMediaInformation.Create(chunk);

            return output;
        }
    }

    public class AkMediaInformation
    {
        public uint SourceId { get; set; }
        public uint UInMemoryMediaSize { get; set; }
        public byte USourceBits { get; set; }

        public static AkMediaInformation Create(ByteChunk chunk)
        {
            return new AkMediaInformation()
            {
                SourceId = chunk.ReadUInt32(),
                UInMemoryMediaSize = chunk.ReadUInt32(),
                USourceBits = chunk.ReadByte(),
            };
        }
    }
}
