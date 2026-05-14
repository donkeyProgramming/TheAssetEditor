namespace Shared.GameFormats.Wwise.Wem.V132.Encoding
{
    public class VorbisCodebook(byte[] data, int bitCount)
    {
        public const int SyncPatternBitWidth = 24;
        public const uint SyncPattern = 0x564342u;
        public const int DimensionsBitWidth = 16;
        public const int EntriesBitWidth = 24;
        public const int VqFloatFieldBitWidth = 32;
        public const int ValueLengthBitWidth = 4;

        public byte[] Data { get; } = data;
        public int BitCount { get; } = bitCount;
    }
}
