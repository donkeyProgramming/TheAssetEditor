namespace Audio.FileFormats.WWise.Didx
{
    public class DidxAudio
    {
        public uint Id { get; internal set; }
        public byte[] ByteArray { get; internal set; }
        public string OwnerFile { get; internal set; }
    }
}
