using Shared.ByteParsing;

namespace Shared.GameFormats.Audio.Codecs
{
    public class PcmAudio
    {
        public ushort BitsPerSample { get; set; }
        public ushort Channels { get; set; }
        public byte[] Data { get; set; } = [];
        public uint SampleRate { get; set; }

        public int SampleCount => Data.Length / (BitsPerSample / BitHelper.BitsPerByte) / Channels;
    }
}
