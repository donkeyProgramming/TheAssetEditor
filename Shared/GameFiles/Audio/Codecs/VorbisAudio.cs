using NVorbis;
using Shared.ByteParsing;
using Shared.GameFormats.Audio.Ogg;

namespace Shared.GameFormats.Audio.Codecs
{
    public class VorbisAudio
    {
        private const byte XiphLacingHeaderByte = 0x02;
        private const int XiphLacingContinuationByte = 255;
        private const float MinPcmSampleValue = -1.0f;
        private const float MaxPcmSampleValue = 1.0f;
        private const int PcmBitsPerSample = 16;
        private const int PcmSampleReadBufferSize = 4096;
        private const int OggSerialNumber = 1;
        private const int VorbisHeaderPacketCount = 3;
        private const double MillisecondsPerSecond = 1000.0;

        public byte Channels { get; set; }
        public byte[] VorbisCodecPrivateData { get; set; } = [];
        public List<VorbisAudioPacket> Packets { get; set; } = [];
        public int SampleCount { get; set; }
        public uint SampleRate { get; set; }

        public PcmAudio ToPcm()
        {
            var oggData = ToOgg();
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

        public byte[] ToOgg()
        {
            var headerPackets = ParseXiphLacedVorbisHeaders(VorbisCodecPrivateData);
            var allPackets = new List<OggAudioPacket>(VorbisHeaderPacketCount + Packets.Count)
            {
                new() { PacketData = headerPackets.Identification, GranulePosition = 0, IsBeginningOfStream = true, IsEndOfStream = false },
                new() { PacketData = headerPackets.Comment, GranulePosition = 0, IsBeginningOfStream = false, IsEndOfStream = false },
                new() { PacketData = headerPackets.Setup, GranulePosition = 0, IsBeginningOfStream = false, IsEndOfStream = false }
            };

            for (var packetIndex = 0; packetIndex < Packets.Count; packetIndex++)
            {
                var packetData = Packets[packetIndex].Data;
                long nextTimestampMilliseconds;
                if (packetIndex + 1 < Packets.Count)
                    nextTimestampMilliseconds = Packets[packetIndex + 1].TimestampMilliseconds;
                else
                    nextTimestampMilliseconds = (long)Math.Round(SampleCount * MillisecondsPerSecond / SampleRate);

                var granulePosition = (long)Math.Round(nextTimestampMilliseconds * SampleRate / MillisecondsPerSecond);
                if (packetIndex == Packets.Count - 1)
                    granulePosition = SampleCount;

                allPackets.Add(new OggAudioPacket
                {
                    PacketData = packetData,
                    GranulePosition = Math.Max(0, granulePosition),
                    IsBeginningOfStream = false,
                    IsEndOfStream = packetIndex == Packets.Count - 1,
                });
            }

            return OggSerialiser.WritePackets(allPackets, OggSerialNumber);
        }

        private static VorbisHeaders ParseXiphLacedVorbisHeaders(byte[] codecPrivate)
        {
            var chunk = new ByteChunk(codecPrivate);

            if (chunk.ReadByte() != XiphLacingHeaderByte)
                throw new InvalidDataException("Vorbis codec private data is missing the expected Xiph lacing header.");

            var identificationPacketSize = ReadXiphLacedSize(chunk);
            var commentPacketSize = ReadXiphLacedSize(chunk);
            var setupPacketSize = chunk.BytesLeft - identificationPacketSize - commentPacketSize;

            if (identificationPacketSize <= 0 || commentPacketSize <= 0 || setupPacketSize <= 0)
                throw new InvalidDataException("Vorbis codec private packet sizes are invalid.");

            var identificationPacket = chunk.ReadBytes(identificationPacketSize);
            var commentPacket = chunk.ReadBytes(commentPacketSize);
            var setupPacket = chunk.ReadBytes(setupPacketSize);

            return new VorbisHeaders
            {
                Identification = identificationPacket,
                Comment = commentPacket,
                Setup = setupPacket,
            };
        }

        private static int ReadXiphLacedSize(ByteChunk chunk)
        {
            var size = 0;
            while (chunk.BytesLeft > 0)
            {
                var value = chunk.ReadByte();
                size += value;
                if (value != XiphLacingContinuationByte)
                    return size;
            }

            throw new InvalidDataException("Unexpected end of Xiph lacing size encoding.");
        }

    }
}
