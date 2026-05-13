namespace Shared.ByteParsing
{
    public static class BitHelper
    {
        public const int BitsPerByte = 8;

        public static int ByteIndexFromBitPosition(int bitPosition) => bitPosition / BitsPerByte;

        public static int BitIndexInByte(int bitPosition) => bitPosition % BitsPerByte;

        public static int BitsToBytes(int bitCount) => (bitCount + BitsPerByte - 1) / BitsPerByte;

        public static int BytesToBits(int byteCount) => byteCount * BitsPerByte;

        public static int ExtractBits(byte value, int bitOffset, int bitCount)
        {
            if (bitOffset is < 0 or > 7)
                throw new ArgumentOutOfRangeException(nameof(bitOffset));

            if (bitCount is < 0 or > BitsPerByte)
                throw new ArgumentOutOfRangeException(nameof(bitCount));

            if (bitOffset + bitCount > BitsPerByte)
                throw new ArgumentOutOfRangeException(nameof(bitCount));

            if (bitCount == 0)
                return 0;

            var mask = (1 << bitCount) - 1;
            return (value >> bitOffset) & mask;
        }

        public static uint ExtractBits(uint value, int bitOffset, int bitCount)
        {
            if (bitOffset is < 0 or > 31)
                throw new ArgumentOutOfRangeException(nameof(bitOffset));

            if (bitCount is < 0 or > 32)
                throw new ArgumentOutOfRangeException(nameof(bitCount));

            if (bitOffset + bitCount > 32)
                throw new ArgumentOutOfRangeException(nameof(bitCount));

            if (bitCount == 0)
                return 0;

            var mask = bitCount == 32 ? uint.MaxValue : (1u << bitCount) - 1u;
            return (value >> bitOffset) & mask;
        }

        public static bool IsBitSet(int value, int bitIndex)
        {
            if (bitIndex is < 0 or > 31)
                throw new ArgumentOutOfRangeException(nameof(bitIndex));

            return (value & (1 << bitIndex)) != 0;
        }
    }
}