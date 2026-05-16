namespace Shared.GameFormats.Audio.Containers.Ogg
{
    public class OggPacket
    {
        public byte[] PacketData { get; set; } = [];
        public long GranulePosition { get; set; }
        public bool IsBeginningOfStream { get; set; }
        public bool IsEndOfStream { get; set; }
    }
}
