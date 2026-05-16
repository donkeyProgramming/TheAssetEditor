namespace Shared.ByteParsing
{
    public class BitWriter
    {
        private byte[] _buffer;

        public BitWriter(int initialCapacity)
        {
            _buffer = new byte[initialCapacity];
        }

        public int BitPosition { get; private set; }

        public void WriteAscii(string value)
        {
            foreach (var character in value)
                WriteByte((byte)character);
        }

        public void WriteByte(byte value) => WriteBits(value, BitHelper.BitsPerByte);

        public void WriteBits(uint value, int bitCount)
        {
            if (bitCount == 0)
                return;

            if (bitCount is < 0 or > 32)
                throw new ArgumentOutOfRangeException(nameof(bitCount));

            EnsureCapacity(BitPosition + bitCount);
            for (var bitIndex = 0; bitIndex < bitCount; bitIndex++)
            {
                var absoluteBitIndex = BitPosition + bitIndex;
                var byteIndex = BitHelper.ByteIndexFromBitPosition(absoluteBitIndex);
                var bitInByte = BitHelper.BitIndexInByte(absoluteBitIndex);
                var bitValue = (int)((value >> bitIndex) & 1U);
                if (bitValue != 0)
                    _buffer[byteIndex] |= (byte)(1 << bitInByte);
            }

            BitPosition += bitCount;
        }

        public void AlignToByte()
        {
            var remainder = BitHelper.BitIndexInByte(BitPosition);
            if (remainder != 0)
                WriteBits(0, BitHelper.BitsPerByte - remainder);
        }

        public byte[] ToArray()
        {
            var byteLength = BitHelper.BitsToBytes(BitPosition);
            var copy = new byte[byteLength];
            Buffer.BlockCopy(_buffer, 0, copy, 0, byteLength);
            return copy;
        }

        private void EnsureCapacity(int requiredBits)
        {
            var requiredBytes = BitHelper.BitsToBytes(requiredBits);
            if (requiredBytes <= _buffer.Length)
                return;

            Array.Resize(ref _buffer, Math.Max(requiredBytes, _buffer.Length * 2));
        }
    }
}
