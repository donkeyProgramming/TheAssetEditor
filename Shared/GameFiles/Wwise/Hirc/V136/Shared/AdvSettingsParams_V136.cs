using Shared.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc.V136.Shared
{
    public class AdvSettingsParams_V136
    {
        private const byte LoudnessNormalisationBit = 0x04;

        public byte BitVector { get; set; }
        public byte VirtualQueueBehavior { get; set; }
        public ushort MaxNumInstance { get; set; }
        public byte BelowThresholdBehavior { get; set; }
        public byte BitVector2 { get; set; }
        // Corresponds to "Enable Loudness Normalization" setting on Sounds in Wwise
        public bool EnableLoudnessNormalisation { get; set; }

        public void ReadData(ByteChunk chunk)
        {
            BitVector = chunk.ReadByte();
            VirtualQueueBehavior = chunk.ReadByte();
            MaxNumInstance = chunk.ReadUShort();
            BelowThresholdBehavior = chunk.ReadByte();
            BitVector2 = chunk.ReadByte();
            EnableLoudnessNormalisation = (BitVector2 & LoudnessNormalisationBit) != 0;
        }

        public byte[] WriteData()
        {
            if (EnableLoudnessNormalisation)
                BitVector2 = (byte)(BitVector2 | LoudnessNormalisationBit);
            else
                BitVector2 = (byte)(BitVector2 & ~LoudnessNormalisationBit);

            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.Byte.EncodeValue(BitVector, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(VirtualQueueBehavior, out _));
            memStream.Write(ByteParsers.UShort.EncodeValue((byte)MaxNumInstance, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(BelowThresholdBehavior, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(BitVector2, out _));
            return memStream.ToArray();
        }

        public uint GetSize()
        {
            var bitVectorSize = ByteHelper.GetPropertyTypeSize(BitVector);
            var virtualQueueBehaviorSize = ByteHelper.GetPropertyTypeSize(VirtualQueueBehavior);
            var maxNumInstanceSize = ByteHelper.GetPropertyTypeSize(MaxNumInstance);
            var belowThresholdBehaviorSize = ByteHelper.GetPropertyTypeSize(BelowThresholdBehavior);
            var bitVector2Size = ByteHelper.GetPropertyTypeSize(BitVector2);
            return bitVectorSize + virtualQueueBehaviorSize + maxNumInstanceSize + belowThresholdBehaviorSize + bitVector2Size;
        }
    }
}
