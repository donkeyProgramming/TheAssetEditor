namespace Shared.GameFormats.Wwise.Didx
{
    public class DidxAudio
    {
        public uint ID { get; set; }
        public byte[] ByteArray { get; set; }
        public string OwnerFilePath { get; set; }
        public uint LanguageID { get; set; }
    }
}
