using System.Buffers.Binary;
using System.Text;
using Shared.ByteParsing;

namespace Shared.GameFormats.Audio.Ogg
{
    public static class OggSerialiser
    {
        private const int OggMaxSegmentSize = 255;
        private const int OggPageHeaderBaseSize = 27;
        private const int OggBeginningOfStreamFlag = 0x02;
        private const int OggEndOfStreamFlag = 0x04;
        private const int OggCrcFieldByteOffset = 22;
        private const int OggCrc32TableSize = 256;
        private const uint OggCrc32Polynomial = 0x04C11DB7u;
        private const int OggCrc32TopByteShift = 24;
        private const int OggCrc32TopBitIndex = 31;

        private static readonly uint[] s_oggCyclicRedundancyCheckTable = BuildOggCyclicRedundancyCheckTable();

        public static byte[] WritePackets(List<OggAudioPacket> packets, int serialNumber)
        {
            using var output = new MemoryStream();
            var sequenceNumber = 0;
            foreach (var packet in packets)
            {
                WriteOggPage(output, packet, serialNumber, sequenceNumber);
                sequenceNumber++;
            }

            return output.ToArray();
        }

        private static void WriteOggPage(Stream output, OggAudioPacket packet, int serialNumber, int sequenceNumber)
        {
            var packetLength = packet.PacketData.Length;
            var segmentCount = packetLength / OggMaxSegmentSize + 1;
            if (segmentCount > OggMaxSegmentSize)
                throw new InvalidDataException("Vorbis packet is too large for a single Ogg page.");

            var headerType = 0;
            if (packet.IsBeginningOfStream)
                headerType |= OggBeginningOfStreamFlag;

            if (packet.IsEndOfStream)
                headerType |= OggEndOfStreamFlag;

            using var header = new MemoryStream(OggPageHeaderBaseSize + segmentCount);
            header.Write(Encoding.ASCII.GetBytes("OggS"));
            header.Write(ByteParsers.Byte.EncodeValue(0, out _));
            header.Write(ByteParsers.Byte.EncodeValue((byte)headerType, out _));
            header.Write(ByteParsers.Int64.EncodeValue(packet.GranulePosition, out _));
            header.Write(ByteParsers.Int32.EncodeValue(serialNumber, out _));
            header.Write(ByteParsers.Int32.EncodeValue(sequenceNumber, out _));
            header.Write(ByteParsers.UInt32.EncodeValue(0u, out _));
            header.Write(ByteParsers.Byte.EncodeValue((byte)segmentCount, out _));

            var remainingBytes = packetLength;
            for (var segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
            {
                byte segmentSize;
                if (remainingBytes >= OggMaxSegmentSize)
                    segmentSize = OggMaxSegmentSize;
                else
                    segmentSize = (byte)remainingBytes;

                header.Write(ByteParsers.Byte.EncodeValue(segmentSize, out _));
                remainingBytes -= OggMaxSegmentSize;
            }

            var headerBytes = header.ToArray();
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
            var cyclicRedundancyCheck = 0u;
            foreach (var pageByte in data)
            {
                var tableIndex = (int)(BitHelper.ExtractBits(cyclicRedundancyCheck, OggCrc32TopByteShift, BitHelper.BitsPerByte) ^ pageByte);
                cyclicRedundancyCheck = (cyclicRedundancyCheck << BitHelper.BitsPerByte) ^ s_oggCyclicRedundancyCheckTable[tableIndex];
            }
            return cyclicRedundancyCheck;
        }
    }
}
