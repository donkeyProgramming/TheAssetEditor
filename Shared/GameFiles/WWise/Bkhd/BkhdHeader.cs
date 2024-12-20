namespace Shared.GameFormats.WWise.Bkhd
{
    public class BkhdHeader
    {
        public string OwnerFileName { get; set; }

        public BnkChunkHeader ChunkHeader { get; set; } = new BnkChunkHeader() { Tag = "BKHD", ChunkSize = 0x18 };

        public uint DwBankGeneratorVersion { get; set; }
        public uint DwSoundBankId { get; set; }     // Name of the file
        public uint DwLanguageId { get; set; }      // Enum 11 - English
        public uint BFeedbackInBank { get; set; }
        public uint DwProjectID { get; set; }
        public byte[] Padding { get; set; }
    }
}
