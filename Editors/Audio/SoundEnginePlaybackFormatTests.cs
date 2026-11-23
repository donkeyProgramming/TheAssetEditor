using System;
using System.IO;
using Editors.Audio.Shared.Wwise;
using NAudio.Wave;

namespace Test.Audio
{
    // Ported from Szy-Cathay/TheAssetEditor-CN 59246da4a
    public class SoundEnginePlaybackFormatTests
    {
        const int SampleRate = 48000;

        [Test]
        public void Extensible24BitPcm_PlaysAs16BitPcm_WithTheSameSignal()
        {
            var samples = Sine(SampleRate / 10);
            using var stream = new MemoryStream(ExtensibleWav24(samples), writable: false);
            using var reader = new WaveFileReader(stream);
            Assert.That(reader.WaveFormat.Encoding, Is.EqualTo(WaveFormatEncoding.Extensible));

            var provider = SoundEngine.CreatePlaybackProvider(reader, out var compatibleStream);
            using var _ = compatibleStream;

            Assert.That(compatibleStream, Is.Not.Null);
            Assert.That(provider.WaveFormat.Encoding, Is.EqualTo(WaveFormatEncoding.Pcm));
            Assert.That(provider.WaveFormat.BitsPerSample, Is.EqualTo(16));
            Assert.That(provider.WaveFormat.SampleRate, Is.EqualTo(SampleRate));
            Assert.That(provider.WaveFormat.Channels, Is.EqualTo(1));

            var output = new byte[samples.Length * 2];
            var read = 0;
            int n;
            while (read < output.Length && (n = provider.Read(output, read, output.Length - read)) > 0)
                read += n;
            Assert.That(read, Is.EqualTo(output.Length));

            for (var i = 0; i < samples.Length; i++)
            {
                var expected = samples[i] / 256.0;  // 24-bit to 16-bit
                Assert.That(BitConverter.ToInt16(output, i * 2), Is.EqualTo(expected).Within(1.5), $"sample {i}");
            }
        }

        [Test]
        public void Plain16BitPcm_UsesTheReaderDirectly()
        {
            using var stream = new MemoryStream(PlainWav16(800), writable: false);
            using var reader = new WaveFileReader(stream);

            var provider = SoundEngine.CreatePlaybackProvider(reader, out var compatibleStream);

            Assert.That(provider, Is.SameAs(reader));
            Assert.That(compatibleStream, Is.Null);
        }

        static int[] Sine(int count)
        {
            var samples = new int[count];
            for (var i = 0; i < count; i++)
                samples[i] = (int)(Math.Sin(i * 2 * Math.PI * 440 / SampleRate) * 0x3FFFFF);
            return samples;
        }

        static byte[] ExtensibleWav24(int[] samples)
        {
            var data = new byte[samples.Length * 3];
            for (var i = 0; i < samples.Length; i++)
            {
                data[i * 3] = (byte)samples[i];
                data[i * 3 + 1] = (byte)(samples[i] >> 8);
                data[i * 3 + 2] = (byte)(samples[i] >> 16);
            }
            return Write(new WaveFormatExtensible(SampleRate, 24, 1), data);
        }

        static byte[] PlainWav16(int sampleCount) => Write(new WaveFormat(8000, 16, 1), new byte[sampleCount * 2]);

        static byte[] Write(WaveFormat format, byte[] data)
        {
            using var stream = new MemoryStream();
            using (var writer = new WaveFileWriter(stream, format))
                writer.Write(data, 0, data.Length);
            return stream.ToArray();
        }
    }
}
