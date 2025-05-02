using Shared.Core.ByteParsing;
using Shared.GameFormats.Wwise.Enums;

namespace Shared.GameFormats.Wwise.Bkhd
{
    public class BkhdChunk
    {
        public string OwnerFilePath { get; set; }
        public BnkChunkHeader ChunkHeader { get; set; } = new BnkChunkHeader() { Tag = BankChunkTypes.BKHD, ChunkSize = 0x18 };
        public AkBankHeader AkBankHeader { get; set; } = new AkBankHeader();
    }

    public class AkBankHeader
    {
        public uint BankGeneratorVersion { get; set; }
        public uint SoundBankId { get; set; }
        public uint LanguageId { get; set; }
        public uint FeedbackInBank { get; set; }
        public uint ProjectId { get; set; }
        public byte[] Padding { get; set; }

        public void ReadData(ByteChunk chunk, uint chunkSize)
        {
            BankGeneratorVersion = chunk.ReadUInt32();
            SoundBankId = chunk.ReadUInt32();
            LanguageId = chunk.ReadUInt32();
            FeedbackInBank = chunk.ReadUInt32();
            ProjectId = chunk.ReadUInt32();

            var headerDiff = (int)chunkSize - 20;
            if (headerDiff > 0)
                Padding = chunk.ReadBytes(headerDiff);
        }

        public byte[] WriteData()
        {
            using var memStream = new MemoryStream();

            memStream.Write(ByteParsers.UInt32.EncodeValue(BankGeneratorVersion, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(SoundBankId, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(LanguageId, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(FeedbackInBank, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(ProjectId, out _));

            if (Padding != null && Padding.Length > 0)
                memStream.Write(Padding, 0, Padding.Length);

            return memStream.ToArray();
        }
    }
}
