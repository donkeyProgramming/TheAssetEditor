using Filetypes.ByteParsing;
using System.IO;

namespace Audio.FileFormats.WWise.Hirc.V136
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

        public uint GetDirectParentId() => NodeBaseParams.DirectParentID;
        public uint GetSourceId() => AkBankSourceData.akMediaInformation.SourceId;

        public override void UpdateSize()
        {
            Size = BnkChunkHeader.HeaderByteSize + AkBankSourceData.GetSize() + NodeBaseParams.GetSize() - 4;
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

            output.akMediaInformation = AkMediaInformation.Create(chunk);

            return output;
        }

        public uint GetSize() => 14;

        public byte[] GetAsByteArray()
        {
            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.UInt32.EncodeValue(PluginId, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue((byte)StreamType, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(akMediaInformation.SourceId, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(akMediaInformation.uInMemoryMediaSize, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(akMediaInformation.uSourceBits, out _));
            return memStream.ToArray();
        }


    }

    public class AkMediaInformation
    {
        public uint SourceId { get; set; }
        public uint uInMemoryMediaSize { get; set; }
        public byte uSourceBits { get; set; }

        public static AkMediaInformation Create(ByteChunk chunk)
        {
            return new AkMediaInformation()
            {
                SourceId = chunk.ReadUInt32(),
                uInMemoryMediaSize = chunk.ReadUInt32(),
                uSourceBits = chunk.ReadByte(),
            };
        }
    }
}
