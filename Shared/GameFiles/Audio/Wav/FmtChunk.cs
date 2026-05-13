using Shared.ByteParsing;

namespace Shared.GameFormats.Audio.Wav
{
    public class FmtChunk : RiffChunk
    {
        public const int ChunkSize = 16;
        public const string ChunkTag = "fmt ";
        public const ushort PcmFormatTag = 1;

        public int Size { get; set; } = ChunkSize;
        public ushort FormatTag { get; set; } = PcmFormatTag;
        public ushort Channels { get; set; }
        public uint SampleRate { get; set; }
        public uint ByteRate { get; set; }
        public ushort BlockAlign { get; set; }
        public ushort BitsPerSample { get; set; }

        public FmtChunk()
        {
            Tag = ChunkTag;
        }

        public override void ReadData(ByteChunk chunk)
        {
            if (chunk.BytesLeft < ChunkSize)
                throw new InvalidDataException($"WAV fmt chunk must be at least {ChunkSize} bytes.");

            FormatTag = chunk.ReadUShort();
            Channels = chunk.ReadUShort();
            SampleRate = chunk.ReadUInt32();
            ByteRate = chunk.ReadUInt32();
            BlockAlign = chunk.ReadUShort();
            BitsPerSample = chunk.ReadUShort();
        }

        public override byte[] WriteData()
        {
            using var stream = new MemoryStream(ChunkSize);
            stream.Write(ByteParsers.UShort.EncodeValue(FormatTag, out _));
            stream.Write(ByteParsers.UShort.EncodeValue(Channels, out _));
            stream.Write(ByteParsers.UInt32.EncodeValue(SampleRate, out _));
            stream.Write(ByteParsers.UInt32.EncodeValue(ByteRate, out _));
            stream.Write(ByteParsers.UShort.EncodeValue(BlockAlign, out _));
            stream.Write(ByteParsers.UShort.EncodeValue(BitsPerSample, out _));
            return stream.ToArray();
        }
    }
}
