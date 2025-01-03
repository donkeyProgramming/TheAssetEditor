using Shared.Core.ByteParsing;
using Shared.GameFormats.Wwise.Enums;
using Shared.GameFormats.Wwise.Hirc.V136.Shared;

namespace Shared.GameFormats.Wwise.Hirc.V112.Shared
{
    public class AkBankSourceData_V112
    {
        public uint PluginId { get; set; }
        public AkPluginType_V112 PluginIdType { get; set; }
        public ushort PluginIdCompany { get; set; }
        public AKBKSourceType StreamType { get; set; }
        public AkMediaInformation_V112 AkMediaInformation { get; set; } = new AkMediaInformation_V112();
        public uint Size { get; set; }

        public static AkBankSourceData_V112 ReadData(ByteChunk chunk)
        {
            var akBankSourceData_V112 = new AkBankSourceData_V112();
            akBankSourceData_V112.PluginId = chunk.ReadUInt32();
            akBankSourceData_V112.PluginIdType = (AkPluginType_V112)(ushort)(akBankSourceData_V112.PluginId >> 0 & 0x000F);
            akBankSourceData_V112.PluginIdCompany = (ushort)(akBankSourceData_V112.PluginId >> 4 & 0x03FF); // Apparently CA doesn't have one
            akBankSourceData_V112.StreamType = (AKBKSourceType)chunk.ReadByte();
            akBankSourceData_V112.AkMediaInformation.ReadData(chunk, akBankSourceData_V112.StreamType);

            if (akBankSourceData_V112.PluginIdType == AkPluginType_V112.Source || akBankSourceData_V112.PluginIdType == AkPluginType_V112.MotionSource)
                akBankSourceData_V112.Size = chunk.ReadUInt32();

            return akBankSourceData_V112;
        }

        public byte[] WriteData()
        {
            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.UInt32.EncodeValue(PluginId, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue((byte)StreamType, out _));
            memStream.Write(AkMediaInformation.WriteData(StreamType));

            if (PluginIdType == AkPluginType_V112.Source || PluginIdType == AkPluginType_V112.MotionSource)
                memStream.Write(ByteParsers.UInt32.EncodeValue(Size, out _));

            return memStream.ToArray();
        }

        public uint GetSize()
        {
            var pluginIdSize = ByteHelper.GetPropertyTypeSize(PluginId);
            var streamTypeSize = ByteHelper.GetPropertyTypeSize((byte)StreamType);
            var mediaInfoSize = AkMediaInformation.GetSize(StreamType);

            uint sizeSize = 0;
            if (PluginIdType == AkPluginType_V112.Source || PluginIdType == AkPluginType_V112.MotionSource)
                sizeSize = (uint)ByteHelper.GetPropertyTypeSize(Size);

            return (uint)(pluginIdSize + streamTypeSize + mediaInfoSize + sizeSize);
        }

        public class AkMediaInformation_V112
        {
            public uint SourceId { get; set; }
            public uint FileId { get; set; }
            public uint FileOffset { get; set; }
            public uint InMemoryMediaSize { get; set; }
            public byte SourceBits { get; set; }

            public void ReadData(ByteChunk chunk, AKBKSourceType sourceType)
            {
                SourceId = chunk.ReadUInt32();
                FileId = chunk.ReadUInt32();

                if (sourceType != AKBKSourceType.Streaming)
                    FileOffset = chunk.ReadUInt32();

                InMemoryMediaSize = chunk.ReadUInt32();
                SourceBits = chunk.ReadByte();
            }

            public byte[] WriteData(AKBKSourceType sourceType)
            {
                using var memStream = new MemoryStream();
                memStream.Write(ByteParsers.UInt32.EncodeValue(SourceId, out _));
                memStream.Write(ByteParsers.UInt32.EncodeValue(FileId, out _));

                if (sourceType != AKBKSourceType.Streaming)
                    memStream.Write(ByteParsers.UInt32.EncodeValue(FileOffset, out _));

                memStream.Write(ByteParsers.UInt32.EncodeValue(InMemoryMediaSize, out _));
                memStream.Write(ByteParsers.Byte.EncodeValue(SourceBits, out _));
                return memStream.ToArray();
            }

            public uint GetSize(AKBKSourceType sourceType)
            {
                var sourceIdSize = ByteHelper.GetPropertyTypeSize(SourceId);
                var fileIdSize = ByteHelper.GetPropertyTypeSize(FileId);

                uint fileOffsetSize = 0;
                if (sourceType != AKBKSourceType.Streaming)
                    fileOffsetSize += (uint)ByteHelper.GetPropertyTypeSize(FileOffset);

                var mediaSize = ByteHelper.GetPropertyTypeSize(InMemoryMediaSize);
                var sourceBitsSize = ByteHelper.GetPropertyTypeSize(SourceBits);
                return sourceIdSize + fileIdSize + fileOffsetSize + mediaSize + sourceBitsSize;
            }
        }
    }
}
