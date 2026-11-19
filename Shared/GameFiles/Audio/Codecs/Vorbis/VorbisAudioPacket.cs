namespace Shared.GameFormats.Audio.Codecs.Vorbis
{
    public class VorbisAudioPacket
    {
        public byte[] Data { get; set; } = [];
        public long TimestampMilliseconds { get; set; }

        // Samples fully decoded once this packet is, clamped to the stream's sample count
        public long GranulePosition { get; set; }
    }
}
