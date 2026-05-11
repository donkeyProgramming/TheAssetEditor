namespace Shared.GameFormats.Audio.Codecs
{
    public class VorbisAudioPacket
    {
        public byte[] Data { get; set; } = [];
        public long TimestampMilliseconds { get; set; }
    }
}
