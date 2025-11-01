using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Bkhd
{
    public class AkBankHeader
    {
        public uint BankGeneratorVersion { get; set; }
        public uint SoundBankId { get; set; }
        public uint LanguageId { get; set; }
        public uint AltValues { get; set; }
        public uint ProjectId { get; set; }
        public byte[] Padding { get; set; }

        public void ReadData(ByteChunk chunk, uint chunkSize)
        {
            BankGeneratorVersion = chunk.ReadUInt32();
            SoundBankId = chunk.ReadUInt32();
            LanguageId = chunk.ReadUInt32();
            AltValues = chunk.ReadUInt32();
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
            memStream.Write(ByteParsers.UInt32.EncodeValue(AltValues, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(ProjectId, out _));

            if (Padding != null && Padding.Length > 0)
                memStream.Write(Padding, 0, Padding.Length);

            return memStream.ToArray();
        }
    }
}
