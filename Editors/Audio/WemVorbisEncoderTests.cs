using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Shared.GameFormats.Audio.Containers.Ogg;
using Shared.GameFormats.Wwise.Wem.V132;
using Shared.GameFormats.Wwise.Wem.V132.Encoding;

namespace Test.Audio
{
    [TestFixture]
    internal class WemVorbisEncoderTests
    {
        // Covers the OggVorbisEncoder defects patched in OggVorbisEncoderFixes: mono at 44.1/48kHz
        // encoded to noise at most qualities (0.4 is the default), stereo lost level from 0.6 up, the
        // first half block of every encode was dropped, and encodes corrupted later encodes in the same
        // process. All formats run in one test so a leak between encodes would show up too.
        [Test]
        public void EncodedWems_DecodeBackToTheInput_ForEveryFormatAndQuality()
        {
            var failures = new List<string>();
            foreach (var quality in new[] { 0.0f, 0.4f, 0.6f, 1.0f })
            {
                foreach (var (channels, sampleRate) in new[] { (1, 44100), (1, 48000), (2, 44100), (1, 22050), (2, 22050) })
                {
                    var input = CreateTestSignal(channels, sampleRate, seconds: 1.0);
                    var wem = WemFile.CreateFromWavBytes(ToWav(input, channels, sampleRate), new WemEncodingSettings { Quality = quality }).WriteData();
                    var output = ReadWav(DecodeWemToWav(wem), out var outChannels);

                    var label = $"{channels}ch {sampleRate}Hz q{quality}";
                    if (outChannels != channels || output.Length != input.Length)
                    {
                        failures.Add($"{label}: got {outChannels}ch, {output.Length} samples, expected {channels}ch, {input.Length}");
                        continue;
                    }

                    // No lag search: the decoded audio has to line up with the input from sample 0
                    var snr = SignalToNoiseDb(input, output);
                    if (snr < 12)
                        failures.Add($"{label}: SNR {snr:0.0} dB");
                }
            }

            Assert.That(failures, Is.Empty);
        }

        // A tone pair, some seeded noise, and a click at the very start that a dropped first block would lose
        static float[] CreateTestSignal(int channels, int sampleRate, double seconds)
        {
            var random = new Random(1234);
            var frames = (int)(sampleRate * seconds);
            var samples = new float[frames * channels];
            for (var i = 0; i < frames; i++)
            {
                var t = (double)i / sampleRate;
                var value = 0.3 * Math.Sin(2 * Math.PI * 440 * t) + 0.15 * Math.Sin(2 * Math.PI * 1330 * t) + 0.05 * (random.NextDouble() * 2 - 1);
                if (i < 200)
                    value += 0.4 * Math.Sin(2 * Math.PI * 3000 * t);
                for (var c = 0; c < channels; c++)
                    samples[i * channels + c] = (float)(c == 0 ? value : value * 0.7);
            }
            return samples;
        }

        static byte[] ToWav(float[] samples, int channels, int sampleRate)
        {
            using var stream = new MemoryStream();
            using (var writer = new WaveFileWriter(stream, new WaveFormat(sampleRate, 16, channels)))
                writer.WriteSamples(samples.Select(x => (short)Math.Clamp(x * short.MaxValue, short.MinValue, short.MaxValue)).ToArray(), 0, samples.Length);
            return stream.ToArray();
        }

        // The same WEM -> Ogg -> PCM path the app uses for playback
        static byte[] DecodeWemToWav(byte[] wemBytes)
        {
            var oggBytes = OggFile.CreateFromWemBytes(wemBytes).WriteData();
            using var reader = new VorbisWaveReader(new MemoryStream(oggBytes, writable: false));
            using var wavStream = new MemoryStream();
            WaveFileWriter.WriteWavFileToStream(wavStream, new SampleToWaveProvider16(reader));
            return wavStream.ToArray();
        }

        static float[] ReadWav(byte[] wav, out int channels)
        {
            using var reader = new WaveFileReader(new MemoryStream(wav));
            channels = reader.WaveFormat.Channels;
            var bytes = new byte[reader.Length];
            reader.ReadExactly(bytes);
            return Enumerable.Range(0, bytes.Length / 2).Select(i => BitConverter.ToInt16(bytes, i * 2) / (float)short.MaxValue).ToArray();
        }

        static double SignalToNoiseDb(float[] reference, float[] actual)
        {
            double signal = 0, noise = 0;
            for (var i = 0; i < reference.Length; i++)
            {
                signal += reference[i] * (double)reference[i];
                var difference = reference[i] - (double)actual[i];
                noise += difference * difference;
            }
            return 10 * Math.Log10(signal / Math.Max(noise, 1e-12));
        }
    }
}
