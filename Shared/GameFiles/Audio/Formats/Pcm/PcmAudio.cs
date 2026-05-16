using NVorbis;
using Shared.ByteParsing;
using Shared.GameFormats.Audio.Containers.Ogg;

namespace Shared.GameFormats.Audio.Formats.Pcm
{
    public class PcmAudio
    {
        private const float MinPcmSampleValue = -1.0f;
        private const float MaxPcmSampleValue = 1.0f;
        private const int PcmBitsPerSample = 16;
        private const int PcmSampleReadBufferSize = 4096;

        public ushort BitsPerSample { get; set; }
        public ushort Channels { get; set; }
        public byte[] Data { get; set; } = [];
        public uint SampleRate { get; set; }
        public int SampleCount => Data.Length / (BitsPerSample / BitHelper.BitsPerByte) / Channels;

        public static PcmAudio CreateFromWemBytes(byte[] wemBytes)
        {
            var oggBytes = OggFile.CreateFromWemBytes(wemBytes).WriteData();
            return CreateFromOggBytes(oggBytes);
        }

        public static PcmAudio CreateFromOggBytes(byte[] oggData)
        {
            using var oggStream = new MemoryStream(oggData, writable: false);
            using var vorbisReader = new VorbisReader(oggStream, closeOnDispose: false);

            var channels = vorbisReader.Channels;
            var sampleRate = vorbisReader.SampleRate;
            var sampleReadBuffer = new float[PcmSampleReadBufferSize * channels];
            using var audioDataStream = new MemoryStream();

            int samplesRead;
            while ((samplesRead = vorbisReader.ReadSamples(sampleReadBuffer, 0, sampleReadBuffer.Length)) > 0)
            {
                for (var sampleIndex = 0; sampleIndex < samplesRead; sampleIndex++)
                {
                    var clampedSample = Math.Clamp(sampleReadBuffer[sampleIndex], MinPcmSampleValue, MaxPcmSampleValue);
                    var pcmSample = (short)Math.Round(clampedSample * short.MaxValue);
                    audioDataStream.WriteByte((byte)BitHelper.ExtractBits((uint)pcmSample, 0, BitHelper.BitsPerByte));
                    audioDataStream.WriteByte((byte)BitHelper.ExtractBits((uint)pcmSample, BitHelper.BitsPerByte, BitHelper.BitsPerByte));
                }
            }

            return new PcmAudio
            {
                BitsPerSample = PcmBitsPerSample,
                Channels = (ushort)channels,
                Data = audioDataStream.ToArray(),
                SampleRate = (uint)sampleRate,
            };
        }
    }
}
