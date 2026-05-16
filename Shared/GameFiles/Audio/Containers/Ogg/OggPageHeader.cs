using System.Text;
using Shared.ByteParsing;

namespace Shared.GameFormats.Audio.Containers.Ogg
{
    public class OggPageHeader
    {
        public const int FixedHeaderSize = 27;
        public const string OggCapturePattern = "OggS";
        public const byte BeginningOfStreamFlag = 0x02;
        public const byte EndOfStreamFlag = 0x04;

        public string CapturePattern { get; set; } = OggCapturePattern;
        public byte Version { get; set; }
        public byte HeaderType { get; set; }
        public long GranulePosition { get; set; }
        public int SerialNumber { get; set; }
        public int SequenceNumber { get; set; }
        public uint Checksum { get; set; }
        public byte PageSegmentCount { get; set; }
        public byte[] SegmentSizes { get; set; } = [];

        public bool IsBeginningOfStream => (HeaderType & BeginningOfStreamFlag) != 0;
        public bool IsEndOfStream => (HeaderType & EndOfStreamFlag) != 0;
        public int PayloadSize => SegmentSizes.Sum(segmentSize => segmentSize);

        public static OggPageHeader ReadData(ByteChunk chunk)
        {
            if (chunk.BytesLeft < FixedHeaderSize)
                throw new InvalidDataException("Ogg stream ended before a complete page header could be read.");

            var header = new OggPageHeader
            {
                CapturePattern = chunk.ReadFixedLength(4),
                Version = chunk.ReadByte(),
                HeaderType = chunk.ReadByte(),
                GranulePosition = chunk.ReadInt64(),
                SerialNumber = chunk.ReadInt32(),
                SequenceNumber = chunk.ReadInt32(),
                Checksum = chunk.ReadUInt32(),
                PageSegmentCount = chunk.ReadByte(),
            };

            if (header.CapturePattern != OggCapturePattern)
                throw new InvalidDataException($"Invalid Ogg capture pattern '{header.CapturePattern}'.");

            if (chunk.BytesLeft < header.PageSegmentCount)
                throw new InvalidDataException("Ogg page segment table exceeds remaining bytes.");

            header.SegmentSizes = chunk.ReadBytes(header.PageSegmentCount);
            return header;
        }

        public byte[] WriteData()
        {
            using var header = new MemoryStream(FixedHeaderSize + SegmentSizes.Length);
            header.Write(Encoding.ASCII.GetBytes(CapturePattern));
            header.Write(ByteParsers.Byte.EncodeValue(Version, out _));
            header.Write(ByteParsers.Byte.EncodeValue(HeaderType, out _));
            header.Write(ByteParsers.Int64.EncodeValue(GranulePosition, out _));
            header.Write(ByteParsers.Int32.EncodeValue(SerialNumber, out _));
            header.Write(ByteParsers.Int32.EncodeValue(SequenceNumber, out _));
            header.Write(ByteParsers.UInt32.EncodeValue(Checksum, out _));
            header.Write(ByteParsers.Byte.EncodeValue(PageSegmentCount, out _));
            header.Write(SegmentSizes);
            return header.ToArray();
        }
    }
}
