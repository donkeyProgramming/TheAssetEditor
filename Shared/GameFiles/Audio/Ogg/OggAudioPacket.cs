namespace Shared.GameFormats.Audio.Ogg
{
    public class OggAudioPacket
    {
        public byte[] PacketData { get; set; } = [];
        public long GranulePosition { get; set; }
        public bool IsBeginningOfStream { get; set; }
        public bool IsEndOfStream { get; set; }
    }
}
