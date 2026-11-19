namespace Shared.GameFormats.Audio.Codecs.Vorbis
{
    public class VorbisAudioPacket
    {
        public byte[] Data { get; set; } = [];
        public long TimestampMilliseconds { get; set; }

        /// <summary>Exact Ogg granule position: the number of samples fully decoded once this packet has
        /// been decoded (first packet 0, then +(previousBlock + currentBlock) / 4 per packet), clamped to
        /// the stream's sample count.</summary>
        public long GranulePosition { get; set; }
    }
}
