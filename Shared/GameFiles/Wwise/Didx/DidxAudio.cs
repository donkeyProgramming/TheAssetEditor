namespace Shared.GameFormats.Wwise.Didx
{
    public class DidxAudio
    {
        public uint Id { get; set; }
        public byte[] ByteArray { get; set; }
        public string OwnerFilePath { get; set; }
        public uint LanguageId { get; set; }
    }
}
