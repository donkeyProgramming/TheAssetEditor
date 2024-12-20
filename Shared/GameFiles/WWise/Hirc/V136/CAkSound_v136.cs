﻿using Shared.Core.ByteParsing;

namespace Shared.GameFormats.WWise.Hirc.V136
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
        public uint GetSourceId() => AkBankSourceData.akMediaInformation.SourceId;

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
        public ushort PluginId_type { get; set; }
        public ushort PluginId_company { get; set; }
        public SourceType StreamType { get; set; }

        public AkMediaInformation akMediaInformation { get; set; }
        public uint USize { get; set; }
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
                output.USize = chunk.ReadUInt32();

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
            memStream.Write(ByteParsers.UInt32.EncodeValue(akMediaInformation.UInMemoryMediaSize, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(akMediaInformation.USourceBits, out _));
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
