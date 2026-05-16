namespace Shared.ByteParsing
{
    public class BitChunk
    {
        private readonly byte[] _buffer;

        public BitChunk(byte[] buffer)
        {
            _buffer = buffer;
        }

        public int BitPosition { get; private set; }

        public uint ReadBits(int bitCount)
        {
            if (bitCount == 0)
                return 0;

            if (bitCount is < 0 or > 32)
                throw new ArgumentOutOfRangeException(nameof(bitCount));

            if (BitPosition + bitCount > BitHelper.BytesToBits(_buffer.Length))
                throw new EndOfStreamException("Attempted to read past the end of the bitstream.");

            uint value = 0;
            for (var bitIndex = 0; bitIndex < bitCount; bitIndex++)
            {
                var absoluteBitIndex = BitPosition + bitIndex;
                var byteIndex = BitHelper.ByteIndexFromBitPosition(absoluteBitIndex);
                var bitInByte = BitHelper.BitIndexInByte(absoluteBitIndex);
                var bitValue = (_buffer[byteIndex] >> bitInByte) & 1;
                value |= (uint)bitValue << bitIndex;
            }

            BitPosition += bitCount;
            return value;
        }

        public byte[] ReadBitRangeAsBytes(int startBit, int bitCount)
        {
            if (bitCount < 0)
                throw new ArgumentOutOfRangeException(nameof(bitCount));

            if (startBit < 0 || startBit + bitCount > BitHelper.BytesToBits(_buffer.Length))
                throw new ArgumentOutOfRangeException(nameof(startBit));

            var output = new byte[BitHelper.BitsToBytes(bitCount)];
            for (var bitIndex = 0; bitIndex < bitCount; bitIndex++)
            {
                var sourceBit = startBit + bitIndex;
                var sourceByte = BitHelper.ByteIndexFromBitPosition(sourceBit);
                var sourceShift = BitHelper.BitIndexInByte(sourceBit);
                var bitValue = (_buffer[sourceByte] >> sourceShift) & 1;
                if (bitValue != 0)
                    output[BitHelper.ByteIndexFromBitPosition(bitIndex)] |= (byte)(1 << BitHelper.BitIndexInByte(bitIndex));
            }

            return output;
        }
    }
}
