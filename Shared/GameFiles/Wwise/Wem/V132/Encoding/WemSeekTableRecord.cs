using Shared.ByteParsing;

namespace Shared.GameFormats.Wwise.Wem.V132.Encoding
{
    public class WemSeekTableRecord
    {
        public const uint Size = 4;

        public ushort GranuleDelta { get; set; }
        public ushort ByteCount { get; set; }

        public static WemSeekTableRecord ReadData(ByteChunk chunk)
        {
            return new WemSeekTableRecord
            {
                GranuleDelta = chunk.ReadUShort(),
                ByteCount = chunk.ReadUShort()
            };
        }

        public byte[] WriteData()
        {
            using var stream = new MemoryStream();
            stream.Write(ByteParsers.UShort.EncodeValue(GranuleDelta, out _));
            stream.Write(ByteParsers.UShort.EncodeValue(ByteCount, out _));
            return stream.ToArray();
        }
    }
}
