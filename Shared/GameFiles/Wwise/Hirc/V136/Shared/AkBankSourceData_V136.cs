using Shared.Core.ByteParsing;
using Shared.GameFormats.Wwise.Enums;

namespace Shared.GameFormats.Wwise.Hirc.V136.Shared
{
    public class AkBankSourceData_V136
    {
        public uint PluginId { get; set; }
        public AkPluginType_V136 PluginIdType { get; set; }
        public ushort PluginIdCompany { get; set; }
        public AKBKSourceType StreamType { get; set; }
        public AkMediaInformation_V136 AkMediaInformation { get; set; } = new AkMediaInformation_V136();
        public uint Size { get; set; }

        public static AkBankSourceData_V136 ReadData(ByteChunk chunk)
        {
            var akBankSourceData_V136 = new AkBankSourceData_V136();
            akBankSourceData_V136.PluginId = chunk.ReadUInt32();
            akBankSourceData_V136.PluginIdType = (AkPluginType_V136)(ushort)(akBankSourceData_V136.PluginId >> 0 & 0x000F);
            akBankSourceData_V136.PluginIdCompany = (ushort)(akBankSourceData_V136.PluginId >> 4 & 0x03FF); // Apparently CA doesn't have one
            akBankSourceData_V136.StreamType = (AKBKSourceType)chunk.ReadByte();
            akBankSourceData_V136.AkMediaInformation.ReadData(chunk); 
            
            if (akBankSourceData_V136.PluginIdType == AkPluginType_V136.Source)
                akBankSourceData_V136.Size = chunk.ReadUInt32();

            return akBankSourceData_V136;
        }

        public byte[] WriteData()
        {
            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.UInt32.EncodeValue(PluginId, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue((byte)StreamType, out _));
            memStream.Write(AkMediaInformation.WriteData());
            if (PluginIdType == AkPluginType_V136.Source)
                memStream.Write(ByteParsers.UInt32.EncodeValue(Size, out _));
            return memStream.ToArray();
        }

        public uint GetSize()
        {
            var pluginIdSize = ByteHelper.GetPropertyTypeSize(PluginId);
            var streamTypeSize = ByteHelper.GetPropertyTypeSize((byte)StreamType);
            var mediaInfoSize = AkMediaInformation.GetSize();

            uint sizeSize = 0;
            if (PluginIdType == AkPluginType_V136.Source)
                sizeSize += (uint)ByteHelper.GetPropertyTypeSize(Size);

            return pluginIdSize + streamTypeSize + mediaInfoSize + sizeSize;
        }

        public class AkMediaInformation_V136
        {
            public uint SourceId { get; set; }
            public uint InMemoryMediaSize { get; set; }
            public byte SourceBits { get; set; }

            public void ReadData(ByteChunk chunk)
            {
                SourceId = chunk.ReadUInt32();
                InMemoryMediaSize = chunk.ReadUInt32();
                SourceBits = chunk.ReadByte();
            }

            public byte[] WriteData()
            {
                using var memStream = new MemoryStream();
                memStream.Write(ByteParsers.UInt32.EncodeValue(SourceId, out _));
                memStream.Write(ByteParsers.UInt32.EncodeValue(InMemoryMediaSize, out _));
                memStream.Write(ByteParsers.Byte.EncodeValue(SourceBits, out _));
                return memStream.ToArray();
            }

            public uint GetSize()
            {
                var sourceIdSize = ByteHelper.GetPropertyTypeSize(SourceId);
                var mediaSizeSize = ByteHelper.GetPropertyTypeSize(InMemoryMediaSize);
                var sourceBitsSize = ByteHelper.GetPropertyTypeSize(SourceBits);
                return sourceIdSize + mediaSizeSize + sourceBitsSize;
            }
        }
    }
}
