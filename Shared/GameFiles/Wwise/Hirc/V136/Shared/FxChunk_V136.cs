using Shared.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc.V136.Shared
{
    public class FxChunk_V136
    {
        public byte FxIndex { get; set; }
        public uint FxId { get; set; }
        public byte IsShareSet { get; set; }
        public byte IsRendered { get; set; }

        public const uint Size = 7;

        public static FxChunk_V136 ReadData(ByteChunk chunk)
        {
            return new FxChunk_V136
            {
                FxIndex = chunk.ReadByte(),
                FxId = chunk.ReadUInt32(),
                IsShareSet = chunk.ReadByte(),
                IsRendered = chunk.ReadByte()
            };
        }

        public byte[] WriteData()
        {
            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.Byte.EncodeValue(FxIndex, out _));
            memStream.Write(ByteParsers.UInt32.EncodeValue(FxId, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(IsShareSet, out _));
            memStream.Write(ByteParsers.Byte.EncodeValue(IsRendered, out _));
            return memStream.ToArray();
        }
    }
}
