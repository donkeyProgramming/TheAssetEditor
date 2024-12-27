using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc.V136
{
    public class CAkSound_v136 : HircItem, ICAkSound, INodeBaseParamsAccessor
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

        public override void UpdateSize()
        {
            var nodeBaseParams = NodeBaseParams.GetSize();
            Size = BnkChunkHeader.HeaderByteSize + AkBankSourceData.GetSize() + nodeBaseParams - 4;
        }

        public override byte[] GetAsByteArray()
        {
            using var memStream = WriteHeader();
            memStream.Write(AkBankSourceData.GetAsByteArray());
            memStream.Write(NodeBaseParams.GetAsByteArray());
            var byteArray = memStream.ToArray();

            // Reload the object to ensure sanity
            var copyInstance = new CAkSound_v136();
            copyInstance.Parse(new ByteChunk(byteArray));

            return byteArray;
        }
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
                //   throw new Exception();
            }

            if (output.PluginIdType == 0x02)
                output.USize = chunk.ReadUInt32();

            output.AkMediaInformation = AkMediaInformation.Create(chunk);

            return output;
        }

        public static uint GetSize() => 14;

        public byte[] GetAsByteArray()
        {
            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.UInt32.EncodeValue(PluginId, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue((byte)StreamType, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(AkMediaInformation.SourceId, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(AkMediaInformation.UInMemoryMediaSize, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(AkMediaInformation.USourceBits, out _));
            return memStream.ToArray();
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
