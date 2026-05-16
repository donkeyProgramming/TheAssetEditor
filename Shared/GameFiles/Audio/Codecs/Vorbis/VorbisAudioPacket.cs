namespace Shared.GameFormats.Audio.Codecs.Vorbis
{
    public class VorbisAudioPacket
    {
        public byte[] Data { get; set; } = [];
        public long TimestampMilliseconds { get; set; }
    }
}
