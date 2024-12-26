namespace Shared.GameFormats.Wwise.Bkhd
{
    public class BkhdHeader
    {
        public string OwnerFileName { get; set; }
        public BnkChunkHeader ChunkHeader { get; set; } = new BnkChunkHeader() { Tag = "BKHD", ChunkSize = 0x18 };
        public uint DwBankGeneratorVersion { get; set; }
        public uint DwSoundBankId { get; set; }
        public uint DwLanguageId { get; set; }
        public uint BFeedbackInBank { get; set; }
        public uint DwProjectId { get; set; }
        public byte[] Padding { get; set; }
    }
}
