using System.Buffers.Binary;
using Shared.ByteParsing;
using Shared.GameFormats.Audio.Codecs.Vorbis;

namespace Shared.GameFormats.Audio.Containers.Ogg
{
    public class OggFile
    {
        private const int OggDefaultSerialNumber = 1;
        private const int VorbisHeaderPacketCount = 3;
        private const double MillisecondsPerSecond = 1000.0;
        private const int OggMaxSegmentSize = 255;
        private const int OggCrcFieldByteOffset = 22;
        private const int OggCrc32TableSize = 256;
        private const uint OggCrc32Polynomial = 0x04C11DB7u;
        private const int OggCrc32TopByteShift = 24;
        private const int OggCrc32TopBitIndex = 31;

        private static readonly uint[] s_oggCyclicRedundancyCheckTable = BuildOggCyclicRedundancyCheckTable();

        public int SerialNumber { get; set; } = 1;
        public List<OggPacket> Packets { get; set; } = [];

        public static OggFile CreateFromWemBytes(byte[] wemBytes)
        {
            var vorbisAudio = VorbisAudio.CreateFromWemBytes(wemBytes);

            var allPackets = new List<OggPacket>(VorbisHeaderPacketCount + vorbisAudio.Packets.Count)
            {
                new() { PacketData = vorbisAudio.IdentificationHeader.WriteData(), GranulePosition = 0, IsBeginningOfStream = true, IsEndOfStream = false },
                new() { PacketData = vorbisAudio.CommentHeader.WriteData(), GranulePosition = 0, IsBeginningOfStream = false, IsEndOfStream = false },
                new() { PacketData = vorbisAudio.SetupHeader.WriteData(), GranulePosition = 0, IsBeginningOfStream = false, IsEndOfStream = false }
            };

            for (var packetIndex = 0; packetIndex < vorbisAudio.Packets.Count; packetIndex++)
            {
                var packetData = vorbisAudio.Packets[packetIndex].Data;
                long nextTimestampMilliseconds;
                if (packetIndex + 1 < vorbisAudio.Packets.Count)
                    nextTimestampMilliseconds = vorbisAudio.Packets[packetIndex + 1].TimestampMilliseconds;
                else
                    nextTimestampMilliseconds = (long)Math.Round(vorbisAudio.SampleCount * MillisecondsPerSecond / vorbisAudio.SampleRate);

                var granulePosition = (long)Math.Round(nextTimestampMilliseconds * vorbisAudio.SampleRate / MillisecondsPerSecond);
                if (packetIndex == vorbisAudio.Packets.Count - 1)
                    granulePosition = vorbisAudio.SampleCount;

                allPackets.Add(new OggPacket
                {
                    PacketData = packetData,
                    GranulePosition = Math.Max(0, granulePosition),
                    IsBeginningOfStream = false,
                    IsEndOfStream = packetIndex == vorbisAudio.Packets.Count - 1,
                });
            }

            return new OggFile
            {
                SerialNumber = OggDefaultSerialNumber,
                Packets = allPackets,
            };
        }

        public void ReadData(ByteChunk chunk)
        {
            Packets = [];
            SerialNumber = 1;

            using var packetBuffer = new MemoryStream();
            var hasOpenPacket = false;
            var packetStartOnThisPage = false;
            var firstPage = true;

            while (chunk.BytesLeft > 0)
            {
                var pageHeader = OggPageHeader.ReadData(chunk);

                if (chunk.BytesLeft < pageHeader.PayloadSize)
                    throw new InvalidDataException("Ogg page payload exceeds remaining bytes.");

                if (firstPage)
                {
                    SerialNumber = pageHeader.SerialNumber;
                    firstPage = false;
                }

                var packetCompletedOnThisPageCount = 0;
                for (var segmentIndex = 0; segmentIndex < pageHeader.SegmentSizes.Length; segmentIndex++)
                {
                    var segmentSize = pageHeader.SegmentSizes[segmentIndex];

                    if (!hasOpenPacket)
                    {
                        hasOpenPacket = true;
                        packetStartOnThisPage = true;
                        packetBuffer.SetLength(0);
                    }

                    if (segmentSize > 0)
                    {
                        var segmentData = chunk.ReadBytes(segmentSize);
                        packetBuffer.Write(segmentData);
                    }

                    if (segmentSize != 255)
                    {
                        packetCompletedOnThisPageCount++;
                        Packets.Add(new OggPacket
                        {
                            PacketData = packetBuffer.ToArray(),
                            GranulePosition = pageHeader.GranulePosition,
                            IsBeginningOfStream = packetStartOnThisPage && packetCompletedOnThisPageCount == 1 && pageHeader.IsBeginningOfStream,
                            IsEndOfStream = pageHeader.IsEndOfStream && segmentIndex == pageHeader.SegmentSizes.Length - 1,
                        });

                        hasOpenPacket = false;
                    }
                }
            }

            if (hasOpenPacket)
                throw new InvalidDataException("Ogg stream ended with an incomplete packet.");
        }

        public byte[] WriteData()
        {
            using var output = new MemoryStream();
            var sequenceNumber = 0;
            foreach (var packet in Packets)
            {
                WriteOggPage(output, packet, SerialNumber, sequenceNumber);
                sequenceNumber++;
            }

            return output.ToArray();
        }

        private static void WriteOggPage(Stream output, OggPacket packet, int serialNumber, int sequenceNumber)
        {
            var packetLength = packet.PacketData.Length;
            var segmentCount = packetLength / OggMaxSegmentSize + 1;
            if (segmentCount > OggMaxSegmentSize)
                throw new InvalidDataException("Vorbis packet is too large for a single Ogg page.");

            byte headerType = 0;
            if (packet.IsBeginningOfStream)
                headerType |= OggPageHeader.BeginningOfStreamFlag;

            if (packet.IsEndOfStream)
                headerType |= OggPageHeader.EndOfStreamFlag;

            var remainingBytes = packetLength;
            var segmentSizes = new byte[segmentCount];
            for (var segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
            {
                byte segmentSize;
                if (remainingBytes >= OggMaxSegmentSize)
                    segmentSize = OggMaxSegmentSize;
                else
                    segmentSize = (byte)remainingBytes;

                segmentSizes[segmentIndex] = segmentSize;
                remainingBytes -= OggMaxSegmentSize;
            }

            var pageHeader = new OggPageHeader
            {
                CapturePattern = OggPageHeader.OggCapturePattern,
                Version = 0,
                HeaderType = headerType,
                GranulePosition = packet.GranulePosition,
                SerialNumber = serialNumber,
                SequenceNumber = sequenceNumber,
                Checksum = 0u,
                PageSegmentCount = (byte)segmentCount,
                SegmentSizes = segmentSizes,
            };

            var headerBytes = pageHeader.WriteData();
            var page = new byte[headerBytes.Length + packetLength];
            headerBytes.CopyTo(page, 0);
            packet.PacketData.CopyTo(page, headerBytes.Length);

            var cyclicRedundancyCheck = ComputeOggCyclicRedundancyCheck32(page);
            BinaryPrimitives.WriteUInt32LittleEndian(page.AsSpan(OggCrcFieldByteOffset), cyclicRedundancyCheck);

            output.Write(page);
        }

        private static uint[] BuildOggCyclicRedundancyCheckTable()
        {
            var table = new uint[OggCrc32TableSize];
            for (uint tableIndex = 0; tableIndex < OggCrc32TableSize; tableIndex++)
            {
                var cyclicRedundancyCheck = tableIndex << OggCrc32TopByteShift;
                for (var bitIndex = 0; bitIndex < BitHelper.BitsPerByte; bitIndex++)
                {
                    if (BitHelper.IsBitSet(unchecked((int)cyclicRedundancyCheck), OggCrc32TopBitIndex))
                        cyclicRedundancyCheck = (cyclicRedundancyCheck << 1) ^ OggCrc32Polynomial;
                    else
                        cyclicRedundancyCheck <<= 1;
                }

                table[tableIndex] = cyclicRedundancyCheck;
            }
            return table;
        }

        private static uint ComputeOggCyclicRedundancyCheck32(byte[] data)
        {
            uint cyclicRedundancyCheck = 0;
            foreach (var pageByte in data)
            {
                var tableIndex = (int)(BitHelper.ExtractBits(cyclicRedundancyCheck, OggCrc32TopByteShift, BitHelper.BitsPerByte) ^ pageByte);
                cyclicRedundancyCheck = (cyclicRedundancyCheck << BitHelper.BitsPerByte) ^ s_oggCyclicRedundancyCheckTable[tableIndex];
            }
            return cyclicRedundancyCheck;
        }
    }
}
