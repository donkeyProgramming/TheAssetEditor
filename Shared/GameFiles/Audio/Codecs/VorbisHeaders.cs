namespace Shared.GameFormats.Audio.Codecs
{
    public class VorbisHeaders
    {
        public byte[] Identification { get; set; } = [];
        public byte[] Comment { get; set; } = [];
        public byte[] Setup { get; set; } = [];
    }
}
