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
        public uint SoundBankID { get; set; }
        public uint LanguageID { get; set; }
        public uint FeedbackInBank { get; set; }
        public uint ProjectID { get; set; }
        public byte[] Padding { get; set; }

        public void ReadData(ByteChunk chunk, uint chunkSize)
        {
            BankGeneratorVersion = chunk.ReadUInt32();
            SoundBankID = chunk.ReadUInt32();
            LanguageID = chunk.ReadUInt32();
            FeedbackInBank = chunk.ReadUInt32();
            ProjectID = chunk.ReadUInt32();

            var headerDiff = (int)chunkSize - 20;
            if (headerDiff > 0)
                Padding = chunk.ReadBytes(headerDiff);
        }

        public byte[] WriteData()
        {
            using var memStream = new MemoryStream();

            memStream.Write(ByteParsers.UInt32.EncodeValue(BankGeneratorVersion, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(SoundBankID, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(LanguageID, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(FeedbackInBank, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(ProjectID, out _));

            if (Padding != null && Padding.Length > 0)
                memStream.Write(Padding, 0, Padding.Length);

            return memStream.ToArray();
        }
    }
}
